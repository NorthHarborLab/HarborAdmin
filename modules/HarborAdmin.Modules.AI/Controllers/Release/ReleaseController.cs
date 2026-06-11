using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Application.Services.Release;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.Modules.AI.Contracts.Release.Dto;
using HarborAdmin.Modules.AI.Contracts.Release.Request;

namespace HarborAdmin.Modules.AI.Controllers.Release;

/// <summary>
/// AI 配置发布 API。
/// </summary>
[ApiController]
[Route("api/admin/ai")]
public sealed class ReleaseController(ReleaseService service) : ControllerBase
{
    /// <summary>
    /// 发布当前草稿。
    /// </summary>
    [HttpPost("publish")]
    public async Task<ApiResult<AiReleaseDto>> Publish([FromBody] PublishAiConfigRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.PublishAsync(request, cancellationToken));

    /// <summary>
    /// 列出发布历史。
    /// </summary>
    [HttpGet("releases")]
    public async Task<ApiResult<IReadOnlyList<AiReleaseDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListReleasesAsync(cancellationToken));

    /// <summary>
    /// 获取已发布快照。
    /// </summary>
    [HttpGet("published")]
    public async Task<ApiResult<AiPublishedSnapshotDto?>> Published([FromQuery] int version, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.GetPublishedAsync(version, cancellationToken));

    /// <summary>
    /// 回滚到指定版本。
    /// </summary>
    [HttpPost("releases/{version:int}/rollback")]
    public async Task<ApiResult<AiReleaseDto>> Rollback(int version, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.RollbackAsync(version, cancellationToken));
}
