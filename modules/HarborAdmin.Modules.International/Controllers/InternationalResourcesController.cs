using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Modules.International.Contracts.Dtos;
using HarborAdmin.Modules.International.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<InternationalVersionDto>> GetVersion(CancellationToken cancellationToken) =>
        Ok(await service.GetVersionAsync(cancellationToken));

    /// <summary>
    /// 获取带版本号的资源包
    /// </summary>
    [HttpGet("bundle")]
    public async Task<ActionResult<InternationalBundleDto>> GetBundle(CancellationToken cancellationToken) =>
        Ok(await service.GetBundleAsync(cancellationToken));

    /// <summary>
    /// 获取带版本号的单页面资源包
    /// </summary>
    [HttpGet("pages/{pageKey}/bundle")]
    public async Task<ActionResult<InternationalPageBundleDto>> GetPageBundle(
        string pageKey,
        CancellationToken cancellationToken) =>
        Ok(await service.GetPageBundleAsync(pageKey, cancellationToken));
}
