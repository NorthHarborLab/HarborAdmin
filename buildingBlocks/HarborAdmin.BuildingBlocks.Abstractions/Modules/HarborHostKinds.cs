namespace HarborAdmin.BuildingBlocks.Abstractions.Modules;

/// <summary>
/// Harbor 宿主类型常量。
/// </summary>
public static class HarborHostKinds
{
    /// <summary>
    /// 管理后台 HTTP API 宿主。
    /// </summary>
    public const string Host = "Host";

    /// <summary>
    /// 配置中心 TCP 宿主。
    /// </summary>
    public const string ConfigCenter = "ConfigCenter";

    /// <summary>
    /// AI 执行 Worker 宿主。
    /// </summary>
    public const string AIWorker = "AIWorker";
}
