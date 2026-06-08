using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Captcha;
using HarborAdmin.Modules.Admin.Application.Services.Auth;
using HarborAdmin.Modules.Admin.Application.Services.Access;
using HarborAdmin.Modules.Admin.Application.Services.Captcha;
using HarborAdmin.Modules.Admin.Application.Services.Dept;
using HarborAdmin.Modules.Admin.Application.Services.DynamicCrud;
using HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;
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
/// Admin 模块依赖注入扩展
/// </summary>
public static class AdminModuleExtensions
{
    /// <summary>
    /// 注册 Admin 模块服务
    /// </summary>
    public static IServiceCollection AddAdminModule(this IServiceCollection services)
    {
        AddAdminInfrastructure(services);
        AddAdminAuth(services);
        AddAdminAccess(services);
        AddAdminSystemManagement(services);
        AddAdminFeatureDesign(services);
        AddAdminDynamicCrud(services);
        return services;
    }

    private static void AddAdminInfrastructure(IServiceCollection services)
    {
        services.AddOptions<AdminAuthOptions>().BindConfiguration(AdminAuthOptions.SectionName);
        services.AddSingleton<IAdminDbContext, AdminDbContext>();
        services.AddSingleton<IAdminRepository, FreeSqlAdminRepository>();
        services.AddSingleton<AdminTokenProtector>();
        services.AddSingleton<CaptchaImagePool>();
        services.AddSingleton<CaptchaChallengeService>();
        services.AddScoped<AdminServiceContext>();
        services.AddScoped<SystemServiceContext>();
        services.AddScoped<IAdminDynamicResourceHandlerResolver, AdminDynamicResourceHandlerResolver>();
    }

    private static void AddAdminAuth(IServiceCollection services)
    {
        services.AddScoped<AuthService>();
    }

    private static void AddAdminAccess(IServiceCollection services)
    {
        services.AddScoped<AccessCacheService>();
        services.AddScoped<IAdminPrincipalResolver, AdminPrincipalResolver>();
        services.AddScoped<IAdminApiAccessEvaluator, AdminApiAccessEvaluator>();
        services.AddScoped<AccessQueryService>();
        services.AddScoped<SessionService>();
        services.AddScoped<ApiAuthorizationService>();
        services.AddScoped<FieldPolicyService>();
    }

    private static void AddAdminSystemManagement(IServiceCollection services)
    {
        services.AddScoped<MenuService>();
        services.AddScoped<DeptService>();
        services.AddScoped<RoleService>();
        services.AddScoped<UserService>();
        services.AddScoped<CacheManagementService>();
    }

    private static void AddAdminFeatureDesign(IServiceCollection services)
    {
        services.AddScoped<AdminMetadataService>();
        services.AddScoped<FeatureDesignServiceContext>();
        services.AddScoped<FeatureDesignFeatureService>();
        services.AddScoped<FeatureDesignFieldService>();
        services.AddScoped<FeatureDesignApiService>();
        services.AddScoped<FeatureDesignActionService>();
    }

    private static void AddAdminDynamicCrud(IServiceCollection services)
    {
        services.AddScoped<AdminDynamicCrudService>();
    }
}
