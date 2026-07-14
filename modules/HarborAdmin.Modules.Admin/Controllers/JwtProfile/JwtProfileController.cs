using System.ComponentModel.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.AspNetCore.Controllers;
using HarborAdmin.Modules.Admin.Application.Services.JwtProfile;
using HarborAdmin.Modules.Admin.Contracts.JwtProfile.Dto;
using HarborAdmin.Modules.Admin.Contracts.JwtProfile.Request;
using Microsoft.AspNetCore.Mvc;

namespace HarborAdmin.Modules.Admin.Controllers.JwtProfile;

/// <summary>
/// JWT Profile 管理。
/// </summary>
[ApiController]
[Route("api/admin/jwt-profiles")]
public sealed class JwtProfileController(JwtProfileService jwtProfileService) : AdminControllerBase
{
    /// <summary>
    /// 查询 JWT Profile 列表。
    /// </summary>
    [HttpGet]
    public async Task<ApiResult<IReadOnlyList<JwtProfileDto>>> List(CancellationToken cancellationToken) =>
        ApiResult.Ok(await jwtProfileService.ListAsync(cancellationToken));

    /// <summary>
    /// 保存 JWT Profile。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<JwtProfileDto>> Save(
        [FromBody] SaveJwtProfileRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await jwtProfileService.SaveAsync(null, request, cancellationToken));

    /// <summary>
    /// 轮换 JWT Profile 签名密钥。
    /// </summary>
    [HttpPost("{profileKey}/rotate-secret")]
    public async Task<ApiResult<RotateJwtProfileSecretResultDto>> RotateSecret(
        [FromRoute, Required] string profileKey,
        [FromBody] RotateJwtProfileSecretRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await jwtProfileService.RotateSecretAsync(profileKey, request, cancellationToken));

    /// <summary>
    /// 设置 JWT Profile 启停状态。
    /// </summary>
    [HttpPut("{profileKey}/enabled")]
    public async Task<ApiResult<JwtProfileDto>> SetEnabled(
        [FromRoute, Required] string profileKey,
        [FromBody] SetJwtProfileEnabledRequest request,
        CancellationToken cancellationToken) =>
        ApiResult.Ok(await jwtProfileService.SetEnabledAsync(profileKey, request.Enabled, cancellationToken));
}
