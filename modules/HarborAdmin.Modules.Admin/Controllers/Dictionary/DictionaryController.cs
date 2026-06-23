using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.Admin.Application.Services.Dictionary;
using HarborAdmin.Modules.Admin.Contracts.Dictionary.Dto;
using HarborAdmin.Modules.Admin.Contracts.Dictionary.Request;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.Admin.Controllers.Dictionary;

/// <summary>
/// Admin 字典 API。
/// </summary>
[ApiController]
[AuthenticatedOnly]
[Route("api/admin/dictionaries")]
public sealed class DictionaryController(AdminDictionaryService dictionaryService) : AdminControllerBase
{
    /// <summary>
    /// 查询字典类型。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<AdminDictionaryDto>>> ListDictionaries(
        [FromQuery] string? keyword,
        CancellationToken cancellationToken) =>
        await OkResultAsync(dictionaryService.ListDictionariesAsync(keyword, cancellationToken));

    /// <summary>
    /// 新建字典类型。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<AdminDictionaryDto>> CreateDictionary(
        [FromBody] SaveAdminDictionaryRequest request,
        CancellationToken cancellationToken) =>
        await CreateResultAsync<SaveAdminDictionaryRequest, AdminDictionaryDto>(request, cancellationToken, dictionaryService.CreateDictionaryAsync);

    /// <summary>
    /// 更新字典类型。
    /// </summary>
    [HttpPut("{dictCode}")]
    public async Task<ApiResult<AdminDictionaryDto>> UpdateDictionary(
        string dictCode,
        [FromBody] SaveAdminDictionaryRequest request,
        CancellationToken cancellationToken) =>
        await UpdateResultAsync<string, SaveAdminDictionaryRequest, AdminDictionaryDto>(dictCode, request, cancellationToken, dictionaryService.UpdateDictionaryAsync);

    /// <summary>
    /// 删除字典类型。
    /// </summary>
    [HttpDelete("{dictCode}")]
    public async Task<ApiResult<bool>> DeleteDictionary(string dictCode, CancellationToken cancellationToken)
    {
        await dictionaryService.DeleteDictionaryAsync(dictCode, cancellationToken);
        return OkResult(true);
    }

    /// <summary>
    /// 查询字典项。
    /// </summary>
    [HttpGet("{dictCode}/items")]
    public async Task<ApiResult<IReadOnlyList<AdminDictionaryItemDto>>> ListItems(
        string dictCode,
        CancellationToken cancellationToken) =>
        await OkResultAsync(dictionaryService.ListItemsAsync(dictCode, cancellationToken));

    /// <summary>
    /// 查询运行时字典选项。
    /// </summary>
    [HttpGet("{dictCode}/options")]
    public async Task<ApiResult<IReadOnlyList<AdminDictionaryOptionDto>>> ListOptions(
        string dictCode,
        [FromQuery] string? dataType,
        CancellationToken cancellationToken) =>
        await OkResultAsync(dictionaryService.ListOptionsAsync(dictCode, dataType, cancellationToken));

    /// <summary>
    /// 新建字典项。
    /// </summary>
    [HttpPost("{dictCode}/items")]
    public async Task<ApiResult<AdminDictionaryItemDto>> CreateItem(
        string dictCode,
        [FromBody] SaveAdminDictionaryItemRequest request,
        CancellationToken cancellationToken) =>
        await OkResultAsync(dictionaryService.CreateItemAsync(dictCode, request, cancellationToken));

    /// <summary>
    /// 更新字典项。
    /// </summary>
    [HttpPut("{dictCode}/items/{itemId:long}")]
    public async Task<ApiResult<AdminDictionaryItemDto>> UpdateItem(
        string dictCode,
        long itemId,
        [FromBody] SaveAdminDictionaryItemRequest request,
        CancellationToken cancellationToken) =>
        await OkResultAsync(dictionaryService.UpdateItemAsync(dictCode, itemId, request, cancellationToken));

    /// <summary>
    /// 删除字典项。
    /// </summary>
    [HttpDelete("{dictCode}/items/{itemId:long}")]
    public async Task<ApiResult<bool>> DeleteItem(
        string dictCode,
        long itemId,
        CancellationToken cancellationToken)
    {
        await dictionaryService.DeleteItemAsync(dictCode, itemId, cancellationToken);
        return OkResult(true);
    }
}
