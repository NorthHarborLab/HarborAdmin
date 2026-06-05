namespace HarborAdmin.Modules.Admin.Contracts.Dtos;

/// <summary>
/// 动态 CRUD 分页查询结果。
/// </summary>
public sealed record DynamicQueryResultDto(
    IReadOnlyList<IReadOnlyDictionary<string, object?>> Items,
    long Total);