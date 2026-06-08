using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.System.Request;

/// <summary>
/// 菜单同级排序请求。
/// </summary>
public sealed class ReorderSystemMenuRequest : IValidatableObject
{
    /// <summary>
    /// 父级菜单 ID。
    /// </summary>
    [MaxLength(32)]
    public string? Pid { get; set; }

    /// <summary>
    /// 排序后的菜单 ID 列表。
    /// </summary>
    public IReadOnlyList<string>? OrderedIds { get; set; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (OrderedIds is null || OrderedIds.Count == 0)
        {
            yield return new ValidationResult("排序菜单不能为空。", [nameof(OrderedIds)]);
            yield break;
        }

        if (OrderedIds.Distinct(StringComparer.Ordinal).Count() != OrderedIds.Count)
        {
            yield return new ValidationResult("排序菜单不能重复。", [nameof(OrderedIds)]);
        }
    }
}
