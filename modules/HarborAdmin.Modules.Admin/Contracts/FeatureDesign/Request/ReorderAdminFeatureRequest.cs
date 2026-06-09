using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;

/// <summary>
/// 功能设计同组排序请求。
/// </summary>
public sealed class ReorderAdminFeatureRequest : IValidatableObject
{
    /// <summary>
    /// 父级分类 ID。
    /// </summary>
    public long? ParentId { get; set; }

    /// <summary>
    /// 根节点分组类型。
    /// </summary>
    public AdminFeatureNodeType? NodeType { get; set; }

    /// <summary>
    /// 排序后的节点 ID 列表。
    /// </summary>
    public IReadOnlyList<long>? OrderedIds { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderedIds is null || OrderedIds.Count == 0)
        {
            yield return new ValidationResult("排序节点不能为空。", [nameof(OrderedIds)]);
            yield break;
        }

        if (OrderedIds.Distinct().Count() != OrderedIds.Count)
        {
            yield return new ValidationResult("排序节点不能重复。", [nameof(OrderedIds)]);
        }
    }
}
