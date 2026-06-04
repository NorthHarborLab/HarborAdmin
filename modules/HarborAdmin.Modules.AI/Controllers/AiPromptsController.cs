using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI Prompt 管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/prompts")]
public sealed class AiPromptsController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 列出 Prompt。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AiPromptDto>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListPromptsAsync(cancellationToken));

    /// <summary>
    /// 创建 Prompt。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AiPromptDto>> Create([FromBody] SaveAiPromptRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SavePromptAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新 Prompt。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<AiPromptDto>> Update(long id, [FromBody] SaveAiPromptRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SavePromptAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除 Prompt。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeletePromptAsync(id, cancellationToken);
        return Ok(true);
    }
}


