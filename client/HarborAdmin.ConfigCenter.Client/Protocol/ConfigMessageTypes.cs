namespace HarborAdmin.ConfigCenter.Client.Protocol;

/// <summary>
/// ConfigCenter TCP JSON 协议中的 <c>type</c> 字段常量
/// </summary>
public static class ConfigMessageTypes
{
    /// <summary>
    /// 客户端 → 服务端:握手,声明 appId、environment
    /// </summary>
    public const string Handshake = "handshake";

    /// <summary>
    /// 客户端 → 服务端:拉取配置
    /// </summary>
    public const string GetConfig = "getConfig";

    /// <summary>
    /// 服务端 → 客户端:配置拉取响应
    /// </summary>
    public const string GetConfigResponse = "getConfigResponse";

    /// <summary>
    /// 客户端 → 服务端:订阅配置变更推送
    /// </summary>
    public const string Subscribe = "subscribe";

    /// <summary>
    /// 服务端 → 客户端:配置已变更通知。
    /// </summary>
    public const string ConfigChanged = "configChanged";

    /// <summary>
    /// Host → 服务端:发布完成通知
    /// </summary>
    public const string PublishNotify = "publishNotify";

    /// <summary>
    /// 服务端 → Host:发布通知确认。
    /// </summary>
    public const string PublishNotifyAck = "publishNotifyAck";

    /// <summary>
    /// 心跳请求
    /// </summary>
    public const string Ping = "ping";

    /// <summary>
    /// 心跳响应
    /// </summary>
    public const string Pong = "pong";

    /// <summary>
    /// 错误响应
    /// </summary>
    public const string Error = "error";
}
