using HarborAdmin.Modules.AI.Application.Services.Prompt;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Contracts.Prompt.Dto;
using HarborAdmin.Modules.AI.Contracts.Prompt.Request;

namespace HarborAdmin.Modules.AI.Controllers.Prompt;

/// <summary>
/// AI Prompt 管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/prompts")]
public sealed class PromptController(PromptService service) : CrudControllerBase<AiPromptDto, PageRequest, SaveAiPromptRequest>
{
    /// <summary>
    /// 列出 Prompt。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<PagedResult<AiPromptDto>>> List([FromQuery] PageRequest query, CancellationToken cancellationToken) =>
        await PageResultAsync(query, service, cancellationToken);

    /// <summary>
    /// 获取 Prompt 详情。
    /// </summary>
    [HttpGet("{id:long}")]
    public async Task<ApiResult<AiPromptDto>> Get(long id, CancellationToken cancellationToken) =>
        await GetResultAsync(id, service, cancellationToken);

    /// <summary>
    /// 创建 Prompt。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiPromptDto>> Create([FromBody] SaveAiPromptRequest request, CancellationToken cancellationToken) =>
        await CreateResultAsync(request, service, cancellationToken);

    /// <summary>
    /// 更新 Prompt。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiPromptDto>> Update(long id, [FromBody] SaveAiPromptRequest request, CancellationToken cancellationToken) =>
        await UpdateResultAsync(id, request, service, cancellationToken);

    /// <summary>
    /// 删除 Prompt。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken) =>
        await DeleteResultAsync(id, service, cancellationToken);
}
