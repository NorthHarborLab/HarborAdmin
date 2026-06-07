using HarborAdmin.Modules.Admin.Application.Services.Dept;
using HarborAdmin.Modules.Admin.Contracts.System;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.Admin.Controllers.System;

/// <summary>
/// 系统部门管理。
/// </summary>
[ApiController]
[Route("api/admin/system/dept")]
public sealed class DeptController(DeptService deptService) : ControllerBase
{
    /// <summary>
    /// 查询部门树。
    /// </summary>
    [HttpGet("list")]
    public async Task<ApiResult<IReadOnlyList<SystemDeptDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await deptService.ListDepartmentsAsync(cancellationToken));

    /// <summary>
    /// 创建部门。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SystemDeptDto>> Create([FromBody] SaveSystemDeptRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await deptService.SaveDepartmentAsync(null, request, cancellationToken));

    /// <summary>
    /// 更新部门。
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<ApiResult<SystemDeptDto>> Update(long id, [FromBody] SaveSystemDeptRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await deptService.SaveDepartmentAsync(id, request, cancellationToken));

    /// <summary>
    /// 删除部门。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await deptService.DeleteDepartmentAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }
}
