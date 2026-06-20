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
public sealed class PageController(InternationalPageService pageService) : HarborControllerBase
{
    /// <summary>
    /// 列出页面。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<InternationalPageDto>>> List(CancellationToken cancellationToken) =>
        await ListResultAsync(cancellationToken, pageService.ListPagesAsync);

    /// <summary>
    /// 列出页面分组树。
    /// </summary>
    [HttpGet("tree")]
    public async Task<ApiResult<IReadOnlyList<InternationalGroupNodeDto>>> ListTree(CancellationToken cancellationToken) =>
        await ListResultAsync(cancellationToken, pageService.ListPageTreeAsync);

    /// <summary>
    /// 创建页面。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<InternationalPageDto>> Create([FromBody] SaveInternationalPageRequest request, CancellationToken cancellationToken) =>
        await CreateResultAsync<SaveInternationalPageRequest, InternationalPageDto>(request, cancellationToken, (body, token) => pageService.SavePageAsync(null, body, token));

    /// <summary>
    /// 更新页面。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<InternationalPageDto>> Update(long id, [FromBody] SaveInternationalPageRequest request, CancellationToken cancellationToken) =>
        await UpdateResultAsync<long, SaveInternationalPageRequest, InternationalPageDto>(id, request, cancellationToken, (pageId, body, token) => pageService.SavePageAsync(pageId, body, token));

    /// <summary>
    /// 删除页面。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken) =>
        await DeleteResultAsync(id, cancellationToken, pageService.DeletePageAsync);

    /// <summary>
    /// 发布页面版本。
    /// </summary>
    [HttpPost("{id:long}/publish")]
    public async Task<ApiResult<InternationalPageDto>> PublishVersion(long id, CancellationToken cancellationToken) =>
        await OkResultAsync(pageService.PublishPageVersionAsync(id, cancellationToken));
}
