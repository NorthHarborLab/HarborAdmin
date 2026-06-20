using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Modules.International.Contracts.Page.Dto;
using HarborAdmin.Modules.International.Contracts.Page.Request;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.International.Controllers.Page;

/// <summary>
/// 前端国际化资源分组管理 API。
/// </summary>
[ApiController]
[Route("api/admin/international/groups")]
public sealed class GroupController(InternationalPageService pageService) : HarborControllerBase
{
    /// <summary>
    /// 创建资源分组。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<InternationalGroupNodeDto>> Create([FromBody] SaveInternationalGroupRequest request, CancellationToken cancellationToken) =>
        await CreateResultAsync<SaveInternationalGroupRequest, InternationalGroupNodeDto>(request, cancellationToken, (body, token) => pageService.SaveGroupAsync(null, body, token));

    /// <summary>
    /// 更新资源分组。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<InternationalGroupNodeDto>> Update(long id, [FromBody] SaveInternationalGroupRequest request, CancellationToken cancellationToken) =>
        await UpdateResultAsync<long, SaveInternationalGroupRequest, InternationalGroupNodeDto>(id, request, cancellationToken, (groupId, body, token) => pageService.SaveGroupAsync(groupId, body, token));
}
