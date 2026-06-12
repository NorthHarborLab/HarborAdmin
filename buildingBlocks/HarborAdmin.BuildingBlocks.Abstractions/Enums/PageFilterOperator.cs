using System.Text.Json.Serialization;

namespace HarborAdmin.BuildingBlocks.Abstractions.Enums;

/// <summary>
/// 分页动态筛选操作符
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PageFilterOperator
{
    /// <summary>
    /// 等于
    /// </summary>
    Eq,

    /// <summary>
    /// 包含
    /// </summary>
    Contains,

    /// <summary>
    /// 大于等于
    /// </summary>
    Gte,

    /// <summary>
    /// 小于等于
    /// </summary>
    Lte,

    /// <summary>
    /// 区间
    /// </summary>
    Between,
}
