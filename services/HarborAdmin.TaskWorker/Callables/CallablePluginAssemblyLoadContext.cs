using System.Reflection;
using System.Runtime.Loader;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;

namespace HarborAdmin.TaskWorker.Callables;

/// <summary>
/// Callable 插件程序集加载上下文
/// </summary>
internal sealed class CallablePluginAssemblyLoadContext(string mainAssemblyPath) : AssemblyLoadContext(isCollectible: true)
{
    private static readonly HashSet<string> SharedAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        typeof(ITaskCallableService).Assembly.GetName().Name!,
        "HarborAdmin.BuildingBlocks.Abstractions",
        "HarborAdmin.BuildingBlocks.Data",
        "Microsoft.Extensions.DependencyInjection.Abstractions",
        "Microsoft.Extensions.DependencyInjection",
    };

    private readonly AssemblyDependencyResolver _resolver = new(mainAssemblyPath);

    /// <inheritdoc />
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (IsSharedAssembly(assemblyName.Name))
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(item => string.Equals(item.GetName().Name, assemblyName.Name, StringComparison.OrdinalIgnoreCase));
        }

        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        return assemblyPath is null ? null : LoadFromAssemblyPath(assemblyPath);
    }

    /// <inheritdoc />
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath is null ? IntPtr.Zero : LoadUnmanagedDllFromPath(libraryPath);
    }

    /// <summary>
    /// 判断是否应复用默认上下文中的共享程序集
    /// </summary>
    /// <param name="assemblyName">程序集名称</param>
    /// <returns>是否共享程序集</returns>
    private static bool IsSharedAssembly(string? assemblyName) =>
        string.IsNullOrWhiteSpace(assemblyName)
        || assemblyName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
        || assemblyName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
        || SharedAssemblyNames.Contains(assemblyName);
}
