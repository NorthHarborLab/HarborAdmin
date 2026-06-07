using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign;

/// <summary>
/// JSON 文本校验。
/// </summary>
public sealed class JsonTextAttribute : ValidationAttribute
{
    /// <summary>
    /// 校验字符串是否为合法 JSON 文本。
    /// </summary>
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return ValidationResult.Success;
        }

        try
        {
            _ = JsonDocument.Parse(text);
            return ValidationResult.Success;
        }
        catch (JsonException)
        {
            return new ValidationResult($"字段 '{validationContext?.DisplayName}' 必须是合法 JSON。");
        }
    }
}
