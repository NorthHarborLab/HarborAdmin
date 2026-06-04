namespace HarborAdmin.Client.ConfigCenter;

/// <summary>
/// ConfigCenter 客户端只读运行状态,供业务进程诊断当前连接与热更新结果。
/// </summary>
public interface IConfigCenterClientState
{
    /// <summary>
    /// 当前订阅的 AppId。
    /// </summary>
    string? AppId { get; }

    /// <summary>
    /// 当前客户端实例 ID。
    /// </summary>
    string? ClientId { get; }

    /// <summary>
    /// 是否处于已连接状态。
    /// </summary>
    bool Connected { get; }

    /// <summary>
    /// 当前已加载的发布版本号。
    /// </summary>
    int Version { get; }

    /// <summary>
    /// 最近一次连接成功时间。
    /// </summary>
    DateTimeOffset? LastConnectedAt { get; }

    /// <summary>
    /// 最近一次应用远程配置时间。
    /// </summary>
    DateTimeOffset? LastReloadAt { get; }

    /// <summary>
    /// 最近一次连接或协议错误。
    /// </summary>
    string? LastError { get; }

    /// <summary>
    /// 当前已加载的配置键值副本。
    /// </summary>
    IReadOnlyDictionary<string, string?> CurrentData { get; }
}

/// <summary>
/// ConfigCenter 客户端运行状态的线程安全实现。
/// </summary>
public sealed class ConfigCenterClientState : IConfigCenterClientState
{
    private readonly object _sync = new();
    private Dictionary<string, string?> _currentData = new(StringComparer.OrdinalIgnoreCase);
    private string? _appId;
    private string? _clientId;
    private bool _connected;
    private int _version;
    private DateTimeOffset? _lastConnectedAt;
    private DateTimeOffset? _lastReloadAt;
    private string? _lastError;

    public string? AppId
    {
        get { lock (_sync) return _appId; }
    }

    public string? ClientId
    {
        get { lock (_sync) return _clientId; }
    }

    public bool Connected
    {
        get { lock (_sync) return _connected; }
    }

    public int Version
    {
        get { lock (_sync) return _version; }
    }

    public DateTimeOffset? LastConnectedAt
    {
        get { lock (_sync) return _lastConnectedAt; }
    }

    public DateTimeOffset? LastReloadAt
    {
        get { lock (_sync) return _lastReloadAt; }
    }

    public string? LastError
    {
        get { lock (_sync) return _lastError; }
    }

    public IReadOnlyDictionary<string, string?> CurrentData
    {
        get
        {
            lock (_sync)
            {
                return _currentData.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
            }
        }
    }

    internal void Configure(string appId, string clientId)
    {
        lock (_sync)
        {
            _appId = appId;
            _clientId = clientId;
        }
    }

    internal void MarkConnected()
    {
        lock (_sync)
        {
            _connected = true;
            _lastConnectedAt = DateTimeOffset.UtcNow;
            _lastError = null;
        }
    }

    internal void MarkDisconnected(string? error = null)
    {
        lock (_sync)
        {
            _connected = false;
            _lastError = error;
        }
    }

    internal void MarkReloaded(int version, IReadOnlyDictionary<string, string?> data)
    {
        lock (_sync)
        {
            _version = version;
            _currentData = data.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
            _lastReloadAt = DateTimeOffset.UtcNow;
            _lastError = null;
        }
    }
}
