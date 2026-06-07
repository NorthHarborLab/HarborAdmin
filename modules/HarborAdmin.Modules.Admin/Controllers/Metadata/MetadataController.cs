using HarborAdmin.Modules.Admin.Application.Services.Access;
using HarborAdmin.Modules.Admin.Application.Services.Metadata;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Dtos;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using Microsoft.AspNetCore.Mvc;
using HarborAdmin.BuildingBlocks.Abstractions.Api;

namespace HarborAdmin.Modules.Admin.Controllers.Metadata;

/// <summary>
/// Admin 动态 Feature schema 查询 API。
/// </summary>
[ApiController]
[Route("api/admin/features")]
public sealed class MetadataController(
    AdminMetadataService service,
    FieldPolicyService fieldPolicyService,
    ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// 获取指定动态 Feature schema。
    /// </summary>
    [HttpGet("{featureCode}/schema")]
    public async Task<ApiResult<DynamicViewSchemaDto>> GetSchema(
        string featureCode,
        CancellationToken cancellationToken)
    {
        var policies = currentUser.Id <= 0
            ? []
            : await fieldPolicyService.GetFieldPoliciesForUserAsync(currentUser.Id, cancellationToken);
        return ApiResult.Ok(await service.GetSchemaAsync(featureCode, policies, cancellationToken));
    }
}
