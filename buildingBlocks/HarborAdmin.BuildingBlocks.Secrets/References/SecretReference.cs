using System.Text;
using System.Text.RegularExpressions;

namespace HarborAdmin.BuildingBlocks.Secrets.References;

/// <summary>
/// 配置中的密钥引用。
/// </summary>
public sealed record SecretReferenceToken(string FullText, string SecretRef, int? Version);

/// <summary>
/// <c>${secret:ref}</c> 引用格式工具。
/// </summary>
public static partial class SecretReferenceParser
{
    /// <summary>
    /// 格式化密钥引用。
    /// </summary>
    public static string Format(string secretRef, int? version = null) =>
        version is > 0 ? $"${{secret:{secretRef}@v{version}}}" : $"${{secret:{secretRef}}}";

    /// <summary>
    /// 查找字符串中的所有密钥引用。
    /// </summary>
    public static IReadOnlyList<SecretReferenceToken> Find(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return ReferencePattern().Matches(value)
            .Select(match => new SecretReferenceToken(
                match.Value,
                match.Groups["ref"].Value,
                int.TryParse(match.Groups["version"].Value, out var version) ? version : null))
            .ToList();
    }

    /// <summary>
    /// 尝试解析完整字符串形式的密钥引用。
    /// </summary>
    public static bool TryParseSingle(string value, out SecretReferenceToken reference)
    {
        reference = new SecretReferenceToken(string.Empty, string.Empty, null);
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var match = ReferencePattern().Match(value.Trim());
        if (!match.Success || match.Index != 0 || match.Length != value.Trim().Length)
        {
            return false;
        }

        reference = new SecretReferenceToken(
            match.Value,
            match.Groups["ref"].Value,
            int.TryParse(match.Groups["version"].Value, out var version) ? version : null);
        return true;
    }

    /// <summary>
    /// 判断是否存在密钥引用。
    /// </summary>
    public static bool Contains(string value) =>
        !string.IsNullOrEmpty(value) && ReferencePattern().IsMatch(value);

    /// <summary>
    /// 替换引用。
    /// </summary>
    public static async Task<string> ReplaceAsync(
        string value,
        Func<SecretReferenceToken, CancellationToken, Task<string>> replacementFactory,
        CancellationToken cancellationToken = default)
    {
        var matches = ReferencePattern().Matches(value);
        if (matches.Count == 0)
        {
            return value;
        }

        var builder = new StringBuilder(value.Length);
        var cursor = 0;
        foreach (Match match in matches)
        {
            builder.Append(value, cursor, match.Index - cursor);
            var reference = new SecretReferenceToken(
                match.Value,
                match.Groups["ref"].Value,
                int.TryParse(match.Groups["version"].Value, out var version) ? version : null);
            builder.Append(await replacementFactory(reference, cancellationToken));
            cursor = match.Index + match.Length;
        }

        builder.Append(value, cursor, value.Length - cursor);
        return builder.ToString();
    }

    /// <summary>
    /// 校验 SecretRef 字符集。
    /// </summary>
    public static bool IsValidRef(string secretRef) =>
        !string.IsNullOrWhiteSpace(secretRef) && RefPattern().IsMatch(secretRef);

    [GeneratedRegex(@"\$\{secret:(?<ref>[A-Za-z0-9._:-]+)(?:@v(?<version>[1-9][0-9]*))?\}", RegexOptions.CultureInvariant)]
    private static partial Regex ReferencePattern();

    [GeneratedRegex(@"^[A-Za-z0-9._:-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex RefPattern();
}
