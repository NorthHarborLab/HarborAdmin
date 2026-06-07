using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Modules.International.Contracts.Dtos;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.International.Controllers;

/// <summary>
/// 前端运行时 国际化资源 API
/// </summary>
[ApiController]
[Route("api/admin/international/resources")]
public sealed class InternationalResourcesController(InternationalService service) : ControllerBase
{
    /// <summary>
    /// 获取当前资源版本
    /// </summary>
    [HttpGet("version")]
    public async Task<ApiResult<InternationalVersionDto>> GetVersion(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.GetVersionAsync(cancellationToken));

    /// <summary>
    /// 获取带版本号的资源包
    /// </summary>
    [HttpGet("bundle")]
    public async Task<ApiResult<InternationalBundleDto>> GetBundle(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.GetBundleAsync(cancellationToken));

    /// <summary>
    /// 获取带版本号的单页面资源包
    /// </summary>
    [HttpGet("pages/{pageKey}/bundle")]
    public async Task<ApiResult<InternationalPageBundleDto>> GetPageBundle(
        string pageKey,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.GetPageBundleAsync(pageKey, cancellationToken));
}

