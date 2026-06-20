using HarborAdmin.BuildingBlocks.Abstractions.Enums;

namespace HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;

/// <summary>
/// 分页动态筛选条件
/// </summary>
public sealed class PageFilterRule
{
    /// <summary>
    /// 字段名
    /// </summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>
    /// 操作符
    /// </summary>
    public PageFilterOperator Operator { get; set; } = PageFilterOperator.Eq;

    /// <summary>
    /// 单值
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// 多值
    /// </summary>
    public IReadOnlyList<object?>? Values { get; set; }
}
