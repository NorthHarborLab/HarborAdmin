using HarborAdmin.Modules.International.Application;
using HarborAdmin.Modules.International.Contracts;
using HarborAdmin.Modules.International.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Modules.International;

/// <summary>
/// 国际化模块依赖注入扩展
/// </summary>
public static class InternationalModuleExtensions
{
    /// <summary>
    /// 注册 国际化模块服务
    /// </summary>
    public static IServiceCollection AddInternationalModule(this IServiceCollection services)
    {
        services.AddSingleton<IInternationalDbContext, InternationalDbContext>();
        services.AddSingleton<IInternationalRepository, FreeSqlInternationalRepository>();
        services.AddScoped<InternationalService>();
        return services;
    }
}
