namespace HarborAdmin.Modules.International.Contracts.Page.Dto;

/// <summary>
/// 国际化资源分组树节点 DTO。
/// </summary>
public sealed record InternationalGroupNodeDto(
    long Id,
    long? ParentId,
    string Key,
    string Path,
    string Name,
    int SortOrder,
    IReadOnlyList<InternationalPageDto> Pages,
    IReadOnlyList<InternationalGroupNodeDto> Children);
