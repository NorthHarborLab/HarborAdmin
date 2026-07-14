using HarborAdmin.Modules.International.Application.Services;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.International.Contracts.Resource.Dto;
using HarborAdmin.Modules.International.Contracts.Page.Dto;
using Microsoft.AspNetCore.Authorization;

namespace HarborAdmin.Modules.International.Controllers.Resource;

/// <summary>
/// 前端运行时国际化资源 API。
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/admin/international/resources")]
public sealed class ResourceController(InternationalResourceBundleService resourceBundleService) : AdminControllerBase
{
    /// <summary>
    /// 获取当前资源版本。
    /// </summary>
    [HttpGet("version")]
    public async Task<ApiResult<InternationalVersionDto>> GetVersion(CancellationToken cancellationToken) =>
        ApiResult.Ok(await resourceBundleService.GetVersionAsync(cancellationToken));

    /// <summary>
    /// 获取带版本号的资源包。
    /// </summary>
    [HttpGet("bundle")]
    public async Task<ApiResult<InternationalBundleDto>> GetBundle(CancellationToken cancellationToken) =>
        ApiResult.Ok(await resourceBundleService.GetBundleAsync(cancellationToken));

    /// <summary>
    /// 获取带版本号的单页面资源包。
    /// </summary>
    [HttpGet("pages/bundle")]
    public async Task<ApiResult<InternationalPageBundleDto>> GetPageBundle([FromQuery] string path, CancellationToken cancellationToken) =>
        ApiResult.Ok(await resourceBundleService.GetPageBundleAsync(path, cancellationToken));

    /// <summary>
    /// 获取带版本号的单页面资源包。
    /// </summary>
    [HttpGet("pages/{pageKey}/bundle")]
    public async Task<ApiResult<InternationalPageBundleDto>> GetLegacyPageBundle(string pageKey, CancellationToken cancellationToken) =>
        ApiResult.Ok(await resourceBundleService.GetPageBundleAsync(pageKey, cancellationToken));
}
