using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HarborAdmin.AIWorker.Infrastructure;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.Client.AI.Constants;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Contracts.Shared.Snapshot;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.AIWorker.Application;

/// <summary>
/// AI 调用执行服务。
/// </summary>
public sealed class AiExecutionService(
    AiRuntimeConfigCache configCache,
    AiPromptComposer promptComposer,
    AiProviderAdapterResolver adapterResolver,
    IAiQuotaService quotaService,
    IAiRepository repository,
    ISecretResolver secretResolver,
    IEventPublisher eventPublisher,
    ILogger<AiExecutionService> logger)
{
    private static readonly ActivitySource ActivitySource = new("HarborAdmin.AI");
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 执行非流式 AI 调用。
    /// </summary>
    public async Task<AiBusinessResponse> InvokeAsync(AiBusinessRequest request, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRequest(request);
        var existing = await FindExistingInvocationAsync(normalized, cancellationToken);
        if (existing is not null)
        {
            return FromExistingLog(existing, normalized.Context);
        }

        var setup = await ResolveSetupAsync(normalized, streaming: false, cancellationToken);
        var log = await CreateLogAsync(normalized, setup.ReleaseVersion, streaming: false, setup.ContextLength, cancellationToken);
        if (!setup.Success)
        {
            var response = FailedResponse(normalized, setup.ReleaseVersion, setup.ErrorCode!, setup.ErrorMessage!);
            await CompleteLogAsync(log, "Failed", setup.ErrorCode, setup.ErrorMessage, null, null, [], new AiUsage(), cancellationToken);
            return response;
        }

        using var activity = ActivitySource.StartActivity("ai.invoke");
        var snapshot = setup.Snapshot!;
        var business = setup.Business!;
        activity?.SetTag("ai.business", business.BusinessKey);
        activity?.SetTag("ai.release", snapshot.Version);

        var stopwatch = Stopwatch.StartNew();
        var fallbackTrace = new List<string>();
        foreach (var route in business.Routes.OrderBy(r => r.Priority))
        {
            // 路由按优先级尝试；校验失败或可恢复错误会进入下一路由。
            var provider = snapshot.Providers.FirstOrDefault(p => string.Equals(p.ProviderKey, route.ProviderKey, StringComparison.Ordinal));
            var model = ResolveModel(normalized, route, provider);
            var profile = BuildCallProfile(business, normalized, route);
            var validation = ValidateRoute(business, provider, model, profile, normalized, streaming: false);
            if (validation.Error is not null)
            {
                fallbackTrace.Add($"{route.ProviderKey}:{validation.Error}");
                continue;
            }

            var reservation = new AiQuotaReservation([]);
            try
            {
                reservation = await quotaService.ReserveAsync(snapshot, provider!, model!, business, normalized.ProducerKey!, setup.ContextLength,
                    cancellationToken);
                var adapter = adapterResolver.Resolve(provider!.AdapterType);
                var apiKey = await ResolveProviderSecretAsync(provider, cancellationToken);
                // 所有供应商适配器共享统一请求对象，具体协议差异下沉到 adapter。
                var callRequest = new AiProviderCallRequest(provider, model!, setup.Messages, false, normalized.InvocationId!, normalized.CorrelationId!,
                    snapshot.Version, apiKey, profile.OutputOptions, profile.ToolOptions, profile.ProviderOptions, route.ProviderOptionsJson,
                    route.OpenRouterOptionsJson);
                var result = await InvokeWithResilienceAsync(adapter, callRequest, provider, profile.OutputOptions, cancellationToken);
                var usage = ApplyPricing(result.Usage, validation.Model);
                await quotaService.CommitAsync(reservation, usage, success: true, cancellationToken);
                var response = new AiBusinessResponse(true, normalized.InvocationId!, normalized.CorrelationId!, "Succeeded", snapshot.Version,
                    result.Content, provider.ProviderKey, model, usage, setup.References, normalized.Context);
                await CompleteLogAsync(log, "Succeeded", null, null, provider, model, fallbackTrace, usage, cancellationToken, result, response.Content,
                    profile.OutputOptions?.ResponseFormat, stopwatch);
                await PublishCallbackAsync(business, normalized, response, cancellationToken);
                return response;
            }
            catch (Exception ex)
            {
                await quotaService.CancelAsync(reservation, cancellationToken);
                var errorCategory = ClassifyError(ex);
                fallbackTrace.Add($"{route.ProviderKey}:{errorCategory}");
                logger.LogWarning(ex, "AI provider {ProviderKey} failed in release {ReleaseVersion}.", route.ProviderKey, snapshot.Version);
                if (!CanFallback(ex))
                {
                    var response = FailedResponse(normalized, snapshot.Version, ErrorCodeFromException(ex), SanitizeException(ex));
                    await CompleteLogAsync(log, "Failed", response.ErrorCode, response.ErrorMessage, provider, model, fallbackTrace, new AiUsage(),
                        cancellationToken, errorCategory: errorCategory, outputFormat: profile.OutputOptions?.ResponseFormat, stopwatch: stopwatch);
                    return response;
                }
            }
        }

        var finalError = FailedResponse(normalized, snapshot.Version, AiErrorCodes.ProviderUnavailable, "All AI providers are unavailable.");
        await CompleteLogAsync(log, "Failed", finalError.ErrorCode, finalError.ErrorMessage, null, null, fallbackTrace, new AiUsage(), cancellationToken,
            stopwatch: stopwatch);
        return finalError;
    }

    /// <summary>
    /// 执行流式 AI 调用。
    /// </summary>
    public async IAsyncEnumerable<AiStreamEvent> StreamAsync(AiBusinessRequest request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeRequest(request);
        var setup = await ResolveSetupAsync(normalized, streaming: true, cancellationToken);
        var log = await CreateLogAsync(normalized, setup.ReleaseVersion, streaming: true, setup.ContextLength, cancellationToken);
        var sequence = 0;
        if (!setup.Success)
        {
            await CompleteLogAsync(log, "Failed", setup.ErrorCode, setup.ErrorMessage, null, null, [], new AiUsage(), cancellationToken);
            yield return new AiStreamEvent("error", normalized.InvocationId!, normalized.CorrelationId!, ++sequence, setup.ReleaseVersion,
                ErrorCode: setup.ErrorCode, ErrorMessage: setup.ErrorMessage);
            yield break;
        }

        var stopwatch = Stopwatch.StartNew();
        var fallbackTrace = new List<string>();
        foreach (var route in setup.Business!.Routes.OrderBy(r => r.Priority))
        {
            var provider = setup.Snapshot!.Providers.FirstOrDefault(p => string.Equals(p.ProviderKey, route.ProviderKey, StringComparison.Ordinal));
            var model = ResolveModel(normalized, route, provider);
            var profile = BuildCallProfile(setup.Business, normalized, route);
            var validation = ValidateRoute(setup.Business, provider, model, profile, normalized, streaming: true);
            if (validation.Error is not null)
            {
                fallbackTrace.Add($"{route.ProviderKey}:{validation.Error}");
                continue;
            }

            AiQuotaReservation reservation = new([]);
            AiUsage finalUsage = new();
            var emittedDelta = false;
            var emittedToClient = false;
            var completed = false;
            var shouldFallback = false;
            AiStreamEvent? errorEvent = null;
            IAsyncEnumerator<AiStreamEvent>? streamEnumerator = null;
            var referenceEvents = setup.References
                .Select(reference => new AiStreamEvent("reference", normalized.InvocationId!, normalized.CorrelationId!, 0, setup.Snapshot.Version,
                    ProviderKey: provider!.ProviderKey, Model: model, Reference: reference))
                .ToList();

            try
            {
                reservation = await quotaService.ReserveAsync(setup.Snapshot, provider!, model!, setup.Business, normalized.ProducerKey!, setup.ContextLength,
                    cancellationToken);
                var adapter = adapterResolver.Resolve(provider!.AdapterType);
                var apiKey = await ResolveProviderSecretAsync(provider, cancellationToken);
                streamEnumerator = adapter.StreamAsync(new AiProviderCallRequest(provider, model!, setup.Messages, true, normalized.InvocationId!,
                        normalized.CorrelationId!, setup.Snapshot.Version, apiKey, profile.OutputOptions, profile.ToolOptions,
                        profile.ProviderOptions, route.ProviderOptionsJson, route.OpenRouterOptionsJson), cancellationToken)
                    .GetAsyncEnumerator(cancellationToken);
            }
            catch (Exception ex)
            {
                await quotaService.CancelAsync(reservation, cancellationToken);
                var errorCategory = ClassifyError(ex);
                fallbackTrace.Add($"{route.ProviderKey}:{errorCategory}");
                logger.LogWarning(ex, "AI streaming provider {ProviderKey} failed in release {ReleaseVersion}.", route.ProviderKey, setup.Snapshot!.Version);
                if (CanFallback(ex))
                {
                    shouldFallback = true;
                }
                else
                {
                    await CompleteLogAsync(log, "Failed", ErrorCodeFromException(ex), SanitizeException(ex), provider, model, fallbackTrace, finalUsage,
                        cancellationToken, errorCategory: errorCategory, outputFormat: profile.OutputOptions?.ResponseFormat, stopwatch: stopwatch);
                    errorEvent = new AiStreamEvent("error", normalized.InvocationId!, normalized.CorrelationId!, ++sequence, setup.Snapshot!.Version,
                        ProviderKey: provider?.ProviderKey, Model: model, ErrorCode: ErrorCodeFromException(ex), ErrorMessage: SanitizeException(ex));
                }
            }

            if (shouldFallback)
            {
                continue;
            }

            if (errorEvent is not null)
            {
                yield return errorEvent;
                yield break;
            }

            while (streamEnumerator is not null)
            {
                AiStreamEvent item;
                try
                {
                    if (!await streamEnumerator.MoveNextAsync())
                    {
                        break;
                    }

                    item = streamEnumerator.Current;
                }
                catch (Exception ex)
                {
                    await quotaService.CancelAsync(reservation, cancellationToken);
                    var errorCategory = ClassifyError(ex);
                    fallbackTrace.Add($"{route.ProviderKey}:{errorCategory}");
                    logger.LogWarning(ex, "AI streaming provider {ProviderKey} failed in release {ReleaseVersion}.", route.ProviderKey,
                        setup.Snapshot!.Version);
                    if (!emittedToClient && !emittedDelta && CanFallback(ex))
                    {
                        // 流式调用尚未向客户端输出内容时才允许切换供应商，避免拼接多个模型的输出。
                        shouldFallback = true;
                    }
                    else
                    {
                        await CompleteLogAsync(log, "Failed", ErrorCodeFromException(ex), SanitizeException(ex), provider, model, fallbackTrace,
                            finalUsage, cancellationToken, errorCategory: errorCategory, outputFormat: profile.OutputOptions?.ResponseFormat,
                            stopwatch: stopwatch);
                        errorEvent = new AiStreamEvent("error", normalized.InvocationId!, normalized.CorrelationId!, ++sequence, setup.Snapshot!.Version,
                            ProviderKey: provider?.ProviderKey, Model: model, ErrorCode: ErrorCodeFromException(ex), ErrorMessage: SanitizeException(ex));
                    }

                    break;
                }

                if (!emittedToClient)
                {
                    // 引用信息在第一段模型输出前发送，便于前端先展示知识来源。
                    foreach (var referenceEvent in referenceEvents)
                    {
                        yield return referenceEvent with { Sequence = ++sequence };
                    }

                    emittedToClient = true;
                }

                emittedDelta |= item.Type is "delta" or "reasoning_delta";
                finalUsage = item.Usage ?? finalUsage;
                completed |= item.Type is "done" or "error";
                yield return item with
                {
                    InvocationId = normalized.InvocationId!,
                    CorrelationId = normalized.CorrelationId!,
                    Sequence = ++sequence,
                    ReleaseVersion = setup.Snapshot.Version
                };
            }

            if (streamEnumerator is not null)
            {
                await streamEnumerator.DisposeAsync();
            }

            if (shouldFallback)
            {
                continue;
            }

            if (errorEvent is not null)
            {
                yield return errorEvent;
                yield break;
            }

            finalUsage = ApplyPricing(finalUsage, validation.Model);
            await quotaService.CommitAsync(reservation, finalUsage, success: true, cancellationToken);
            await CompleteLogAsync(log, "Succeeded", null, null, provider, model, fallbackTrace, finalUsage, cancellationToken,
                outputFormat: profile.OutputOptions?.ResponseFormat, stopwatch: stopwatch);
            if (!emittedToClient)
            {
                foreach (var referenceEvent in referenceEvents)
                {
                    yield return referenceEvent with { Sequence = ++sequence };
                }
            }

            if (!completed)
            {
                // 供应商流结束但未显式 done 时补一个 done，统一前端收尾逻辑。
                yield return new AiStreamEvent("done", normalized.InvocationId!, normalized.CorrelationId!, ++sequence, setup.Snapshot.Version,
                    ProviderKey: provider!.ProviderKey, Model: model, Usage: finalUsage);
            }

            yield break;
        }

        await CompleteLogAsync(log, "Failed", AiErrorCodes.ProviderUnavailable, "All AI providers are unavailable.", null, null, fallbackTrace, new AiUsage(),
            cancellationToken, stopwatch: stopwatch);
        yield return new AiStreamEvent("error", normalized.InvocationId!, normalized.CorrelationId!, ++sequence, setup.Snapshot!.Version,
            ErrorCode: AiErrorCodes.ProviderUnavailable, ErrorMessage: "All AI providers are unavailable.");
    }

    /// <summary>
    /// 解析当前请求所需的发布快照、业务配置、消息上下文和引用。
    /// </summary>
    private async Task<ExecutionSetup> ResolveSetupAsync(AiBusinessRequest request, bool streaming, CancellationToken cancellationToken)
    {
        var snapshot = await configCache.GetCurrentAsync(cancellationToken);
        if (snapshot is null)
        {
            return ExecutionSetup.Failed(0, AiErrorCodes.ProviderUnavailable, "AI config has not been published.");
        }

        var business = snapshot.Businesses.FirstOrDefault(b => string.Equals(b.BusinessKey, request.BusinessKey, StringComparison.Ordinal));
        if (business is null)
        {
            return ExecutionSetup.Failed(snapshot.Version, AiErrorCodes.BusinessNotFound, $"AI business '{request.BusinessKey}' was not found.");
        }

        if (!ProducerAllowed(business, request.ProducerKey!))
        {
            return ExecutionSetup.Failed(snapshot.Version, AiErrorCodes.ProducerNotAllowed, "Producer is not allowed by current AI business.");
        }

        if (streaming && !business.EnableStreaming)
        {
            return ExecutionSetup.Failed(snapshot.Version, AiErrorCodes.StreamingNotEnabled, $"AI business '{request.BusinessKey}' does not enable streaming.");
        }

        if (!business.AllowModelOverride && !string.IsNullOrWhiteSpace(request.Model))
        {
            return ExecutionSetup.Failed(snapshot.Version, AiErrorCodes.OverrideNotAllowed, "Model override is not allowed by current AI business.");
        }

        if (!business.AllowPromptOverride && !string.IsNullOrWhiteSpace(request.PromptOverride))
        {
            return ExecutionSetup.Failed(snapshot.Version, AiErrorCodes.OverrideNotAllowed, "Prompt override is not allowed by current AI business.");
        }

        if (!business.AllowProviderOptionsOverride && request.ProviderOptions is not null)
        {
            return ExecutionSetup.Failed(snapshot.Version, AiErrorCodes.OverrideNotAllowed, "Provider options override is not allowed by current AI business.");
        }

        var prompt = string.IsNullOrWhiteSpace(business.PromptKey)
            ? null
            : snapshot.Prompts.Where(p => string.Equals(p.PromptKey, business.PromptKey, StringComparison.Ordinal))
                .OrderByDescending(p => p.Version)
                .FirstOrDefault();
        var knowledgeKeys = SplitCsv(business.KnowledgeKeys).ToHashSet(StringComparer.Ordinal);
        var knowledgeBases = snapshot.KnowledgeBases.Where(k => knowledgeKeys.Contains(k.KnowledgeKey)).ToList();
        try
        {
            var messages = promptComposer.Compose(business, request, prompt, knowledgeBases, out var references, out var contextLength).ToList();
            if (business.MaxContextTokens > 0 && contextLength > business.MaxContextTokens)
            {
                if (string.Equals(business.ContextOverflowStrategy, "Truncate", StringComparison.OrdinalIgnoreCase))
                {
                    // 截断只删除非 system 消息，尽量保留安全约束和系统指令。
                    messages = TruncateMessages(messages, business.MaxContextTokens);
                    contextLength = EstimateTokens(messages);
                }

                if (contextLength > business.MaxContextTokens)
                {
                    return ExecutionSetup.Failed(snapshot.Version, AiErrorCodes.ContextTooLarge, "AI context exceeds business MaxContextTokens.");
                }
            }

            return new ExecutionSetup(true, snapshot.Version, snapshot, business, messages, references, contextLength);
        }
        catch (ValidationDomainException ex)
        {
            var errorCode = ex.Message.StartsWith("AI_PROMPT_VARIABLE_REQUIRED:", StringComparison.Ordinal)
                ? AiErrorCodes.InvalidRequest
                : ex.Message;
            return ExecutionSetup.Failed(snapshot.Version, errorCode, SanitizeException(ex));
        }
    }

    /// <summary>
    /// 根据幂等键查找已经完成或正在记录的调用。
    /// </summary>
    private async Task<AiInvocationLog?> FindExistingInvocationAsync(AiBusinessRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return null;
        }

        return await repository.GetInvocationByIdempotencyAsync(request.BusinessKey, request.ProducerKey!, request.IdempotencyKey, cancellationToken);
    }

    /// <summary>
    /// 创建调用日志。
    /// </summary>
    private async Task<AiInvocationLog> CreateLogAsync(AiBusinessRequest request, int releaseVersion, bool streaming, int contextLength,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        return await repository.InsertInvocationLogAsync(new AiInvocationLog
        {
            InvocationId = request.InvocationId!,
            CorrelationId = request.CorrelationId!,
            BusinessKey = request.BusinessKey,
            ProducerKey = request.ProducerKey!,
            IdempotencyKey = request.IdempotencyKey!,
            ReleaseVersion = releaseVersion,
            RequestedModel = request.Model,
            Streaming = streaming,
            Status = "Pending",
            ContextLength = contextLength,
            OutputFormat = request.OutputOptions?.ResponseFormat,
            RequestHash = Hash(JsonSerializer.Serialize(request, JsonOptions)),
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
    }

    /// <summary>
    /// 完成调用日志并写入供应商、用量、错误和响应摘要。
    /// </summary>
    private async Task CompleteLogAsync(
        AiInvocationLog log,
        string status,
        string? errorCode,
        string? errorMessage,
        AiProviderSnapshot? provider,
        string? model,
        IReadOnlyList<string> fallbackTrace,
        AiUsage usage,
        CancellationToken cancellationToken,
        AiProviderCallResult? result = null,
        string? responseContent = null,
        string? outputFormat = null,
        Stopwatch? stopwatch = null,
        string? errorCategory = null)
    {
        stopwatch?.Stop();
        log.Status = status;
        log.ErrorCode = errorCode;
        log.ErrorCategory = errorCategory;
        log.ErrorMessage = errorMessage;
        log.ProviderKey = provider?.ProviderKey;
        log.ActualModel = model;
        log.FallbackTrace = string.Join(" | ", fallbackTrace.Where(t => !string.IsNullOrWhiteSpace(t)));
        log.DurationMs = stopwatch is null ? log.DurationMs : (int)stopwatch.ElapsedMilliseconds;
        log.PromptTokens = usage.PromptTokens;
        log.CompletionTokens = usage.CompletionTokens;
        log.TotalTokens = usage.TotalTokens;
        log.ReasoningTokens = usage.ReasoningTokens;
        log.CachedTokens = usage.CachedTokens;
        log.NativePromptTokens = usage.NativePromptTokens;
        log.NativeCompletionTokens = usage.NativeCompletionTokens;
        log.Cost = usage.Cost;
        log.ProviderRequestId = result?.ProviderRequestId;
        log.FinishReason = result?.FinishReason;
        log.UpstreamProvider = result?.UpstreamProvider;
        log.ToolCallCount = result?.ToolCallCount ?? log.ToolCallCount;
        log.OutputFormat = outputFormat ?? log.OutputFormat;
        log.ResponseHash = string.IsNullOrEmpty(responseContent) ? null : Hash(responseContent);
        log.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.UpdateInvocationLogAsync(log, cancellationToken);
    }

    /// <summary>
    /// 按业务配置发布调用完成回调。
    /// </summary>
    private async Task PublishCallbackAsync(AiBusinessSnapshot business, AiBusinessRequest request, AiBusinessResponse response,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(business.CallbackTopic))
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(request.CallbackName) &&
            !string.Equals(request.CallbackName, business.CallbackTopic, StringComparison.Ordinal))
        {
            return;
        }

        await eventPublisher.PublishAsync(business.CallbackTopic, response, cancellationToken);
    }

    /// <summary>
    /// 执行供应商非流式调用并应用超时、重试和输出校验。
    /// </summary>
    private async Task<AiProviderCallResult> InvokeWithResilienceAsync(
        IAiProviderAdapter adapter,
        AiProviderCallRequest request,
        AiProviderSnapshot provider,
        AiOutputOptions? outputOptions,
        CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, provider.MaxRetryCount + 1);
        Exception? lastError = null;
        for (var attempt = 1; attempt <= attempts; attempt++)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (provider.TimeoutSeconds > 0)
            {
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(provider.TimeoutSeconds));
            }

            try
            {
                return await InvokeWithOutputValidationAsync(adapter, request, outputOptions, timeoutCts.Token);
            }
            catch (Exception ex) when (attempt < attempts && CanRetry(ex, cancellationToken))
            {
                lastError = ex;
                // 线性退避即可，供应商级熔断目前由配置和后续扩展承接。
                await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
            }
        }

        throw lastError ?? new ValidationDomainException(AiErrorCodes.ProviderUnavailable);
    }

    /// <summary>
    /// 执行调用并在需要时校验 JSON 输出格式。
    /// </summary>
    private static async Task<AiProviderCallResult> InvokeWithOutputValidationAsync(
        IAiProviderAdapter adapter,
        AiProviderCallRequest request,
        AiOutputOptions? outputOptions,
        CancellationToken cancellationToken)
    {
        var maxAttempts = outputOptions is { ValidateAndRetry: true } ? Math.Max(1, outputOptions.MaxRetryCount + 1) : 1;
        Exception? lastError = null;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            var result = await adapter.InvokeAsync(request, cancellationToken);
            if (!RequiresJsonValidation(outputOptions))
            {
                return result;
            }

            try
            {
                JsonDocument.Parse(result.Content);
                return result;
            }
            catch (JsonException ex)
            {
                lastError = ex;
            }
        }

        throw new ValidationDomainException(AiErrorCodes.InvalidRequest, errorMeta: lastError);
    }

    /// <summary>
    /// 解析供应商 API Key 明文。
    /// </summary>
    private async Task<string?> ResolveProviderSecretAsync(AiProviderSnapshot provider, CancellationToken cancellationToken) =>
        string.IsNullOrWhiteSpace(provider.SecretRef)
            ? null
            : await secretResolver.ResolveAsync(provider.SecretRef, cancellationToken);

    /// <summary>
    /// 补齐请求追踪、生产者和幂等字段。
    /// </summary>
    private static AiBusinessRequest NormalizeRequest(AiBusinessRequest request)
    {
        var invocationId = string.IsNullOrWhiteSpace(request.InvocationId) ? Guid.NewGuid().ToString("N") : request.InvocationId.Trim();
        var producerKey = string.IsNullOrWhiteSpace(request.ProducerKey) ? "unknown" : request.ProducerKey.Trim();
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? invocationId : request.IdempotencyKey.Trim();
        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId) ? invocationId : request.CorrelationId.Trim();
        return request with
        {
            InvocationId = invocationId,
            ProducerKey = producerKey,
            IdempotencyKey = idempotencyKey,
            CorrelationId = correlationId
        };
    }

    /// <summary>
    /// 从已有调用日志重建幂等响应。
    /// </summary>
    private static AiBusinessResponse FromExistingLog(AiInvocationLog log, IReadOnlyDictionary<string, string>? context) =>
        new(log.Status == "Succeeded", log.InvocationId, log.CorrelationId, log.Status, log.ReleaseVersion, ProviderKey: log.ProviderKey,
            Model: log.ActualModel, Usage: new AiUsage(log.PromptTokens, log.CompletionTokens, log.TotalTokens, log.ReasoningTokens, log.CachedTokens,
                log.NativePromptTokens, log.NativeCompletionTokens, log.Cost), Context: context, ErrorCode: log.ErrorCode, ErrorMessage: log.ErrorMessage);

    /// <summary>
    /// 构造失败响应。
    /// </summary>
    private static AiBusinessResponse FailedResponse(AiBusinessRequest request, int releaseVersion, string errorCode, string errorMessage) =>
        new(false, request.InvocationId!, request.CorrelationId!, "Failed", releaseVersion, Context: request.Context, ErrorCode: errorCode,
            ErrorMessage: errorMessage);

    /// <summary>
    /// 校验路由、供应商、模型和调用能力是否匹配。
    /// </summary>
    private static RouteValidation ValidateRoute(
        AiBusinessSnapshot business,
        AiProviderSnapshot? provider,
        string? model,
        CallProfile profile,
        AiBusinessRequest request,
        bool streaming)
    {
        if (provider is null)
        {
            return RouteValidation.Failed("provider not found");
        }

        if (streaming && !provider.SupportsStreaming)
        {
            return RouteValidation.Failed("streaming not supported");
        }

        if (string.IsNullOrWhiteSpace(model))
        {
            return RouteValidation.Failed(AiErrorCodes.ModelNotConfigured);
        }

        var configuredModel = provider.Models.FirstOrDefault(m => string.Equals(m.ModelName, model, StringComparison.OrdinalIgnoreCase));
        if (configuredModel is null)
        {
            return RouteValidation.Failed("model not configured for provider");
        }

        if (streaming && !configuredModel.SupportsStreaming)
        {
            return RouteValidation.Failed("model streaming not supported");
        }

        if (RequiresVision(request) && !configuredModel.SupportsVision)
        {
            return RouteValidation.Failed("model vision not supported");
        }

        if (profile.ToolOptions?.Tools is { Count: > 0 } && !configuredModel.SupportsTools)
        {
            return RouteValidation.Failed("model tools not supported");
        }

        if (profile.ToolOptions is not null && business.MaxToolRounds > 0 && profile.ToolOptions.MaxToolRounds > business.MaxToolRounds)
        {
            return RouteValidation.Failed("max tool rounds exceeded");
        }

        if (string.Equals(profile.OutputOptions?.ResponseFormat, "json_schema", StringComparison.OrdinalIgnoreCase) &&
            !configuredModel.SupportsStructuredOutput)
        {
            return RouteValidation.Failed("model structured output not supported");
        }

        if (string.Equals(profile.OutputOptions?.ResponseFormat, "json_object", StringComparison.OrdinalIgnoreCase) &&
            !configuredModel.SupportsJsonMode)
        {
            return RouteValidation.Failed("model json mode not supported");
        }

        return RouteValidation.Success(configuredModel);
    }

    /// <summary>
    /// 合成输出、工具和供应商调用选项。
    /// </summary>
    private static CallProfile BuildCallProfile(AiBusinessSnapshot business, AiBusinessRequest request, AiBusinessRouteSnapshot route)
    {
        var businessOutput = string.IsNullOrWhiteSpace(business.OutputFormat)
            ? null
            : new AiOutputOptions(business.OutputFormat, business.OutputJsonSchema, business.OutputStrict,
                business.OutputValidateAndRetry, business.OutputMaxRetryCount);
        var output = request.OutputOptions ?? businessOutput;
        var toolOptions = Deserialize<AiToolOptions>(business.ToolOptionsJson);
        if (toolOptions is not null && business.MaxToolRounds > 0 && toolOptions.MaxToolRounds <= 0)
        {
            toolOptions = toolOptions with { MaxToolRounds = business.MaxToolRounds };
        }

        var providerOptions = request.ProviderOptions ?? Deserialize<AiProviderOptions>(route.ProviderOptionsJson) ??
            Deserialize<AiProviderOptions>(business.ProviderOptionsJson);
        return new CallProfile(output, toolOptions, providerOptions);
    }

    /// <summary>
    /// 按请求覆盖、路由覆盖和供应商默认顺序解析模型名。
    /// </summary>
    private static string? ResolveModel(AiBusinessRequest request, AiBusinessRouteSnapshot route, AiProviderSnapshot? provider) =>
        FirstNotBlank(request.Model, route.ModelOverride, provider?.Models.FirstOrDefault(m => m.IsDefault)?.ModelName, provider?.Models.FirstOrDefault()?.ModelName);

    /// <summary>
    /// 按模型价格补全供应商未返回的成本。
    /// </summary>
    private static AiUsage ApplyPricing(AiUsage usage, AiProviderModelSnapshot? model)
    {
        if (model is null || usage.Cost > 0)
        {
            return usage;
        }

        var inputCost = usage.PromptTokens * (model.InputPrice ?? 0);
        var outputCost = usage.CompletionTokens * (model.OutputPrice ?? 0);
        var cachedCost = usage.CachedTokens * (model.CachedInputPrice ?? 0);
        var reasoningCost = usage.ReasoningTokens * (model.ReasoningPrice ?? 0);
        return usage with { Cost = inputCost + outputCost + cachedCost + reasoningCost };
    }

    /// <summary>
    /// 判断 Producer 是否允许调用当前业务。
    /// </summary>
    private static bool ProducerAllowed(AiBusinessSnapshot business, string producerKey)
    {
        var allowed = SplitCsv(business.AllowedProducerKeys).ToList();
        return allowed.Contains("*", StringComparer.Ordinal) || allowed.Contains(producerKey, StringComparer.Ordinal);
    }

    /// <summary>
    /// 拆分逗号分隔配置。
    /// </summary>
    private static IReadOnlyList<string> SplitCsv(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>
    /// 删除较早的非 system 消息直到上下文长度满足限制。
    /// </summary>
    private static List<AiMessage> TruncateMessages(IReadOnlyList<AiMessage> messages, int maxTokens)
    {
        var result = messages.ToList();
        while (EstimateTokens(result) > maxTokens && result.Count > 1)
        {
            var index = result.FindIndex(m => !string.Equals(m.Role, "system", StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                break;
            }

            result.RemoveAt(index);
        }

        return result;
    }

    /// <summary>
    /// 粗略估算消息列表 token 数。
    /// </summary>
    private static int EstimateTokens(IReadOnlyList<AiMessage> messages)
    {
        var chars = messages.Sum(message =>
            (message.Content?.Length ?? 0) + (message.Parts?.Sum(part => (part.Text?.Length ?? 0) + (part.ResultJson?.Length ?? 0)) ?? 0));
        return Math.Max(1, (int)Math.Ceiling(chars / 3.0));
    }

    /// <summary>
    /// 判断请求是否需要视觉或文件输入能力。
    /// </summary>
    private static bool RequiresVision(AiBusinessRequest request) =>
        request.Attachments is { Count: > 0 } ||
        request.Messages?.SelectMany(m => m.Parts ?? []).Any(p => p.Type is "image_url" or "file_uri" or "audio" or "video") == true;

    /// <summary>
    /// 判断是否需要对输出做 JSON 解析校验。
    /// </summary>
    private static bool RequiresJsonValidation(AiOutputOptions? options) =>
        options is { ValidateAndRetry: true } &&
        (string.Equals(options.ResponseFormat, "json_schema", StringComparison.OrdinalIgnoreCase) ||
         string.Equals(options.ResponseFormat, "json_object", StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// 反序列化可选 JSON 配置。
    /// </summary>
    private static T? Deserialize<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <summary>
    /// 返回第一个非空白字符串。
    /// </summary>
    private static string? FirstNotBlank(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    /// <summary>
    /// 判断当前异常是否可以在同一供应商内重试。
    /// </summary>
    private static bool CanRetry(Exception ex, CancellationToken cancellationToken) =>
        ex is AiProviderException { IsRecoverable: true } ||
        ex is OperationCanceledException && !cancellationToken.IsCancellationRequested;

    /// <summary>
    /// 判断当前异常是否允许回退到下一供应商路由。
    /// </summary>
    private static bool CanFallback(Exception ex) =>
        ex is AiProviderException { IsRecoverable: true } ||
        ex is OperationCanceledException ||
        ex is HttpRequestException;

    /// <summary>
    /// 将异常映射为对调用方暴露的错误码。
    /// </summary>
    private static string ErrorCodeFromException(Exception ex) =>
        ex is ValidationDomainException { Message: AiErrorCodes.QuotaExceeded }
            ? AiErrorCodes.QuotaExceeded
            : ex is ValidationDomainException { Message: AiErrorCodes.InvalidRequest }
                ? AiErrorCodes.InvalidRequest
            : ex is ValidationDomainException { Message: AiErrorCodes.ProviderUnavailable }
                ? AiErrorCodes.ProviderUnavailable
            : ex is AiProviderException providerException
                ? $"AI_PROVIDER_{providerException.Category.ToUpperInvariant()}"
                : AiErrorCodes.ProviderUnavailable;

    /// <summary>
    /// 将异常归类为调用日志中的错误类别。
    /// </summary>
    private static string ClassifyError(Exception ex) =>
        ex is AiProviderException providerException ? providerException.Category :
        ex is OperationCanceledException ? "Timeout" :
        ex is ValidationDomainException { Message: AiErrorCodes.QuotaExceeded } ? "QuotaExceeded" :
        ex is ValidationDomainException { Message: AiErrorCodes.InvalidRequest } ? "InvalidRequest" :
        ex is ValidationDomainException { Message: AiErrorCodes.ProviderUnavailable } ? "ProviderUnavailable" :
        "Unknown";

    /// <summary>
    /// 清理异常消息，避免日志和响应携带过长或多行内容。
    /// </summary>
    private static string SanitizeException(Exception ex)
    {
        var message = ex.Message.ReplaceLineEndings(" ").Trim();
        return message.Length <= 500 ? message : message[..500];
    }

    /// <summary>
    /// 计算文本 SHA-256 哈希。
    /// </summary>
    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private sealed record ExecutionSetup(
        bool Success,
        int ReleaseVersion,
        AiConfigSnapshot? Snapshot,
        AiBusinessSnapshot? Business,
        IReadOnlyList<AiMessage> Messages,
        IReadOnlyList<AiReference> References,
        int ContextLength,
        string? ErrorCode = null,
        string? ErrorMessage = null)
    {
        public static ExecutionSetup Failed(int releaseVersion, string errorCode, string errorMessage) =>
            new(false, releaseVersion, null, null, [], [], 0, errorCode, errorMessage);
    }

    private sealed record CallProfile(AiOutputOptions? OutputOptions, AiToolOptions? ToolOptions, AiProviderOptions? ProviderOptions);

    private sealed record RouteValidation(string? Error, AiProviderModelSnapshot? Model)
    {
        public static RouteValidation Failed(string error) => new(error, null);

        public static RouteValidation Success(AiProviderModelSnapshot? model) => new(null, model);
    }
}
