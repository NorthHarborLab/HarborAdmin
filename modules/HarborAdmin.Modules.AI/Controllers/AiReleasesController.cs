using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.Modules.AI.Application.Services;
using HarborAdmin.Modules.AI.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.AI.Controllers;

/// <summary>
/// AI 配置发布 API。
/// </summary>
[ApiController]
[Route("api/admin/ai")]
public sealed class AiReleasesController(AiManagementService service) : ControllerBase
{
    /// <summary>
    /// 发布当前草稿。
    /// </summary>
    [HttpPost("publish")]
    public async Task<ActionResult<AiReleaseDto>> Publish([FromBody] PublishAiConfigRequest request, CancellationToken cancellationToken) =>
        Ok(await service.PublishAsync(request, cancellationToken));

    /// <summary>
    /// 列出发布历史。
    /// </summary>
    [HttpGet("releases")]
    public async Task<ActionResult<IReadOnlyList<AiReleaseDto>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListReleasesAsync(cancellationToken));

    /// <summary>
    /// 获取已发布快照。
    /// </summary>
    [HttpGet("published")]
    public async Task<ActionResult<ApiResult<AiPublishedSnapshotDto?>>> Published([FromQuery] int version, CancellationToken cancellationToken) =>
        Ok(ApiResult<AiPublishedSnapshotDto?>.Ok(await service.GetPublishedAsync(version, cancellationToken)));

    /// <summary>
    /// 回滚到指定版本。
    /// </summary>
    [HttpPost("releases/{version:int}/rollback")]
    public async Task<ActionResult<AiReleaseDto>> Rollback(int version, CancellationToken cancellationToken) =>
        Ok(await service.RollbackAsync(version, cancellationToken));
}
