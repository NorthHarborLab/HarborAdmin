using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Modules.International.Contracts.Dtos;
using HarborAdmin.Modules.International.Contracts.Requests;
using HarborAdmin.Client.AI.Invocation;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.International.Controllers;

/// <summary>
/// 前端国际化页面与分块条目管理 API
/// </summary>
[ApiController]
[Route("api/admin/international/pages")]
public sealed class InternationalPagesController(InternationalService service) : ControllerBase
{
    /// <summary>
    /// 列出页面
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<InternationalPageDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListPagesAsync(cancellationToken));

    /// <summary>
    /// 创建页面
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<InternationalPageDto>> Create(
        [FromBody] CreateInternationalPageRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreatePageAsync(request, cancellationToken);
        return ApiResult.Ok(created);
    }

    /// <summary>
    /// 更新页面
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<InternationalPageDto>> Update(
        long id,
        [FromBody] UpdateInternationalPageRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.UpdatePageAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除页面
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeletePageAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }

    /// <summary>
    /// 发布页面版本
    /// </summary>
    [HttpPost("{id:long}/publish")]
    public async Task<ApiResult<InternationalPageDto>> PublishVersion(long id, CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.PublishPageVersionAsync(id, cancellationToken));

    /// <summary>
    /// 列出页面条目
    /// </summary>
    [HttpGet("{pageId:long}/entries")]
    public async Task<ApiResult<IReadOnlyList<InternationalEntryDto>>> ListEntries(
        long pageId,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.ListEntriesAsync(pageId, cancellationToken));

    /// <summary>
    /// 创建页面条目
    /// </summary>
    [HttpPost("{pageId:long}/entries")]
    public async Task<ApiResult<InternationalEntryDto>> CreateEntry(
        long pageId,
        [FromBody] CreateInternationalEntryRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.CreateEntryAsync(pageId, request, cancellationToken));

    /// <summary>
    /// 更新页面条目
    /// </summary>
    [HttpPut("entries/{entryId:long}")]
    public async Task<ApiResult<InternationalEntryDto>> UpdateEntry(
        long entryId,
        [FromBody] UpdateInternationalEntryRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.UpdateEntryAsync(entryId, request, cancellationToken));

    /// <summary>
    /// 删除页面条目
    /// </summary>
    [HttpDelete("entries/{entryId:long}")]
    public async Task<ApiResult<bool>> DeleteEntry(long entryId, CancellationToken cancellationToken)
    {
        await service.DeleteEntryAsync(entryId, cancellationToken);
        return ApiResult.Ok(true);
    }

    /// <summary>
    /// 请求 AI 翻译条目。
    /// </summary>
    [HttpPost("entries/{entryId:long}/translate")]
    public async Task<ApiResult<AiBusinessResponse>> TranslateEntry(
        long entryId,
        [FromBody] TranslateInternationalEntryRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await service.TranslateEntryAsync(entryId, request, cancellationToken));
}


