using System.Reflection;
using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;
using HarborAdmin.Modules.TaskOrchestration.Infrastructure.Contexts;
using Microsoft.Extensions.Options;

namespace HarborAdmin.TaskWorker.Callables;

/// <summary>
/// 动态 DLL Callable 注册表
/// </summary>
public sealed class DynamicTaskCallableRegistry : ITaskCallableRegistry, IDisposable
{
    private readonly CallablePluginOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DynamicTaskCallableRegistry> _logger;
    private readonly object _reloadGate = new();
    private readonly CancellationTokenSource _disposeTokenSource = new();
    private CallablePluginCatalog _catalog = CallablePluginCatalog.Empty;
    private FileSystemWatcher? _watcher;
    private int _reloadVersion;

    /// <summary>
    /// 初始化动态 DLL Callable 注册表
    /// </summary>
    public DynamicTaskCallableRegistry(
        IOptions<CallablePluginOptions> options,
        IServiceScopeFactory scopeFactory,
        ILogger<DynamicTaskCallableRegistry> logger)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _logger = logger;
        EnsureDirectories();
        Reload();
        StartWatcher();
    }

    /// <summary>
    /// 列出当前已加载的可调用接口方法
    /// </summary>
    /// <returns>可调用接口方法描述集合</returns>
    public IReadOnlyList<TaskCallableDescriptor> List() => _catalog.Descriptors;

    /// <summary>
    /// 按完整类名调用插件
    /// </summary>
    public async Task<JsonNode?> InvokeAsync(
        string fullClassName,
        JsonNode? request,
        TaskExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(fullClassName))
        {
            throw new InvalidOperationException("Callable 节点必须配置 fullClassName。");
        }

        var catalog = _catalog;
        if (!catalog.Entries.TryGetValue(fullClassName.Trim(), out var entry))
        {
            throw new InvalidOperationException($"Callable '{fullClassName}' was not found in plugin directory '{_options.GetPluginDirectory()}'.");
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ITaskOrchestrationDbContext>();
        var service = (ITaskCallableService)ActivatorUtilities.CreateInstance(scope.ServiceProvider, entry.ImplementationType);
        return await service.ExecuteAsync(new TaskCallableExecutionContext(request, executionContext, db.Orm, scope.ServiceProvider), cancellationToken);
    }

    /// <summary>
    /// 释放注册表资源
    /// </summary>
    public void Dispose()
    {
        _disposeTokenSource.Cancel();
        _watcher?.Dispose();
        _catalog.Dispose();
        _disposeTokenSource.Dispose();
    }

    /// <summary>
    /// 确保插件目录存在
    /// </summary>
    private void EnsureDirectories()
    {
        Directory.CreateDirectory(_options.GetPluginDirectory());
        Directory.CreateDirectory(_options.GetShadowDirectory());
    }

    /// <summary>
    /// 启动文件监听
    /// </summary>
    private void StartWatcher()
    {
        _watcher = new FileSystemWatcher(_options.GetPluginDirectory())
        {
            Filter = "*.dll",
            IncludeSubdirectories = false,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.CreationTime | NotifyFilters.Size,
            EnableRaisingEvents = true,
        };
        _watcher.Created += (_, _) => ScheduleReload();
        _watcher.Changed += (_, _) => ScheduleReload();
        _watcher.Deleted += (_, _) => ScheduleReload();
        _watcher.Renamed += (_, _) => ScheduleReload();
    }

    /// <summary>
    /// 调度防抖重载
    /// </summary>
    private void ScheduleReload()
    {
        var version = Interlocked.Increment(ref _reloadVersion);
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(Math.Max(100, _options.ReloadDebounceMilliseconds), _disposeTokenSource.Token);
                if (version == Volatile.Read(ref _reloadVersion) && !_disposeTokenSource.IsCancellationRequested)
                {
                    Reload();
                }
            }
            catch (OperationCanceledException)
            {
            }
        });
    }

    /// <summary>
    /// 重载插件目录
    /// </summary>
    private void Reload()
    {
        lock (_reloadGate)
        {
            CallablePluginCatalog? next = null;
            try
            {
                next = BuildCatalog();
                var previous = _catalog;
                _catalog = next;
                previous.Dispose();
                _logger.LogInformation("Loaded {Count} callable plugin(s) from {Directory}.", next.Descriptors.Count, _options.GetPluginDirectory());
            }
            catch (Exception ex)
            {
                next?.Dispose();
                _logger.LogError(ex, "Failed to reload callable plugins from {Directory}; keeping previous catalog.", _options.GetPluginDirectory());
            }
        }
    }

    /// <summary>
    /// 构建插件目录快照
    /// </summary>
    /// <returns>插件目录快照</returns>
    private CallablePluginCatalog BuildCatalog()
    {
        CleanupShadowRoot();
        var pluginFiles = Directory.GetFiles(_options.GetPluginDirectory(), "*.dll", SearchOption.TopDirectoryOnly);
        if (pluginFiles.Length == 0)
        {
            return CallablePluginCatalog.Empty;
        }

        var shadowDirectory = Path.Combine(_options.GetShadowDirectory(), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString());
        Directory.CreateDirectory(shadowDirectory);
        CopyPluginFiles(shadowDirectory);

        var entries = new Dictionary<string, CallablePluginEntry>(StringComparer.OrdinalIgnoreCase);
        var loadContexts = new List<CallablePluginAssemblyLoadContext>();
        try
        {
            foreach (var pluginFile in pluginFiles.Select(file => Path.Combine(shadowDirectory, Path.GetFileName(file))))
            {
                LoadPluginAssembly(pluginFile, entries, loadContexts);
            }

            var descriptors = entries.Values.Select(item => item.Descriptor).OrderBy(item => item.FullClassName, StringComparer.OrdinalIgnoreCase).ToArray();
            return new CallablePluginCatalog(entries, descriptors, loadContexts, shadowDirectory);
        }
        catch
        {
            foreach (var loadContext in loadContexts)
            {
                loadContext.Unload();
            }

            TryDeleteDirectory(shadowDirectory);
            throw;
        }
    }

    /// <summary>
    /// 复制插件文件到影子目录
    /// </summary>
    /// <param name="shadowDirectory">影子目录</param>
    private void CopyPluginFiles(string shadowDirectory)
    {
        foreach (var file in Directory.EnumerateFiles(_options.GetPluginDirectory(), "*.*", SearchOption.TopDirectoryOnly)
                     .Where(file => IsShadowCopiedExtension(Path.GetExtension(file))))
        {
            File.Copy(file, Path.Combine(shadowDirectory, Path.GetFileName(file)), true);
        }
    }

    /// <summary>
    /// 判断文件扩展名是否需要影子复制
    /// </summary>
    /// <param name="extension">文件扩展名</param>
    /// <returns>是否复制</returns>
    private static bool IsShadowCopiedExtension(string extension) =>
        string.Equals(extension, ".dll", StringComparison.OrdinalIgnoreCase)
        || string.Equals(extension, ".pdb", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 加载单个插件程序集
    /// </summary>
    private void LoadPluginAssembly(
        string pluginFile,
        IDictionary<string, CallablePluginEntry> entries,
        ICollection<CallablePluginAssemblyLoadContext> loadContexts)
    {
        var loadContext = new CallablePluginAssemblyLoadContext(pluginFile);
        var assembly = loadContext.LoadFromAssemblyPath(pluginFile);
        var discovered = DiscoverCallableTypes(assembly).ToArray();
        if (discovered.Length == 0)
        {
            loadContext.Unload();
            return;
        }

        loadContexts.Add(loadContext);
        foreach (var type in discovered)
        {
            AddCallableType(type, entries);
        }
    }

    /// <summary>
    /// 查找程序集中的 Callable 类型
    /// </summary>
    /// <param name="assembly">程序集</param>
    /// <returns>Callable 类型集合</returns>
    private static IEnumerable<Type> DiscoverCallableTypes(Assembly assembly) =>
        assembly.GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false }
                           && typeof(ITaskCallableService).IsAssignableFrom(type));

    /// <summary>
    /// 添加 Callable 类型
    /// </summary>
    private void AddCallableType(Type type, IDictionary<string, CallablePluginEntry> entries)
    {
        if (string.IsNullOrWhiteSpace(type.FullName))
        {
            return;
        }

        if (entries.ContainsKey(type.FullName))
        {
            throw new InvalidOperationException($"Duplicate callable full class name '{type.FullName}'.");
        }

        using var scope = _scopeFactory.CreateScope();
        var service = (ITaskCallableService)ActivatorUtilities.CreateInstance(scope.ServiceProvider, type);
        entries[type.FullName] = new CallablePluginEntry(
            type,
            new TaskCallableDescriptor(type.FullName, service.ServiceKey, service.MethodKey, service.DisplayName, service.RequestType, service.ResponseType));
    }

    /// <summary>
    /// 清理旧影子目录
    /// </summary>
    private void CleanupShadowRoot()
    {
        var shadowRoot = _options.GetShadowDirectory();
        Directory.CreateDirectory(shadowRoot);
        foreach (var directory in Directory.EnumerateDirectories(shadowRoot))
        {
            try
            {
                Directory.Delete(directory, true);
            }
            catch
            {
                // 仍被旧执行引用的目录会在后续重载中再次尝试删除。
            }
        }
    }

    /// <summary>
    /// 尝试删除目录
    /// </summary>
    /// <param name="directory">目录路径</param>
    private static void TryDeleteDirectory(string directory)
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch
        {
        }
    }
}
