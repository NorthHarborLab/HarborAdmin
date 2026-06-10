using System.Reflection;

namespace HarborAdmin.BuildingBlocks.Abstractions.Modules;

/// <summary>
/// Harbor 模块程序集发现器。
/// </summary>
public static class HarborModuleAssemblyDiscovery
{
    private const string ModuleAssemblyPrefix = "HarborAdmin.Modules.";

    /// <summary>
    /// 发现当前进程可用的 Harbor 模块程序集。
    /// </summary>
    /// <param name="additionalAssemblies">调用方显式追加的模块程序集。</param>
    /// <returns>去重后的模块程序集列表。</returns>
    public static IReadOnlyList<Assembly> Discover(IEnumerable<Assembly>? additionalAssemblies = null) =>
        DiscoverCore(additionalAssemblies)
            .GroupBy(assembly => assembly.GetName().Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

    /// <summary>
    /// 判断程序集是否为 Harbor 业务模块程序集。
    /// </summary>
    /// <param name="assembly">待判断程序集。</param>
    /// <returns>是否为模块程序集。</returns>
    public static bool IsModuleAssembly(Assembly assembly) =>
        IsModuleAssemblyName(assembly.GetName());

    /// <summary>
    /// 判断程序集名称是否为 Harbor 业务模块程序集名称。
    /// </summary>
    /// <param name="assemblyName">待判断程序集名称。</param>
    /// <returns>是否为模块程序集名称。</returns>
    public static bool IsModuleAssemblyName(AssemblyName assemblyName) =>
        assemblyName.Name?.StartsWith(ModuleAssemblyPrefix, StringComparison.OrdinalIgnoreCase) == true;

    /// <summary>
    /// 发现模块程序集，顺序为显式追加、已加载程序集、入口程序集引用。
    /// </summary>
    private static IEnumerable<Assembly> DiscoverCore(IEnumerable<Assembly>? additionalAssemblies)
    {
        var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in additionalAssemblies ?? [])
        {
            if (IsModuleAssembly(assembly) && loadedNames.Add(assembly.GetName().Name!))
            {
                yield return assembly;
            }
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(IsModuleAssembly))
        {
            if (loadedNames.Add(assembly.GetName().Name!))
            {
                yield return assembly;
            }
        }

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
        {
            yield break;
        }

        foreach (var reference in entryAssembly.GetReferencedAssemblies().Where(IsModuleAssemblyName))
        {
            if (!loadedNames.Add(reference.Name!))
            {
                continue;
            }

            // 模块程序集可能尚未被 CLR 加载；主动加载后调用方才能反射扫描实体或 Controller。
            yield return Assembly.Load(reference);
        }
    }
}