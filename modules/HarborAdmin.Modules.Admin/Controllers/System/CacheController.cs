using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.Modules.Admin.Application.Services.System;
using HarborAdmin.Modules.Admin.Contracts.System.Dto;
using HarborAdmin.Modules.Admin.Contracts.System.Request;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.Admin.Controllers.System;

/// <summary>
/// 系统缓存管理。
/// </summary>
[ApiController]
[Route("api/admin/system/cache")]
public sealed class CacheController(CacheManagementService cacheManagementService) : ControllerBase
{
    /// <summary>
    /// 获取缓存概览。
    /// </summary>
    [HttpGet("overview")]
    public async Task<ApiResult<CacheOverviewDto>> Overview(CancellationToken cancellationToken) =>
        ApiResult.Ok(await cacheManagementService.GetOverviewAsync(cancellationToken));

    /// <summary>
    /// 获取分组下运行时 tag 列表。
    /// </summary>
    [HttpGet("groups/tags")]
    public async Task<ApiResult<IReadOnlyList<CacheTagDto>>> GroupTags(
        [FromQuery] string groupPrefix,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await cacheManagementService.GetGroupTagsAsync(groupPrefix, cancellationToken));

    /// <summary>
    /// 获取 tag 下 key 列表。
    /// </summary>
    [HttpGet("tags/keys")]
    public async Task<ApiResult<IReadOnlyList<string>>> TagKeys(
        [FromQuery] string tag,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await cacheManagementService.GetTagKeysAsync(tag, cancellationToken));

    /// <summary>
    /// 查看 key 缓存内容。
    /// </summary>
    [HttpGet("keys/value")]
    public async Task<ApiResult<CacheEntryValueDto>> KeyValue([FromQuery] string key, CancellationToken cancellationToken) =>
        ApiResult.Ok(await cacheManagementService.GetKeyValueAsync(key, cancellationToken));

    /// <summary>
    /// 清理 tag。
    /// </summary>
    [HttpPost("tags/invalidate")]
    public async Task<ApiResult<bool>> InvalidateTag([FromQuery] string tag, CancellationToken cancellationToken)
    {
        await cacheManagementService.InvalidateTagAsync(tag, cancellationToken);
        return ApiResult.Ok(true);
    }

    /// <summary>
    /// 清理 key。
    /// </summary>
    [HttpPost("keys/invalidate")]
    public async Task<ApiResult<bool>> InvalidateKey([FromBody] InvalidateCacheKeyRequest request, CancellationToken cancellationToken)
    {
        await cacheManagementService.InvalidateKeyAsync(request.Key, cancellationToken);
        return ApiResult.Ok(true);
    }

    /// <summary>
    /// 清理整组。
    /// </summary>
    [HttpPost("groups/invalidate")]
    public async Task<ApiResult<bool>> InvalidateGroup([FromQuery] string groupPrefix, CancellationToken cancellationToken)
    {
        await cacheManagementService.InvalidateGroupAsync(groupPrefix, cancellationToken);
        return ApiResult.Ok(true);
    }
}
