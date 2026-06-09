using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;

/// <summary>
/// 功能字段排序请求。
/// </summary>
public sealed class ReorderAdminFeatureFieldRequest : IValidatableObject
{
    /// <summary>
    /// 排序后的字段 ID 列表。
    /// </summary>
    public IReadOnlyList<long>? OrderedIds { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderedIds is null || OrderedIds.Count == 0)
        {
            yield return new ValidationResult("排序字段不能为空。", [nameof(OrderedIds)]);
            yield break;
        }

        if (OrderedIds.Distinct().Count() != OrderedIds.Count)
        {
            yield return new ValidationResult("排序字段不能重复。", [nameof(OrderedIds)]);
        }
    }
}
