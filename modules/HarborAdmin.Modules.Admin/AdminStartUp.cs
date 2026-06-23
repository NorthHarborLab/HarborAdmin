using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Captcha;
using HarborAdmin.Modules.Admin.Application.Services.Auth;
using HarborAdmin.Modules.Admin.Application.Services.Access;
using HarborAdmin.Modules.Admin.Application.Services.Captcha;
using HarborAdmin.Modules.Admin.Application.Services.Dept;
using HarborAdmin.Modules.Admin.Application.Services.Dictionary;
using HarborAdmin.Modules.Admin.Application.Services.DynamicCrud;
using HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;
using HarborAdmin.Modules.Admin.Application.Services.JwtProfile;
using HarborAdmin.Modules.Admin.Application.Services.Menu;
using HarborAdmin.Modules.Admin.Application.Services.Metadata;
using HarborAdmin.Modules.Admin.Application.Services.Role;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Application.Services.System;
using HarborAdmin.Modules.Admin.Application.Services.User;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;
using HarborAdmin.Modules.Admin.Infrastructure.Options;
using HarborAdmin.Modules.Admin.Infrastructure.Repositories;
using HarborAdmin.Modules.Admin.Infrastructure.Resolvers;
using HarborAdmin.Modules.Admin.Infrastructure.Security;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Modules.Admin;

/// <summary>
/// Admin 模块启动入口。
/// </summary>
public sealed class AdminStartUp : HarborModuleMetadataBase, IHarborModuleStartup
{
    /// <inheritdoc />
    public override string ModuleName => "Admin";

    /// <inheritdoc />
    public override string GetDbKey() => "AdminDb";

    /// <summary>
    /// 注册 Admin 模块服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="context">模块注册上下文。</param>
    public void AddModule(IServiceCollection services, HarborModuleRegistrationContext context)
    {
        AddAdminInfrastructure(services);
        AddAdminAuth(services);
        AddAdminAccess(services);
        AddAdminSystemManagement(services);
        AddAdminDictionary(services);
        AddAdminFeatureDesign(services);
        AddAdminDynamicCrud(services);
    }

    /// <summary>
    /// 注册 Admin 基础设施。
    /// </summary>
    private static void AddAdminInfrastructure(IServiceCollection services)
    {
        services.AddOptions<AdminAuthOptions>().BindConfiguration(AdminAuthOptions.SectionName);
        services.AddSingleton<IAdminDbContext, AdminDbContext>();
        services.AddScoped<AdminServiceContext>();
        services.AddScoped<IAdminRuntimeStateRepository, AdminRuntimeStateRepository>();
        services.AddScoped<IAdminAuthRepository, AdminAuthRepository>();
        services.AddScoped<IAdminDictionaryRepository, AdminDictionaryRepository>();
        services.AddScoped<IAdminUserRepository, AdminUserRepository>();
        services.AddScoped<IAdminMenuRepository, AdminMenuRepository>();
        services.AddScoped<IAdminAccessRepository, AdminAccessRepository>();
        services.AddScoped<IAdminFeatureDesignRepository, AdminFeatureDesignRepository>();
        services.AddScoped<IAdminJwtProfileRepository, AdminJwtProfileRepository>();
        services.AddScoped<IAdminJwtRefreshTokenRepository, AdminJwtRefreshTokenRepository>();
        services.AddScoped<IAdminDepartmentRepository, AdminDepartmentRepository>();
        services.AddScoped<IAdminRoleRepository, AdminRoleRepository>();
        services.AddScoped<JwtProfileTokenService>();
        services.AddSingleton<CaptchaImagePool>();
        services.AddSingleton<CaptchaChallengeService>();
        services.AddScoped<SystemServiceContext>();
        services.AddScoped<IAdminDynamicResourceHandlerResolver, AdminDynamicResourceHandlerResolver>();
    }

    /// <summary>
    /// 注册匿名认证服务。
    /// </summary>
    private static void AddAdminAuth(IServiceCollection services)
    {
        services.AddScoped<AuthService>();
    }

    /// <summary>
    /// 注册访问控制服务。
    /// </summary>
    private static void AddAdminAccess(IServiceCollection services)
    {
        services.AddScoped<AccessCacheService>();
        services.AddScoped<IAdminApiAccessEvaluator, AdminApiAccessEvaluator>();
        services.AddScoped<AccessQueryService>();
        services.AddScoped<SessionService>();
        services.AddScoped<ApiAuthorizationService>();
        services.AddScoped<JwtProfileService>();
        services.AddScoped<FieldPolicyService>();
        services.AddScoped<AdminRuntimeAccessService>();
        services.AddScoped<AdminFieldProjectionService>();
        services.AddScoped<AdminFieldInputValidator>();
    }

    /// <summary>
    /// 注册系统管理服务。
    /// </summary>
    private static void AddAdminSystemManagement(IServiceCollection services)
    {
        services.AddScoped<MenuService>();
        services.AddScoped<DeptService>();
        services.AddScoped<RoleService>();
        services.AddScoped<UserService>();
        services.AddScoped<CacheManagementService>();
    }

    /// <summary>
    /// 注册字典服务。
    /// </summary>
    private static void AddAdminDictionary(IServiceCollection services)
    {
        services.AddScoped<AdminDictionaryService>();
        services.AddScoped<AdminFieldOptionResolver>();
    }

    /// <summary>
    /// 注册功能设计服务。
    /// </summary>
    private static void AddAdminFeatureDesign(IServiceCollection services)
    {
        services.AddScoped<AdminMetadataService>();
        services.AddScoped<FeatureDesignServiceContext>();
        services.AddScoped<FeatureDesignFeatureService>();
        services.AddScoped<FeatureDesignFieldService>();
        services.AddScoped<FeatureDesignApiService>();
        services.AddScoped<FeatureDesignActionService>();
    }

    /// <summary>
    /// 注册动态 CRUD 服务。
    /// </summary>
    private static void AddAdminDynamicCrud(IServiceCollection services)
    {
        services.AddScoped<AdminDynamicCrudService>();
        services.AddSingleton<IAdminDynamicResourceHandler, AuthDynamicTestResourceHandler>();
    }
}
