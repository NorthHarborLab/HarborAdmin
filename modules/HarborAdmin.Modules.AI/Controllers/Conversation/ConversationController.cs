using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Application.Services.Conversation;
using HarborAdmin.Modules.AI.Contracts.Conversation.Dto;
using HarborAdmin.Modules.AI.Contracts.Conversation.Request;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.AI.Controllers.Conversation;

/// <summary>
/// AI 聊天会话 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/conversations")]
public sealed class ConversationController(ConversationService conversationService, ICurrentUser currentUser) : HarborControllerBase
{
    /// <summary>
    /// 分页列出当前用户会话。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<PagedResult<AiConversationDto>>> List(
        [FromQuery] AiConversationListQuery query,
        CancellationToken cancellationToken) =>
        await OkResultAsync(conversationService.ListAsync(currentUser.Id, query, cancellationToken));

    /// <summary>
    /// 获取会话详情。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResult<AiConversationDetailDto>> Get(long id, CancellationToken cancellationToken) =>
        await OkResultAsync(conversationService.GetDetailAsync(currentUser.Id, id, cancellationToken));

    /// <summary>
    /// 创建会话。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiConversationDetailDto>> Create(
        [FromBody] SaveAiConversationRequest request,
        CancellationToken cancellationToken) =>
        await OkResultAsync(conversationService.CreateAsync(currentUser.Id, request, cancellationToken));

    /// <summary>
    /// 更新会话设置。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiConversationDetailDto>> Update(
        long id,
        [FromBody] SaveAiConversationRequest request,
        CancellationToken cancellationToken) =>
        await OkResultAsync(conversationService.UpdateAsync(currentUser.Id, id, request, cancellationToken));

    /// <summary>
    /// 删除会话。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken) =>
        await DeleteResultAsync(id, cancellationToken, (conversationId, token) => conversationService.DeleteAsync(currentUser.Id, conversationId, token));
}
