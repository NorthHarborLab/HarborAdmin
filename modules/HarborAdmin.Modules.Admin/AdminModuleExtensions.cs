using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Application.Captcha;
using HarborAdmin.Modules.Admin.Application.Services.Auth;
using HarborAdmin.Modules.Admin.Application.Services.Authorization;
using HarborAdmin.Modules.Admin.Application.Services.Captcha;
using HarborAdmin.Modules.Admin.Application.Services.Dept;
using HarborAdmin.Modules.Admin.Application.Services.DynamicCurd;
using HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;
using HarborAdmin.Modules.Admin.Application.Services.FieldPolicy;
using HarborAdmin.Modules.Admin.Application.Services.Menu;
using HarborAdmin.Modules.Admin.Application.Services.Metadata;
using HarborAdmin.Modules.Admin.Application.Services.Role;
using HarborAdmin.Modules.Admin.Application.Services.Session;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Application.Services.User;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;
using HarborAdmin.Modules.Admin.Infrastructure.Options;
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
        services.AddHttpContextAccessor();
        services.AddOptions<AdminAuthOptions>().BindConfiguration(AdminAuthOptions.SectionName);
        services.AddSingleton<IAdminDbContext, AdminDbContext>();
        services.AddSingleton<AdminTokenProtector>();
        services.AddSingleton<CaptchaImagePool>();
        services.AddSingleton<CaptchaChallengeService>();
        services.AddScoped<AuthService>();
        services.AddScoped<AdminServiceContext>();
        services.AddScoped<AccessQueryService>();
        services.AddScoped<SessionService>();
        services.AddScoped<ApiAuthorizationService>();
        services.AddScoped<MenuService>();
        services.AddScoped<DeptService>();
        services.AddScoped<RoleService>();
        services.AddScoped<UserService>();
        services.AddScoped<FieldPolicyService>();
        services.AddScoped<IAdminDynamicResourceHandlerResolver, AdminDynamicResourceHandlerResolver>();
        services.AddScoped<AdminMetadataService>();
        services.AddScoped<FeatureDesignServiceContext>();
        services.AddScoped<FeatureDesignFeatureService>();
        services.AddScoped<FeatureDesignFieldService>();
        services.AddScoped<FeatureDesignApiService>();
        services.AddScoped<FeatureDesignActionService>();
        services.AddScoped<AdminDynamicCrudService>();
        return services;
    }
}
