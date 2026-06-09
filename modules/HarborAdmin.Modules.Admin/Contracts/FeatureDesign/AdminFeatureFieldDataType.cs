namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign;

/// <summary>
/// 功能字段数据类型。
/// </summary>
public enum AdminFeatureFieldDataType : short
{
    /// <summary>
    /// 字符串。
    /// </summary>
    String,

    /// <summary>
    /// 整数。
    /// </summary>
    Int,

    /// <summary>
    /// 小数。
    /// </summary>
    Decimal,

    /// <summary>
    /// 布尔。
    /// </summary>
    Bool,

    /// <summary>
    /// 日期时间。
    /// </summary>
    DateTime,

    /// <summary>
    /// 长整数。
    /// </summary>
    Long,

    /// <summary>
    /// 数组。
    /// </summary>
    Array,

    /// <summary>
    /// 对象。
    /// </summary>
    Object,
}
