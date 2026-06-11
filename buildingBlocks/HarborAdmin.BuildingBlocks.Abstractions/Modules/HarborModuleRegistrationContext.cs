using Microsoft.Extensions.Configuration;

namespace HarborAdmin.BuildingBlocks.Abstractions.Modules;

/// <summary>
/// Harbor 模块注册上下文。
/// </summary>
public sealed class HarborModuleRegistrationContext
{
    /// <summary>
    /// 初始化模块注册上下文。
    /// </summary>
    /// <param name="configuration">配置根。</param>
    /// <param name="hostKind">宿主类型。</param>
    public HarborModuleRegistrationContext(IConfiguration configuration, string hostKind)
    {
        Configuration = configuration;
        HostKind = string.IsNullOrWhiteSpace(hostKind)
            ? HarborHostKinds.Host
            : hostKind.Trim();
    }

    /// <summary>
    /// 配置根。
    /// </summary>
    public IConfiguration Configuration { get; }

    /// <summary>
    /// 当前宿主类型。
    /// </summary>
    public string HostKind { get; }

    /// <summary>
    /// 判断当前宿主类型是否匹配。
    /// </summary>
    /// <param name="hostKind">待匹配宿主类型。</param>
    /// <returns>匹配时返回 <see langword="true"/>。</returns>
    public bool IsHostKind(string hostKind) =>
        string.Equals(HostKind, hostKind, StringComparison.OrdinalIgnoreCase);
}
