using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;

namespace HarborAdmin.BuildingBlocks.AspNetCore.Controllers;

/// <summary>
/// 后台管理 API Controller 基类。
/// </summary>
[JwtTokenProfile(JwtTokenProfileKeys.Admin)]
public abstract class AdminControllerBase : HarborControllerBase;
