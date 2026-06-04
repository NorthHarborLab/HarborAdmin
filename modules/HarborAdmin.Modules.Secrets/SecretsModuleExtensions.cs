using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Modules.Secrets;

/// <summary>
/// Secrets 模块依赖注入扩展。
/// </summary>
public static class SecretsModuleExtensions
{
    /// <summary>
    /// 注册 Secrets 管理模块。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="configuration">配置根。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddSecretsModule(this IServiceCollection services, IConfiguration configuration)
    {
        return services;
    }
}
