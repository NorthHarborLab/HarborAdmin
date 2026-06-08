using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Infrastructure.Contexts;
using HarborAdmin.Modules.International.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Modules.International;

/// <summary>
/// 国际化模块依赖注入扩展。
/// </summary>
public static class InternationalModuleExtensions
{
    /// <summary>
    /// 注册国际化模块服务。
    /// </summary>
    public static IServiceCollection AddInternationalModule(this IServiceCollection services)
    {
        services.AddSingleton<IInternationalDbContext, InternationalDbContext>();
        services.AddSingleton<IInternationalRepository, FreeSqlInternationalRepository>();
        services.AddScoped<InternationalCacheCoordinator>();
        services.AddScoped<InternationalPageService>();
        services.AddScoped<InternationalEntryService>();
        services.AddScoped<InternationalResourceBundleService>();
        services.AddScoped<InternationalTranslationService>();
        return services;
    }
}
