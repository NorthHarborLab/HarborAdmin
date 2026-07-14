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
public sealed class GroupController(InternationalPageService pageService) : AdminControllerBase
{
    /// <summary>
    /// 创建资源分组。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<InternationalGroupNodeDto>> Create([FromBody] SaveInternationalGroupRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await pageService.SaveGroupAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新资源分组。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<InternationalGroupNodeDto>> Update(long id, [FromBody] SaveInternationalGroupRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await pageService.SaveGroupAsync(id, request, cancellationToken));
}
