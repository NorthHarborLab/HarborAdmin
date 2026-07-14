using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.Modules.Admin.Application.Services.Access;
using HarborAdmin.Modules.Admin.Application.Services.Metadata;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Results;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Dto;

namespace HarborAdmin.Modules.Admin.Controllers.Metadata;

/// <summary>
/// Admin 动态 Feature schema 查询 API。
/// </summary>
[ApiController]
[Route("api/admin/features")]
public sealed class MetadataController(
    AdminMetadataService service,
    AdminRuntimeAccessService accessService,
    ICurrentUser currentUser) : AdminControllerBase
{
    /// <summary>
    /// 获取已注册错误码目录。
    /// </summary>
    [HttpGet("~/api/admin/metadata/error-codes")]
    [AuthenticatedOnly]
    public ApiResult<IReadOnlyList<HarborErrorDefinition>> GetErrorCodes() =>
        ApiResult.Ok(HarborErrorCatalog.Discover(AppDomain.CurrentDomain.GetAssemblies()));

    /// <summary>
    /// 获取指定动态 Feature schema。
    /// </summary>
    [HttpGet("{featureCode}/schema")]
    [AuthenticatedOnly]
    public async Task<ApiResult<DynamicViewSchemaDto>> GetSchema(
        string featureCode,
        CancellationToken cancellationToken)
    {
        var accessSet = currentUser.Id <= 0
            ? new AdminFieldPermissionSet(false, new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase), new HashSet<string>(StringComparer.OrdinalIgnoreCase))
            : await accessService.GetFieldPermissionsAsync(currentUser.Id, featureCode, AdminFieldSurface.Detail, cancellationToken);
        return ApiResult.Ok(await service.GetSchemaAsync(featureCode, accessSet, cancellationToken));
    }
}
