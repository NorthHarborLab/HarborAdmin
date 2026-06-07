using HarborAdmin.BuildingBlocks.Abstractions.Exception;

namespace HarborAdmin.Modules.Admin.Application.Services.Shared;

/// <summary>
/// Admin 模块 ID 与标识码解析工具。
/// </summary>
internal static class AdminIdHelper
{
    /// <summary>
    /// 将字符串解析为 long 类型 ID。
    /// </summary>
    public static long ParseId(string value) =>
        long.TryParse(value, out var id) ? id : throw new ValidationDomainException($"无效 ID：{value}");

    /// <summary>
    /// 将字符串解析为可空 long 类型 ID。
    /// </summary>
    public static long? ParseNullableId(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == "0" ? null : ParseId(value);

    /// <summary>
    /// 将任意字符串规范化为小写标识码。
    /// </summary>
    public static string BuildCode(string value)
    {
        var cleaned = new string(value.Select(ch => char.IsLetterOrDigit(ch) ? char.ToLowerInvariant(ch) : '_').ToArray()).Trim('_');
        return string.IsNullOrWhiteSpace(cleaned) ? Guid.NewGuid().ToString("N")[..8] : cleaned;
    }
}
