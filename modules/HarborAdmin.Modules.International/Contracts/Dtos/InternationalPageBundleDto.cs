namespace HarborAdmin.Modules.International.Contracts.Dtos;

/// <summary>
/// 前端单页面国际化资源包。
/// </summary>
public sealed record InternationalPageBundleDto(
    string PageKey,
    int Version,
    IReadOnlyDictionary<string, object> Messages);
