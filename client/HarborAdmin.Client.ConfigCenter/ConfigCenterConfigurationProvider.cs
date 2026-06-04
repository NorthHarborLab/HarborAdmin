using Microsoft.Extensions.Configuration;

namespace HarborAdmin.Client.ConfigCenter;

/// <summary>
/// 从 ConfigCenter 拉取的配置数据提供程序，支持通过 <see cref="ConfigurationProvider.OnReload"/> 触发热更新。
/// </summary>
public sealed class ConfigCenterConfigurationProvider : ConfigurationProvider
{
    /// <summary>
    /// 用远程拉取的键值对更新内部数据并触发变更通知
    /// </summary>
    /// <param name="data">扁平化配置字典，键格式与标准 <see cref="IConfiguration"/> 路径一致。</param>
    internal void SetData(IReadOnlyDictionary<string, string> data)
    {
        Data = data.ToDictionary(static pair => pair.Key, static pair => (string?)pair.Value, StringComparer.OrdinalIgnoreCase);
        OnReload();
    }

    /// <summary>
    /// 获取当前已从 ConfigCenter 加载的扁平化配置副本
    /// </summary>
    public IReadOnlyDictionary<string, string?> GetAllData() =>
        Data.ToDictionary(static x => x.Key, static x => x.Value, StringComparer.OrdinalIgnoreCase);
}
