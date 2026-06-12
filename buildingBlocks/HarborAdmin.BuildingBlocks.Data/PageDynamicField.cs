using HarborAdmin.BuildingBlocks.Abstractions.Enums;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// 分页动态查询字段映射
/// </summary>
public sealed class PageDynamicField
{
    private readonly IReadOnlySet<PageFilterOperator> _operators;

    /// <summary>
    /// 初始化分页动态查询字段映射
    /// </summary>
    /// <param name="field">前端字段名</param>
    /// <param name="property">实体属性名</param>
    /// <param name="valueType">字段值类型</param>
    /// <param name="operators">允许的操作符</param>
    public PageDynamicField(
        string field,
        string property,
        Type valueType,
        IEnumerable<PageFilterOperator>? operators = null)
    {
        Field = field;
        Property = property;
        ValueType = valueType;
        _operators = (operators ?? [PageFilterOperator.Eq]).ToHashSet();
    }

    /// <summary>
    /// 前端字段名
    /// </summary>
    public string Field { get; }

    /// <summary>
    /// 实体属性名
    /// </summary>
    public string Property { get; }

    /// <summary>
    /// 字段值类型
    /// </summary>
    public Type ValueType { get; }

    /// <summary>
    /// 判断操作符是否允许
    /// </summary>
    /// <param name="operator">操作符</param>
    /// <returns>是否允许</returns>
    public bool Allows(PageFilterOperator @operator) => _operators.Contains(@operator);
}
