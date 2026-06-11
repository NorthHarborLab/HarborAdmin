using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Application.Services.Shared;
using HarborAdmin.Modules.AI.Contracts.Conversation.Dto;
using HarborAdmin.Modules.AI.Contracts.Conversation.Request;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.Conversation;

/// <summary>
/// AI 聊天会话服务。
/// </summary>
public sealed class ConversationService(IAiConversationRepository repository)
{
    private const string DefaultTitle = "新对话";

    /// <summary>
    /// 分页列出当前用户会话。
    /// </summary>
    public async Task<PagedResult<AiConversationDto>> ListAsync(
        long userId,
        AiConversationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await repository.ListConversationsAsync(userId, query.Skip, query.PageSize, cancellationToken);
        var dtos = items.Select(MapToListDto).ToList();
        return PagedResult<AiConversationDto>.From(dtos, total > int.MaxValue ? int.MaxValue : (int)total);
    }

    /// <summary>
    /// 获取会话详情。
    /// </summary>
    public async Task<AiConversationDetailDto> GetDetailAsync(
        long userId,
        long conversationId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository.GetConversationDetailAsync(userId, conversationId, cancellationToken)
            ?? throw new NotFoundDomainException("会话不存在。");
        return MapToDetailDto(conversation);
    }

    /// <summary>
    /// 创建会话。
    /// </summary>
    public async Task<AiConversationDetailDto> CreateAsync(
        long userId,
        SaveAiConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var businessKey = NormalizeRequiredBusinessKey(request.BusinessKey);
        var now = DateTimeOffset.UtcNow;
        var conversation = new AiConversation
        {
            UserId = userId,
            Title = NormalizeOptional(request.Title) ?? DefaultTitle,
            BusinessKey = businessKey,
            ProducerKey = AiNormalizationHelper.NormalizeOptional(request.ProducerKey),
            Model = AiNormalizationHelper.NormalizeOptional(request.Model),
            PromptVariablesJson = NormalizePromptVariablesJson(request.PromptVariablesJson),
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = userId,
            UpdatedBy = userId,
        };

        await repository.InsertConversationAsync(conversation, cancellationToken);
        return MapToDetailDto(conversation);
    }

    /// <summary>
    /// 更新会话设置。
    /// </summary>
    public async Task<AiConversationDetailDto> UpdateAsync(
        long userId,
        long conversationId,
        SaveAiConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository.GetConversationDetailAsync(userId, conversationId, cancellationToken)
            ?? throw new NotFoundDomainException("会话不存在。");

        if (!string.IsNullOrWhiteSpace(request.Title))
        {
            conversation.Title = request.Title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(request.BusinessKey))
        {
            conversation.BusinessKey = AiNormalizationHelper.NormalizeKey(request.BusinessKey, nameof(request.BusinessKey));
        }

        if (request.ProducerKey is not null)
        {
            conversation.ProducerKey = AiNormalizationHelper.NormalizeOptional(request.ProducerKey);
        }

        if (request.Model is not null)
        {
            conversation.Model = AiNormalizationHelper.NormalizeOptional(request.Model);
        }

        if (request.PromptVariablesJson is not null)
        {
            conversation.PromptVariablesJson = NormalizePromptVariablesJson(request.PromptVariablesJson);
        }

        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        conversation.UpdatedBy = userId;
        await repository.UpdateConversationAsync(conversation, cancellationToken);
        return MapToDetailDto(conversation);
    }

    /// <summary>
    /// 删除会话。
    /// </summary>
    public async Task DeleteAsync(long userId, long conversationId, CancellationToken cancellationToken = default)
    {
        _ = await repository.GetConversationAsync(userId, conversationId, cancellationToken)
            ?? throw new NotFoundDomainException("会话不存在。");
        await repository.DeleteConversationAsync(userId, conversationId, cancellationToken);
    }

