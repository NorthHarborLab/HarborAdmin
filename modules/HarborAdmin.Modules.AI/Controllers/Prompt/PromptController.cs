using HarborAdmin.Modules.AI.Application.Services.Prompt;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.Modules.AI.Contracts.Prompt.Dto;
using HarborAdmin.Modules.AI.Contracts.Prompt.Request;

namespace HarborAdmin.Modules.AI.Controllers.Prompt;

/// <summary>
/// AI Prompt 管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/prompts")]
public sealed class PromptController(PromptService service) : ControllerBase
{
    /// <summary>
    /// 列出 Prompt。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AiPromptDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListPromptsAsync(cancellationToken));

    /// <summary>
    /// 创建 Prompt。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AiPromptDto>> Create([FromBody] SaveAiPromptRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SavePromptAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新 Prompt。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<AiPromptDto>> Update(long id, [FromBody] SaveAiPromptRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.SavePromptAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除 Prompt。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeletePromptAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }
}
