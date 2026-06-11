using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Client.AI.Invocation;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.International.Contracts.Entry.Dto;
using HarborAdmin.Modules.International.Contracts.Entry.Request;

namespace HarborAdmin.Modules.International.Controllers.Entry;

/// <summary>
/// 前端国际化条目管理 API。
/// </summary>
[ApiController]
[Route("api/admin/international/pages")]
public sealed class EntryController(InternationalEntryService entryService, InternationalTranslationService translationService) : ControllerBase
{
    /// <summary>
    /// 列出页面条目。
    /// </summary>
    [HttpGet("{pageId:long}/entries")]
    public async Task<ApiResult<IReadOnlyList<InternationalEntryDto>>> ListEntries(long pageId) =>
        ApiResult.Ok(await entryService.ListEntriesAsync(pageId, HttpContext.RequestAborted));

    /// <summary>
    /// 创建页面条目。
    /// </summary>
    [HttpPost("{pageId:long}/entries")]
    public async Task<ApiResult<InternationalEntryDto>> CreateEntry(long pageId, [FromBody] SaveInternationalEntryRequest request) =>
        ApiResult.Ok(await entryService.SaveEntryAsync(request, pageId, null, HttpContext.RequestAborted));

    /// <summary>
    /// 更新页面条目。
    /// </summary>
    [HttpPut("entries/{entryId:long}")]
    public async Task<ApiResult<InternationalEntryDto>> UpdateEntry(long entryId, [FromBody] SaveInternationalEntryRequest request) =>
        ApiResult.Ok(await entryService.SaveEntryAsync(request, entryId: entryId, cancellationToken: HttpContext.RequestAborted));

    /// <summary>
    /// 删除页面条目。
    /// </summary>
    [HttpDelete("entries/{entryId:long}")]
    public async Task<ApiResult<bool>> DeleteEntry(long entryId)
    {
        await entryService.DeleteEntryAsync(entryId, HttpContext.RequestAborted);
        return ApiResult.Ok(true);
    }

    /// <summary>
    /// 请求 AI 翻译条目。
    /// </summary>
    [HttpPost("entries/{entryId:long}/translate")]
    public async Task<ApiResult<AiBusinessResponse>> TranslateEntry(long entryId, [FromBody] TranslateInternationalEntryRequest request) =>
        ApiResult.Ok(await translationService.TranslateEntryAsync(entryId, request, HttpContext.RequestAborted));
}