using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.ConfigCenter.Contracts.Publish.Request;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.ConfigCenter.Controllers.Publish;

/// <summary>
/// 配置发布与发布历史查询 API。
/// </summary>
/// <param name="service">配置中心应用服务。</param>
[ApiController]
[Route("api/admin/config-center/{appId}")]
public sealed class PublishController(ConfigCenterPublishService service) : HarborControllerBase
{
    /// <summary>
    /// 列出发布历史（按版本降序）。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>发布记录列表。</returns>
    [HttpGet("releases")]
    public async Task<ApiResult<IReadOnlyList<ConfigReleaseDto>>> ListReleases(string appId, CancellationToken cancellationToken) =>
        await OkResultAsync(service.ListReleasesAsync(appId, cancellationToken));

    /// <summary>
    /// 发布当前草稿：写入发布快照并通过 TCP 通知 ConfigCenter 进程。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="request">发布请求（含发布人）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>发布结果（发布 ID 与版本号）。</returns>
    [HttpPost("publish")]
    public async Task<ApiResult<PublishConfigResult>> Publish(string appId, [FromBody] PublishConfigRequest request, CancellationToken cancellationToken) =>
        await OkResultAsync(service.PublishAsync(appId, request, cancellationToken));

    /// <summary>
    /// 获取已发布配置快照；<paramref name="version"/> 为 0 或未传时取最新版本。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="version">发布版本号，0 表示最新。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>快照；不存在时 404。</returns>
    [HttpGet("published")]
    public async Task<ApiResult<AdminPublishedConfigSnapshot>> GetPublished(string appId, [FromQuery] int version = 0,
        CancellationToken cancellationToken = default) =>
        await OkResultAsync(service.GetAdminPublishedSnapshotRequiredAsync(appId, version, cancellationToken));

    /// <summary>
    /// 按版本列出发布快照配置项。
    /// </summary>
    /// <param name="appId">应用标识。</param>
    /// <param name="version">发布版本号。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>快照配置项列表。</returns>
    [HttpGet("releases/{version:int}/items")]
    public async Task<ApiResult<IReadOnlyList<ConfigReleaseItemDto>>> ListReleaseItems(
        string appId,
        [FromRoute] int version,
        CancellationToken cancellationToken) =>
        await OkResultAsync(service.ListReleaseItemsByVersionAsync(appId, version, cancellationToken));
}
