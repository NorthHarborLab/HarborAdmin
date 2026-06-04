using System.Reflection;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HarborAdmin.BuildingBlocks.Mapping;

/// <summary>
/// Harbor 映射服务注册扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    private static readonly Lock SyncRoot = new();
    private static readonly HashSet<string> ScannedAssemblies = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 注册 Harbor 框架映射能力，并扫描模块映射配置。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="assemblies">需要扫描 Mapster <see cref="IRegister" /> 配置的程序集；未传入时扫描当前已加载的 HarborAdmin 程序集</param>
    /// <returns>原服务集合</returns>
    public static IServiceCollection AddHarborMapping(this IServiceCollection services, params Assembly[] assemblies)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        var scanAssemblies = NormalizeAssemblies(assemblies);
        lock (SyncRoot)
        {
            // GlobalSettings 是进程级配置，重复扫描同一程序集会重复应用配置。
            var newAssemblies = scanAssemblies
                .Where(assembly => ScannedAssemblies.Add(ResolveAssemblyKey(assembly)))
                .ToArray();
            if (newAssemblies.Length > 0)
            {
                config.Scan(newAssemblies);
            }
        }

        services.TryAddSingleton(config);
        services.TryAddScoped<IHarborMapper, HarborMapper>();
        return services;
    }

    /// <summary>
    /// 规范化待扫描程序集列表。
    /// </summary>
    /// <param name="assemblies">调用方显式传入的程序集集合</param>
    /// <returns>去除动态程序集并按程序集标识去重后的程序集集合</returns>
    private static IReadOnlyList<Assembly> NormalizeAssemblies(IReadOnlyCollection<Assembly> assemblies) =>
        (assemblies.Count == 0 ? DiscoverHarborAssemblies() : assemblies)
        .Where(assembly => !assembly.IsDynamic)
        .GroupBy(ResolveAssemblyKey, StringComparer.OrdinalIgnoreCase)
        .Select(group => group.First())
        .ToArray();

    /// <summary>
    /// 发现当前进程中可用于映射扫描的 HarborAdmin 程序集。
    /// </summary>
    /// <returns>已加载或可从入口程序集引用中加载的 HarborAdmin 程序集集合</returns>
    private static IReadOnlyList<Assembly> DiscoverHarborAssemblies()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(IsHarborAssembly)
            .ToDictionary(assembly => assembly.GetName().Name!, StringComparer.OrdinalIgnoreCase);

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is not null)
        {
            foreach (var reference in entryAssembly.GetReferencedAssemblies().Where(IsHarborAssemblyName))
            {
                if (!loaded.ContainsKey(reference.Name!))
                {
                    loaded[reference.Name!] = Assembly.Load(reference);
                }
            }
        }

        return loaded.Values.ToArray();
    }

    /// <summary>
    /// 判断程序集是否属于 HarborAdmin。
    /// </summary>
    /// <param name="assembly">待判断的程序集</param>
    /// <returns>属于 HarborAdmin 程序集时返回 true</returns>
    private static bool IsHarborAssembly(Assembly assembly) =>
        IsHarborAssemblyName(assembly.GetName());

    /// <summary>
    /// 判断程序集名称是否属于 HarborAdmin。
    /// </summary>
    /// <param name="assemblyName">待判断的程序集名称</param>
    /// <returns>名称以 HarborAdmin. 开头时返回 true</returns>
    private static bool IsHarborAssemblyName(AssemblyName assemblyName) =>
        assemblyName.Name?.StartsWith("HarborAdmin.", StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// 解析用于去重的程序集标识。
    /// </summary>
    /// <param name="assembly">待解析的程序集</param>
    /// <returns>程序集完整名称、短名称或物理路径</returns>
    private static string ResolveAssemblyKey(Assembly assembly) =>
        assembly.FullName ?? assembly.GetName().Name ?? assembly.Location;
}
