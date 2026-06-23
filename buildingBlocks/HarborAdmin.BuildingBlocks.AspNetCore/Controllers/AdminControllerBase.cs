using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;

namespace HarborAdmin.BuildingBlocks.AspNetCore.Controllers;

/// <summary>
/// 后台管理 API Controller 基类。
/// </summary>
[JwtTokenProfile(JwtTokenProfileKeys.Admin)]
public abstract class AdminControllerBase : HarborControllerBase;

/// <summary>
/// 后台管理 CRUD Controller 基类。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TQuery">分页查询请求类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
[JwtTokenProfile(JwtTokenProfileKeys.Admin)]
public abstract class AdminCrudControllerBase<TDto, TQuery, TSaveRequest> : HarborCrudControllerBase<TDto, TQuery, TSaveRequest>
    where TQuery : PageRequest;
