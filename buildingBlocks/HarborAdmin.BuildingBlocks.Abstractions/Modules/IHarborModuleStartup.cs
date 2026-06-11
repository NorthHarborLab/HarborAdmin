using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.BuildingBlocks.Abstractions.Modules;

/// <summary>
/// Harbor 模块启动入口。
/// </summary>
public interface IHarborModuleStartup : IHarborModuleMetadata
{
    /// <summary>
    /// 注册模块服务。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="context">模块注册上下文。</param>
    void AddModule(IServiceCollection services, HarborModuleRegistrationContext context);
}
