using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 业务管理 API。
/// </summary>
[ApiController]
[Route("api/admin/ai/businesses")]
public sealed class AiBusinessesController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 列出业务。
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AiBusinessDto>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListBusinessesAsync(cancellationToken));

    /// <summary>
    /// 创建业务。
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<AiBusinessDto>> Create([FromBody] SaveAiBusinessRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SaveBusinessAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新业务。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<AiBusinessDto>> Update(long id, [FromBody] SaveAiBusinessRequest request, CancellationToken cancellationToken) =>
        Ok(await service.SaveBusinessAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除业务。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ActionResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeleteBusinessAsync(id, cancellationToken);
        return Ok(true);
    }
}


