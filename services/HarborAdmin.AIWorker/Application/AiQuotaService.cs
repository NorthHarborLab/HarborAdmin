using System.Data;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Client.AI.Constants;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Contracts.Shared.Snapshot;
using HarborAdmin.Modules.AI.Domain.Entities;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.AIWorker.Application;

/// <summary>
/// 基于原子窗口桶的 AI 配额服务。
/// </summary>
public sealed class AiQuotaService(IAiRepository repository, IAiDbContext dbContext, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager) : IAiQuotaService
{
    /// <inheritdoc />
    public async Task<AiQuotaReservation> ReserveAsync(
        AiConfigSnapshot snapshot,
        AiProviderSnapshot provider,
        string model,
        AiBusinessSnapshot business,
        string producerKey,
        int estimatedTokens,
        CancellationToken cancellationToken = default)
    {
        var rules = BuildRules(snapshot, provider.ProviderKey, model, business.BusinessKey, producerKey).ToList();
        if (rules.Count == 0)
        {
            return new AiQuotaReservation([]);
        }

        var refs = new List<AiQuotaBucketRef>();
        // Serializable 隔离级别保证并发预留时窗口桶计数不会被突破。
        using var uow = unitOfWorkManager.Begin(entityRegistry.GetDbKey<AiQuotaBucket>(), isolationLevel: IsolationLevel.Serializable);
        using (dbContext.Bind(uow.Orm))
        {
            foreach (var rule in rules)
            {
                foreach (var window in rule.Windows)
                {
                    var bucket = await GetOrCreateBucketAsync(rule, window.Type, window.Start, cancellationToken);
                    var requestCount = bucket.ReservedRequests + bucket.SuccessRequests + bucket.FailedRequests;
                    if (window.RequestLimit is > 0 && requestCount >= window.RequestLimit)
                    {
                        throw new ValidationDomainException(
                            AiErrorCodes.QuotaExceeded,
                            errorMeta: new { LimitType = "RequestCount", Limit = window.RequestLimit, Current = requestCount, WindowType = window.Type });
                    }

                    if (window.TokenLimit is > 0 && bucket.TotalTokens + estimatedTokens > window.TokenLimit)
                    {
                        throw new ValidationDomainException(
                            AiErrorCodes.QuotaExceeded,
                            errorMeta: new { LimitType = "Token", Limit = window.TokenLimit, Current = bucket.TotalTokens + estimatedTokens, WindowType = window.Type });
                    }

                    if (window.BudgetLimit is > 0 && bucket.Cost >= window.BudgetLimit)
                    {
                        throw new ValidationDomainException(
                            AiErrorCodes.QuotaExceeded,
                            errorMeta: new { LimitType = "Budget", Limit = window.BudgetLimit, Current = bucket.Cost, WindowType = window.Type });
                    }

                    // 预留阶段只占用请求名额；真实 token 和成本在调用完成后提交。
                    bucket.ReservedRequests += 1;
                    await repository.SaveQuotaBucketAsync(bucket, cancellationToken);
                    refs.Add(new AiQuotaBucketRef(bucket.ProviderKey, bucket.Model, bucket.BusinessKey, bucket.ProducerKey, bucket.WindowType, bucket.WindowStart));
                }
            }
        }

        uow.Commit();
        return new AiQuotaReservation(refs);
    }

    /// <inheritdoc />
    public Task CommitAsync(AiQuotaReservation reservation, AiUsage usage, bool success, CancellationToken cancellationToken = default) =>
        ApplyAsync(reservation, usage, success ? QuotaCompletion.Success : QuotaCompletion.Failed, cancellationToken);

    /// <inheritdoc />
    public Task CancelAsync(AiQuotaReservation reservation, CancellationToken cancellationToken = default) =>
        ApplyAsync(reservation, new AiUsage(), QuotaCompletion.Cancelled, cancellationToken);

    /// <summary>
    /// 将预留请求按成功、失败或取消状态落到窗口桶。
    /// </summary>
    private async Task ApplyAsync(AiQuotaReservation reservation, AiUsage usage, QuotaCompletion completion, CancellationToken cancellationToken)
    {
        if (reservation.Buckets.Count == 0)
        {
            return;
        }

        using var uow = unitOfWorkManager.Begin(entityRegistry.GetDbKey<AiQuotaBucket>(), isolationLevel: IsolationLevel.Serializable);
        using (dbContext.Bind(uow.Orm))
        {
            foreach (var bucketRef in reservation.Buckets)
            {
                var bucket = await repository.GetQuotaBucketAsync(bucketRef.ProviderKey, bucketRef.Model, bucketRef.BusinessKey, bucketRef.ProducerKey,
                    bucketRef.WindowType, bucketRef.WindowStart, cancellationToken);
                if (bucket is null)
                {
                    continue;
                }

                bucket.ReservedRequests = Math.Max(0, bucket.ReservedRequests - 1);
                if (completion == QuotaCompletion.Success)
                {
                    // 成功调用才累计 token 和成本；失败只累计失败次数，取消只释放预留。
                    bucket.SuccessRequests += 1;
                    bucket.TotalTokens += Math.Max(0, usage.TotalTokens);
                    bucket.Cost += Math.Max(0, usage.Cost);
                }
                else if (completion == QuotaCompletion.Failed)
                {
                    bucket.FailedRequests += 1;
                }

                await repository.SaveQuotaBucketAsync(bucket, cancellationToken);
            }
        }

        uow.Commit();
    }

