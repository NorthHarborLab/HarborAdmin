using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 知识库管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/knowledge-bases")]
public sealed class AiKnowledgeBasesController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 列出知识库。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiKnowledgeBaseDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListKnowledgeBasesAsync(cancellationToken));

    /// <summary>
    /// 创建知识库。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiKnowledgeBaseDto>> Create([FromBody] SaveAiKnowledgeBaseRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SaveKnowledgeBaseAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新知识库。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiKnowledgeBaseDto>> Update(long id, [FromBody] SaveAiKnowledgeBaseRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SaveKnowledgeBaseAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除知识库。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteKnowledgeBaseAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }
}




