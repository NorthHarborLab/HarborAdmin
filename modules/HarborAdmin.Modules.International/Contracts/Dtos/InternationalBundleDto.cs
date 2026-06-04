namespace HarborAdmin.Modules.International.Contracts.Dtos;

/// <summary>
/// 前端国际化资源包。
/// </summary>
public sealed record InternationalBundleDto(int Version, IReadOnlyDictionary<string, object> Messages);
