using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Modules.International.Contracts.Dtos;
using HarborAdmin.Modules.International.Contracts.Requests;
using HarborAdmin.Client.AI.Invocation;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.International.Controllers;

/// <summary>
/// 前端国际化条目管理 API。
/// </summary>
[ApiController]
[Route("api/admin/international/pages")]
public sealed class InternationalEntriesController(
    InternationalEntryService entryService,
    InternationalTranslationService translationService) : ControllerBase
{
    /// <summary>
    /// 列出页面条目。
    /// </summary>
    [HttpGet("{pageId:long}/entries")]
    public async Task<ApiResult<IReadOnlyList<InternationalEntryDto>>> ListEntries(
        long pageId,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await entryService.ListEntriesAsync(pageId, cancellationToken));

    /// <summary>
    /// 创建页面条目。
    /// </summary>
    [HttpPost("{pageId:long}/entries")]
    public async Task<ApiResult<InternationalEntryDto>> CreateEntry(
        long pageId,
        [FromBody] CreateInternationalEntryRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await entryService.CreateEntryAsync(pageId, request, cancellationToken));

    /// <summary>
    /// 更新页面条目。
    /// </summary>
    [HttpPut("entries/{entryId:long}")]
    public async Task<ApiResult<InternationalEntryDto>> UpdateEntry(
        long entryId,
        [FromBody] UpdateInternationalEntryRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await entryService.UpdateEntryAsync(entryId, request, cancellationToken));

    /// <summary>
    /// 删除页面条目。
    /// </summary>
    [HttpDelete("entries/{entryId:long}")]
    public async Task<ApiResult<bool>> DeleteEntry(long entryId, CancellationToken cancellationToken)
    {
        await entryService.DeleteEntryAsync(entryId, cancellationToken);
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
        ApiResult.Ok(await translationService.TranslateEntryAsync(entryId, request, cancellationToken));
}
