using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Infrastructure.Contexts;
using HarborAdmin.Modules.International.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Modules.International;

/// <summary>
/// 国际化模块启动入口。
/// </summary>
public sealed class InternationalStartUp : HarborModuleMetadataBase, IHarborModuleStartup
{
    /// <inheritdoc />
    public override string ModuleName => "International";

    /// <inheritdoc />
    public override string GetDbKey() => "AdminDb";

    /// <summary>
    /// 注册国际化模块服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="context">模块注册上下文。</param>
    public void AddModule(IServiceCollection services, HarborModuleRegistrationContext context)
    {
        services.AddHarborModuleData<IInternationalDbContext, InternationalDbContext, IInternationalRepository, FreeSqlInternationalRepository>();
        services.AddScoped<InternationalCacheCoordinator>();
        services.AddScoped<InternationalPageService>();
        services.AddScoped<InternationalEntryService>();
        services.AddScoped<InternationalResourceBundleService>();
        services.AddScoped<InternationalTranslationService>();
    }
}
