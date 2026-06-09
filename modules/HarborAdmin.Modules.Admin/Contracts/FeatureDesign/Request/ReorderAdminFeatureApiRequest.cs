using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;

/// <summary>
/// 功能 API 排序请求。
/// </summary>
public sealed class ReorderAdminFeatureApiRequest : IValidatableObject
{
    /// <summary>
    /// 排序后的 API ID 列表。
    /// </summary>
    public IReadOnlyList<long>? OrderedIds { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderedIds is null || OrderedIds.Count == 0)
        {
            yield return new ValidationResult("排序 API 不能为空。", [nameof(OrderedIds)]);
            yield break;
        }

        if (OrderedIds.Distinct().Count() != OrderedIds.Count)
        {
            yield return new ValidationResult("排序 API 不能重复。", [nameof(OrderedIds)]);
        }
    }
}
