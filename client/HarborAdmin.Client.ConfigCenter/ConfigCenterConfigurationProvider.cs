using Microsoft.Extensions.Configuration;

namespace HarborAdmin.Client.ConfigCenter;

/// <summary>
/// 从 ConfigCenter 拉取的配置数据提供程序，支持通过 <see cref="ConfigurationProvider.OnReload"/> 触发热更新。
/// </summary>
public sealed class ConfigCenterConfigurationProvider : ConfigurationProvider
{
    private readonly object _sync = new();

    /// <summary>
    /// 当前已加载的发布版本号。
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// 用远程拉取的键值对更新内部数据并触发变更通知
    /// </summary>
    /// <param name="data">扁平化配置字典，键格式与标准 <see cref="IConfiguration"/> 路径一致。</param>
    /// <param name="version">发布版本号,0 表示无已发布版本。</param>
    /// <returns>数据被应用并触发 reload 时返回 <see langword="true"/>。</returns>
    internal bool SetData(IReadOnlyDictionary<string, string> data, int version = 0)
    {
        lock (_sync)
        {
            if (version > 0 && version == Version)
            {
                return false;
            }

            Data = data.ToDictionary(static pair => pair.Key, static pair => (string?)pair.Value, StringComparer.OrdinalIgnoreCase);
            Version = version;
        }

        OnReload();
        return true;
    }

    /// <summary>
    /// 获取当前已从 ConfigCenter 加载的扁平化配置副本
    /// </summary>
    public IReadOnlyDictionary<string, string?> GetAllData()
    {
        lock (_sync)
        {
            return Data.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
