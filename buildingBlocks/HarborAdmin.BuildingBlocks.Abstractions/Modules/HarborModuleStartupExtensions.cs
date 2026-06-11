using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.BuildingBlocks.Abstractions.Modules;

/// <summary>
/// Harbor 模块启动注册扩展。
/// </summary>
public static class HarborModuleStartupExtensions
{
    /// <summary>
    /// 扫描并注册 Harbor 模块。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="moduleAssemblies">模块程序集。</param>
    /// <param name="configuration">配置根。</param>
    /// <param name="hostKind">宿主类型。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddHarborModules(this IServiceCollection services, IEnumerable<Assembly>? moduleAssemblies, IConfiguration configuration,
        string hostKind = HarborHostKinds.Host)
    {
        var context = new HarborModuleRegistrationContext(configuration, hostKind);
        foreach (var startup in DiscoverStartups(moduleAssemblies))
        {
            startup.AddModule(services, context);
        }

        return services;
    }

    /// <summary>
    /// 发现模块启动入口。
    /// </summary>
    /// <param name="moduleAssemblies">模块程序集。</param>
    /// <returns>模块启动入口列表。</returns>
    public static IReadOnlyList<IHarborModuleStartup> DiscoverStartups(IEnumerable<Assembly>? moduleAssemblies = null)
    {
        var startups = new List<IHarborModuleStartup>();
        foreach (var assembly in HarborModuleAssemblyDiscovery.Discover(moduleAssemblies))
        {
            var startupTypes = GetLoadableTypes(assembly)
                .Where(type => type is { IsClass: true, IsAbstract: false } && typeof(IHarborModuleStartup).IsAssignableFrom(type))
                .ToArray();

            if (startupTypes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Module assembly '{assembly.GetName().Name}' must declare exactly one IHarborModuleStartup implementation.");
            }

            if (startupTypes.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Module assembly '{assembly.GetName().Name}' declares multiple IHarborModuleStartup implementations: {string.Join(", ", startupTypes.Select(type => type.FullName))}.");
            }

            var startup = Activator.CreateInstance(startupTypes[0]) as IHarborModuleStartup
                          ?? throw new InvalidOperationException(
                              $"Module startup '{startupTypes[0].FullName}' must provide a public parameterless constructor.");
            startups.Add(startup);
        }

        return startups;
    }

    /// <summary>
    /// 安全获取程序集类型列表。
    /// </summary>
    private static IReadOnlyList<Type> GetLoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type is not null).Cast<Type>().ToArray();
        }
    }
}