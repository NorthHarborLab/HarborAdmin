using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

namespace HarborAdmin.TaskWorker.Callables;

/// <summary>
/// Callable 插件目录快照
/// </summary>
internal sealed class CallablePluginCatalog(
    IReadOnlyDictionary<string, CallablePluginEntry> entries,
    IReadOnlyList<TaskCallableDescriptor> descriptors,
    IReadOnlyList<CallablePluginAssemblyLoadContext> loadContexts,
    string shadowDirectory) : IDisposable
{
    /// <summary>
    /// 空插件目录
    /// </summary>
    public static CallablePluginCatalog Empty { get; } = new(
        new Dictionary<string, CallablePluginEntry>(StringComparer.OrdinalIgnoreCase),
        [],
        [],
        string.Empty);

    /// <summary>
    /// 插件入口索引
    /// </summary>
    public IReadOnlyDictionary<string, CallablePluginEntry> Entries { get; } = entries;

    /// <summary>
    /// Callable 描述集合
    /// </summary>
    public IReadOnlyList<TaskCallableDescriptor> Descriptors { get; } = descriptors;

    /// <summary>
    /// 释放插件目录
    /// </summary>
    public void Dispose()
    {
        foreach (var loadContext in loadContexts)
        {
            loadContext.Unload();
        }

        TryDeleteDirectory(shadowDirectory);
    }

    /// <summary>
    /// 尝试删除目录
    /// </summary>
    /// <param name="directory">目录路径</param>
    private static void TryDeleteDirectory(string directory)
    {
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
        {
            return;
        }

        try
        {
            Directory.Delete(directory, true);
        }
        catch
        {
            // 旧插件上下文可能仍有执行中的引用，下一次重载会继续尝试清理。
        }
    }
}
