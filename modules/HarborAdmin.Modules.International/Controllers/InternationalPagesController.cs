using HarborAdmin.Modules.International.Application;
using HarborAdmin.Modules.International.Contracts;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<ActionResult<IReadOnlyList<InternationalPageDto>>> List(CancellationToken cancellationToken) =>
        Ok(await service.ListPagesAsync(cancellationToken));

    /// <summary>
    /// 创建页面
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<InternationalPageDto>> Create(
        [FromBody] CreateInternationalPageRequest request,
        CancellationToken cancellationToken)
    {
        var created = await service.CreatePageAsync(request, cancellationToken);
        return CreatedAtAction(nameof(List), null, created);
    }

    /// <summary>
    /// 更新页面
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ActionResult<InternationalPageDto>> Update(
        long id,
        [FromBody] UpdateInternationalPageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdatePageAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除页面
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id, CancellationToken cancellationToken)
    {
        await service.DeletePageAsync(id, cancellationToken);
        return Ok(true);
    }

    /// <summary>
    /// 发布页面版本
    /// </summary>
    [HttpPost("{id:long}/publish")]
    public async Task<ActionResult<InternationalPageDto>> PublishVersion(long id, CancellationToken cancellationToken) =>
        Ok(await service.PublishPageVersionAsync(id, cancellationToken));

    /// <summary>
    /// 列出页面条目
    /// </summary>
    [HttpGet("{pageId:long}/entries")]
    public async Task<ActionResult<IReadOnlyList<InternationalEntryDto>>> ListEntries(
        long pageId,
        CancellationToken cancellationToken) =>
        Ok(await service.ListEntriesAsync(pageId, cancellationToken));

    /// <summary>
    /// 创建页面条目
    /// </summary>
    [HttpPost("{pageId:long}/entries")]
    public async Task<ActionResult<InternationalEntryDto>> CreateEntry(
        long pageId,
        [FromBody] CreateInternationalEntryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.CreateEntryAsync(pageId, request, cancellationToken));

    /// <summary>
    /// 更新页面条目
    /// </summary>
    [HttpPut("entries/{entryId:long}")]
    public async Task<ActionResult<InternationalEntryDto>> UpdateEntry(
        long entryId,
        [FromBody] UpdateInternationalEntryRequest request,
        CancellationToken cancellationToken) =>
        Ok(await service.UpdateEntryAsync(entryId, request, cancellationToken));

    /// <summary>
    /// 删除页面条目
    /// </summary>
    [HttpDelete("entries/{entryId:long}")]
    public async Task<IActionResult> DeleteEntry(long entryId, CancellationToken cancellationToken)
    {
        await service.DeleteEntryAsync(entryId, cancellationToken);
        return Ok(true);
    }
}
