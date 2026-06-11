using HarborAdmin.Modules.AI.Application.Services.KnowledgeBase;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Contracts.KnowledgeBase.Dto;
using HarborAdmin.Modules.AI.Contracts.KnowledgeBase.Request;

namespace HarborAdmin.Modules.AI.Controllers.KnowledgeBase;

/// <summary>
/// AI 知识库管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/knowledge-bases")]
public sealed class KnowledgeBaseController(KnowledgeBaseService service) : CrudControllerBase<AiKnowledgeBaseDto, SaveAiKnowledgeBaseRequest>
{
    /// <summary>
    /// 列出知识库。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiKnowledgeBaseDto>>> List(CancellationToken cancellationToken) =>
        await ListResultAsync(service, cancellationToken);

    /// <summary>
    /// 获取知识库详情。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResult<AiKnowledgeBaseDto>> Get(long id, CancellationToken cancellationToken) =>
        await GetResultAsync(id, service, cancellationToken);

    /// <summary>
    /// 创建知识库。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiKnowledgeBaseDto>> Create([FromBody] SaveAiKnowledgeBaseRequest request, CancellationToken cancellationToken) =>
        await CreateResultAsync(request, service, cancellationToken);

    /// <summary>
    /// 更新知识库。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiKnowledgeBaseDto>> Update(long id, [FromBody] SaveAiKnowledgeBaseRequest request, CancellationToken cancellationToken) =>
        await UpdateResultAsync(id, request, service, cancellationToken);

    /// <summary>
    /// 删除知识库。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken) =>
        await DeleteResultAsync(id, service, cancellationToken);
}
