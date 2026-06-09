namespace HarborAdmin.Modules.International.Contracts.Page.Dto;

/// <summary>
/// 前端单页面国际化资源包。
/// </summary>
public sealed record InternationalPageBundleDto(
    string Path,
    int Version,
    IReadOnlyDictionary<string, object> Messages);
