namespace HarborAdmin.TaskWorker.Callables;

/// <summary>
/// Callable 插件加载配置
/// </summary>
public sealed class CallablePluginOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "Harbor:TaskWorker:CallablePlugins";

    /// <summary>
    /// 插件目录
    /// </summary>
    public string Directory { get; set; } = "callables";

    /// <summary>
    /// 影子复制目录
    /// </summary>
    public string? ShadowDirectory { get; set; }

    /// <summary>
    /// 重载防抖毫秒数
    /// </summary>
    public int ReloadDebounceMilliseconds { get; set; } = 1000;

    /// <summary>
    /// 获取插件目录绝对路径
    /// </summary>
    /// <returns>插件目录绝对路径</returns>
    public string GetPluginDirectory()
    {
        return Path.GetFullPath(Path.IsPathRooted(Directory)
            ? Directory
            : Path.Combine(AppContext.BaseDirectory, Directory));
    }

    /// <summary>
    /// 获取影子复制目录绝对路径
    /// </summary>
    /// <returns>影子复制目录绝对路径</returns>
    public string GetShadowDirectory()
    {
        var directory = string.IsNullOrWhiteSpace(ShadowDirectory)
            ? Path.Combine(Path.GetTempPath(), "HarborAdmin.TaskWorker", "callables-shadow")
            : ShadowDirectory;
        return Path.GetFullPath(Path.IsPathRooted(directory)
            ? directory
            : Path.Combine(AppContext.BaseDirectory, directory));
    }
}
