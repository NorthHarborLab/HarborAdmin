using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HarborAdmin.ConfigCenter.Client.Protocol;

/// <summary>
/// ConfigCenter TCP JSON 协议消息体(序列化后为帧 payload)
/// </summary>
public sealed class ConfigMessage
{
    /// <summary>
    /// JSON 中消息类型字段名
    /// </summary>
    public const string TypePropertyName = "type";

    /// <summary>
    /// 消息类型,见 <see cref="ConfigMessageTypes"/>
    /// </summary>
    [JsonPropertyName(TypePropertyName)]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// 应用标识
    /// </summary>
    [JsonPropertyName("appId")]
    public string? AppId { get; set; }

    /// <summary>
    /// 环境名称
    /// </summary>
    [JsonPropertyName("environment")]
    public string? Environment { get; set; }

    /// <summary>
    /// 客户端实例 ID(握手时使用)
    /// </summary>
    [JsonPropertyName("clientId")]
    public string? ClientId { get; set; }

    /// <summary>
    /// 配置版本号;<c>getConfig</c> 请求中 0 表示最新
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; }

    /// <summary>
    /// 发布记录主键(<c>publishNotify</c> 使用)
    /// </summary>
    [JsonPropertyName("releaseId")]
    public long ReleaseId { get; set; }

    /// <summary>
    /// 扁平化配置键值对(<c>getConfigResponse</c> 使用)
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, string>? Data { get; set; }

    /// <summary>
    /// 操作是否成功(握手确认、publishNotifyAck 等)
    /// </summary>
    [JsonPropertyName("ok")]
    public bool Ok { get; set; }

    /// <summary>
    /// 错误码(<c>error</c> 类型)
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// 错误或描述信息
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// JSON 序列化选项(camelCase,忽略 null)
    /// </summary>
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// 构造握手请求
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    /// <param name="clientId">客户端 ID</param>
    public static ConfigMessage HandshakeRequest(string appId, string environment, string clientId) =>
        new()
        {
            Type = ConfigMessageTypes.Handshake,
            AppId = appId,
            Environment = environment,
            ClientId = clientId
        };

    /// <summary>
    /// 构造拉取配置请求
    /// </summary>
    /// <param name="version">版本号,0 为最新</param>
    public static ConfigMessage GetConfigRequest(int version = 0) =>
        new() { Type = ConfigMessageTypes.GetConfig, Version = version };

    /// <summary>
    /// 构造订阅请求
    /// </summary>
    public static ConfigMessage SubscribeRequest() =>
        new() { Type = ConfigMessageTypes.Subscribe };

    /// <summary>
    /// 构造发布通知(Host 短连接使用)
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    /// <param name="releaseId">发布主键</param>
    public static ConfigMessage PublishNotifyRequest(string appId, string environment, long releaseId) =>
        new()
        {
            Type = ConfigMessageTypes.PublishNotify,
            AppId = appId,
            Environment = environment,
            ReleaseId = releaseId
        };

    /// <summary>
    /// 构造心跳请求
    /// </summary>
    public static ConfigMessage PingRequest() =>
        new() { Type = ConfigMessageTypes.Ping };

    /// <summary>
    /// 将消息编码为完整 TCP 帧(4 字节大端长度 + UTF-8 JSON)
    /// </summary>
    /// <returns>可写入网络流的字节数组</returns>
    public byte[] ToFrameBytes()
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(this, JsonOptions);
        var frame = new byte[4 + payload.Length];
        BinaryPrimitives.WriteUInt32BigEndian(frame.AsSpan(0, 4), (uint)payload.Length);
        payload.CopyTo(frame, 4);
        return frame;
    }

    /// <summary>
    /// 从帧 payload 反序列化消息
    /// </summary>
    /// <param name="payload">UTF-8 JSON 字节</param>
    /// <returns>消息对象;payload 为空时返回 <see langword="null"/></returns>
    public static ConfigMessage? FromPayload(ReadOnlySpan<byte> payload) =>
        payload.Length == 0
            ? null
            : JsonSerializer.Deserialize<ConfigMessage>(payload, JsonOptions);
}
