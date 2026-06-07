using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HarborAdmin.BuildingBlocks.Caching.Infrastructure;

/// <summary>
/// 缓存 JSON 内容脱敏工具。
/// </summary>
internal static class CacheEntryMasker
{
    private static readonly HashSet<string> DefaultSensitiveFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "PrivateKeyBase64",
        "Password",
        "Token"
    };

    /// <summary>
    /// 对 JSON 文本中的敏感字段做脱敏。
    /// </summary>
    public static string MaskJson(string json, IReadOnlyCollection<string> sensitiveFields)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return json;
        }

        var fieldSet = new HashSet<string>(DefaultSensitiveFields, StringComparer.OrdinalIgnoreCase);
        foreach (var field in sensitiveFields)
        {
            if (!string.IsNullOrWhiteSpace(field))
            {
                fieldSet.Add(field);
            }
        }

        try
        {
            var node = JsonNode.Parse(json);
            if (node is null)
            {
                return json;
            }

            MaskNode(node, fieldSet);
            return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }
        catch (JsonException)
        {
            return json;
        }
    }

    private static void MaskNode(JsonNode node, HashSet<string> sensitiveFields)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    if (sensitiveFields.Contains(property.Key))
                    {
                        obj[property.Key] = "***";
                        continue;
                    }

                    if (property.Value is not null)
                    {
                        MaskNode(property.Value, sensitiveFields);
                    }
                }

                break;
            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        MaskNode(item, sensitiveFields);
                    }
                }

                break;
        }
    }

    /// <summary>
    /// 截断超大 JSON 文本。
    /// </summary>
    public static (string Json, bool Truncated) TruncateIfNeeded(string json, int maxBytes)
    {
        var size = Encoding.UTF8.GetByteCount(json);
        if (size <= maxBytes)
        {
            return (json, false);
        }

        var truncated = json[..Math.Min(json.Length, maxBytes / 2)];
        return ($"{truncated}\n/* truncated: {size} bytes */", true);
    }
}