    /// <summary>
    /// 获取或创建指定窗口桶。
    /// </summary>
    private async Task<AiQuotaBucket> GetOrCreateBucketAsync(QuotaRule rule, string windowType, DateTimeOffset windowStart,
        CancellationToken cancellationToken)
    {
        var bucket = await repository.GetQuotaBucketAsync(rule.ProviderKey, rule.Model, rule.BusinessKey, rule.ProducerKey, windowType, windowStart,
            cancellationToken);
        return bucket ?? new AiQuotaBucket
        {
            ProviderKey = rule.ProviderKey,
            Model = rule.Model,
            BusinessKey = rule.BusinessKey,
            ProducerKey = rule.ProducerKey,
            WindowType = windowType,
            WindowStart = windowStart
        };
    }

    /// <summary>
    /// 从发布快照中构建当前调用适用的配额规则。
    /// </summary>
    private static IEnumerable<QuotaRule> BuildRules(AiConfigSnapshot snapshot, string providerKey, string model, string businessKey, string producerKey)
    {
        foreach (var quota in snapshot.ProviderQuotas
                     .Where(q => Matches(q.ProviderKey, providerKey) && MatchesOptional(q.ProducerKey, producerKey)))
        {
            yield return new QuotaRule(providerKey, null, businessKey, producerKey, BuildWindows(
                quota.RequestsPerMinute, null, null,
                quota.RequestsPerDay, quota.TokensPerDay,
                quota.TokensPerMonth, quota.MonthlyBudget));
        }

        foreach (var quota in snapshot.ModelQuotas
                     .Where(q => Matches(q.ProviderKey, providerKey))
                     .Where(q => MatchesOptional(q.ModelName, model))
                     .Where(q => MatchesOptional(q.BusinessKey, businessKey))
                     .Where(q => MatchesOptional(q.ProducerKey, producerKey)))
        {
            yield return new QuotaRule(providerKey, string.IsNullOrWhiteSpace(quota.ModelName) ? model : quota.ModelName, businessKey, producerKey, BuildWindows(
                quota.RequestsPerMinute, quota.TokensPerMinute, null,
                quota.RequestsPerDay, quota.TokensPerDay,
                null, quota.MonthlyBudget));
        }
    }

    /// <summary>
    /// 根据限额配置构建分钟、天、月窗口。
    /// </summary>
    private static IReadOnlyList<QuotaWindow> BuildWindows(
        int? requestsPerMinute,
        int? tokensPerMinute,
        decimal? budgetPerMinute,
        int? requestsPerDay,
        int? tokensPerDay,
        int? tokensPerMonth,
        decimal? monthlyBudget)
    {
        var now = DateTimeOffset.UtcNow;
        var windows = new List<QuotaWindow>();
        if (requestsPerMinute is > 0 || tokensPerMinute is > 0 || budgetPerMinute is > 0)
        {
            windows.Add(new QuotaWindow("Minute", new DateTimeOffset(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, TimeSpan.Zero),
                requestsPerMinute, tokensPerMinute, budgetPerMinute));
        }

        if (requestsPerDay is > 0 || tokensPerDay is > 0)
        {
            windows.Add(new QuotaWindow("Day", new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero), requestsPerDay,
                tokensPerDay, null));
        }

        if (tokensPerMonth is > 0 || monthlyBudget is > 0)
        {
            windows.Add(new QuotaWindow("Month", new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero), null,
                tokensPerMonth, monthlyBudget));
        }

        return windows;
    }

    /// <summary>
    /// 判断必填维度是否匹配。
    /// </summary>
    private static bool Matches(string configured, string actual) =>
        string.Equals(configured, actual, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 判断可选维度是否匹配；空配置表示通配。
    /// </summary>
    private static bool MatchesOptional(string? configured, string actual) =>
        string.IsNullOrWhiteSpace(configured) || string.Equals(configured, actual, StringComparison.OrdinalIgnoreCase);

    private sealed record QuotaRule(string ProviderKey, string? Model, string BusinessKey, string ProducerKey, IReadOnlyList<QuotaWindow> Windows);

    private sealed record QuotaWindow(string Type, DateTimeOffset Start, int? RequestLimit, int? TokenLimit, decimal? BudgetLimit);

    private enum QuotaCompletion
    {
        Success,
        Failed,
        Cancelled
    }
}