    /// <summary>
    /// 写入用户消息并更新会话元数据。
    /// </summary>
    public async Task AppendUserMessageAsync(
        long userId,
        long conversationId,
        string content,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository.GetConversationAsync(userId, conversationId, cancellationToken)
            ?? throw new NotFoundDomainException("会话不存在。");

        var trimmed = content.Trim();
        if (trimmed.Length == 0)
        {
            throw new ValidationDomainException("用户消息不能为空。");
        }

        var sequence = await repository.GetNextConversationMessageSequenceAsync(conversationId, cancellationToken);
        await repository.InsertConversationMessageAsync(
            new AiConversationMessage
            {
                ConversationId = conversationId,
                Role = "user",
                Content = trimmed,
                Sequence = sequence,
            },
            cancellationToken);

        if (string.Equals(conversation.Title, DefaultTitle, StringComparison.Ordinal))
        {
            conversation.Title = TrimTitle(trimmed);
        }

        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        conversation.UpdatedBy = userId;
        await repository.UpdateConversationAsync(conversation, cancellationToken);
    }

    /// <summary>
    /// 写入助手消息。
    /// </summary>
    public async Task AppendAssistantMessageAsync(
        long userId,
        long conversationId,
        string content,
        string? invocationId,
        bool isError,
        CancellationToken cancellationToken = default)
    {
        _ = await repository.GetConversationAsync(userId, conversationId, cancellationToken)
            ?? throw new NotFoundDomainException("会话不存在。");

        var sequence = await repository.GetNextConversationMessageSequenceAsync(conversationId, cancellationToken);
        await repository.InsertConversationMessageAsync(
            new AiConversationMessage
            {
                ConversationId = conversationId,
                Role = "assistant",
                Content = content,
                Sequence = sequence,
                InvocationId = invocationId,
                IsError = isError,
            },
            cancellationToken);

        var conversation = await repository.GetConversationAsync(userId, conversationId, cancellationToken);
        if (conversation is null)
        {
            return;
        }

        conversation.UpdatedAt = DateTimeOffset.UtcNow;
        conversation.UpdatedBy = userId;
        await repository.UpdateConversationAsync(conversation, cancellationToken);
    }

    /// <summary>
    /// 校验会话归属。
    /// </summary>
    public Task<AiConversation?> GetOwnedConversationAsync(
        long userId,
        long conversationId,
        CancellationToken cancellationToken = default) =>
        repository.GetConversationAsync(userId, conversationId, cancellationToken);

    /// <summary>
    /// 映射列表 DTO。
    /// </summary>
    private static AiConversationDto MapToListDto(AiConversation conversation) =>
        new(
            conversation.Id.ToString(),
            conversation.Title,
            conversation.BusinessKey,
            conversation.ProducerKey,
            conversation.Model,
            FormatTime(conversation.UpdatedAt ?? conversation.CreatedAt));

    /// <summary>
    /// 映射详情 DTO。
    /// </summary>
    private static AiConversationDetailDto MapToDetailDto(AiConversation conversation) =>
        new(
            conversation.Id.ToString(),
            conversation.Title,
            conversation.BusinessKey,
            conversation.ProducerKey,
            conversation.Model,
            conversation.PromptVariablesJson,
            FormatTime(conversation.UpdatedAt ?? conversation.CreatedAt),
            conversation.Messages
                .OrderBy(message => message.Sequence)
                .Select(message => new AiConversationMessageDto(
                    message.Id.ToString(),
                    message.Role,
                    message.Content,
                    message.Sequence,
                    message.InvocationId,
                    message.IsError))
                .ToList());

    /// <summary>
    /// 规范化业务 Key。
    /// </summary>
    private static string NormalizeRequiredBusinessKey(string? businessKey)
    {
        if (string.IsNullOrWhiteSpace(businessKey))
        {
            throw new ValidationDomainException("业务 Key 不能为空。");
        }

        return AiNormalizationHelper.NormalizeKey(businessKey, nameof(businessKey));
    }

    /// <summary>
    /// 规范化 Prompt 变量 JSON。
    /// </summary>
    private static string? NormalizePromptVariablesJson(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// 规范化可选字符串。
    /// </summary>
    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    /// <summary>
    /// 截取会话标题。
    /// </summary>
    private static string TrimTitle(string content) =>
        content.Length <= 50 ? content : $"{content[..50]}…";

    /// <summary>
    /// 格式化时间。
    /// </summary>
    private static string FormatTime(DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss");
}
