using System.Reflection;

namespace HarborAdmin.Host.Infrastructure;

/// <summary>
/// MVC 模块控制器注册扩展。
/// </summary>
internal static class ModuleApplicationPartExtensions
{
    /// <summary>
    /// 将 Host 引用的 HarborAdmin 模块程序集自动注册为 MVC ApplicationPart。
    /// </summary>
    public static IMvcBuilder AddHarborModuleApplicationParts(this IMvcBuilder builder, IEnumerable<Assembly>? moduleAssemblies = null)
    {
        foreach (var assembly in moduleAssemblies ?? DiscoverHarborModuleAssemblies())
        {
            builder.AddApplicationPart(assembly);
        }

        return builder;
    }

    /// <summary>
    /// 发现入口程序集直接引用的 <c>HarborAdmin.Modules.*</c> 程序集。
    /// </summary>
    public static IReadOnlyList<Assembly> DiscoverHarborModuleAssemblies() =>
        DiscoverModuleAssemblies()
            .GroupBy(assembly => assembly.GetName().Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToArray();

    /// <summary>
    /// 发现入口程序集直接引用的 <c>HarborAdmin.Modules.*</c> 程序集。
    /// </summary>
    private static IEnumerable<Assembly> DiscoverModuleAssemblies()
    {
        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(IsModuleAssembly)
            .ToDictionary(assembly => assembly.GetName().Name!, StringComparer.OrdinalIgnoreCase);

        foreach (var assembly in loaded.Values)
        {
            yield return assembly;
        }

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly is null)
        {
            yield break;
        }

        foreach (var reference in entryAssembly.GetReferencedAssemblies().Where(IsModuleAssemblyName))
        {
            if (loaded.ContainsKey(reference.Name!))
            {
                continue;
            }

            // 模块程序集可能还没被 CLR 加载；主动加载后 MVC 才能发现其中的 Controller。
            yield return Assembly.Load(reference);
        }
    }

    /// <summary>
    /// 判断已加载程序集是否为 HarborAdmin 业务模块程序集。
    /// </summary>
    private static bool IsModuleAssembly(Assembly assembly) =>
        IsModuleAssemblyName(assembly.GetName());

    /// <summary>
    /// 判断程序集名称是否符合 HarborAdmin 业务模块命名约定。
    /// </summary>
    private static bool IsModuleAssemblyName(AssemblyName assemblyName) =>
        assemblyName.Name?.StartsWith("HarborAdmin.Modules.", StringComparison.OrdinalIgnoreCase) == true;
}
