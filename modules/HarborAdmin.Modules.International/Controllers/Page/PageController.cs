using HarborAdmin.Modules.International.Application.Services;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.International.Contracts.Page.Dto;
using HarborAdmin.Modules.International.Contracts.Page.Request;

namespace HarborAdmin.Modules.International.Controllers.Page;

/// <summary>
/// 前端国际化页面管理 API。
/// </summary>
[ApiController]
[Route("api/admin/international/pages")]
public sealed class PageController(InternationalPageService pageService) : AdminControllerBase
{
    /// <summary>
    /// 列出页面。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<InternationalPageDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await pageService.ListPagesAsync(cancellationToken));

    /// <summary>
    /// 列出页面分组树。
    /// </summary>
    [HttpGet("tree")]
    public async Task<ApiResult<IReadOnlyList<InternationalGroupNodeDto>>> ListTree(CancellationToken cancellationToken) =>
        ApiResult.Ok(await pageService.ListPageTreeAsync(cancellationToken));

    /// <summary>
    /// 创建页面。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<InternationalPageDto>> Create([FromBody] SaveInternationalPageRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await pageService.SavePageAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新页面。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<InternationalPageDto>> Update(long id, [FromBody] SaveInternationalPageRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await pageService.SavePageAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除页面。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await pageService.DeletePageAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }

    /// <summary>
    /// 发布页面版本。
    /// </summary>
    [HttpPost("{id:long}/publish")]
    public async Task<ApiResult<InternationalPageDto>> PublishVersion(long id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await pageService.PublishPageVersionAsync(id, cancellationToken));
}
