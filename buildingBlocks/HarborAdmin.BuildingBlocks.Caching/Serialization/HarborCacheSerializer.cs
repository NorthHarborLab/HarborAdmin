using System.Text.Json;

namespace HarborAdmin.BuildingBlocks.Caching.Serialization;

/// <summary>
/// Harbor 缓存 JSON 序列化工具。
/// </summary>
internal static class HarborCacheSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    /// <summary>
    /// 将对象序列化为 UTF-8 JSON 字节。
    /// </summary>
    public static byte[] Serialize<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    /// <summary>
    /// 从 UTF-8 JSON 字节反序列化对象。
    /// </summary>
    public static T? Deserialize<T>(byte[] bytes) => JsonSerializer.Deserialize<T>(bytes, Options);

    /// <summary>
    /// 将对象序列化为 JSON 字符串。
    /// </summary>
    public static string SerializeToString<T>(T value) => JsonSerializer.Serialize(value, Options);

    /// <summary>
    /// 将任意运行时类型对象序列化为 JSON 字符串。
    /// </summary>
    public static string SerializeObjectToString(object value) => JsonSerializer.Serialize(value, value.GetType(), Options);

    /// <summary>
    /// 从 JSON 字符串反序列化对象。
    /// </summary>
    public static T? DeserializeFromString<T>(string value) => JsonSerializer.Deserialize<T>(value, Options);
}