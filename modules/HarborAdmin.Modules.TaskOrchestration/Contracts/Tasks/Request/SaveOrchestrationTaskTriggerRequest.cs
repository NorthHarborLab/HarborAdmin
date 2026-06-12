using System.ComponentModel.DataAnnotations;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// 保存编排任务触发器请求
/// </summary>
public sealed class SaveOrchestrationTaskTriggerRequest : IValidatableObject
{
    /// <summary>
    /// 触发器编码
    /// </summary>
    [Required(ErrorMessage = "触发器编码不能为空")]
    [MaxLength(64, ErrorMessage = "触发器编码不能超过 64 个字符")]
    public string TriggerCode { get; set; } = string.Empty;

    /// <summary>
    /// 触发器类型
    /// </summary>
    public OrchestrationTriggerType TriggerType { get; set; }

    /// <summary>
    /// Cron 表达式
    /// </summary>
    [MaxLength(128, ErrorMessage = "Cron 表达式不能超过 128 个字符")]
    public string? CronExpression { get; set; }

    /// <summary>
    /// 时区 ID
    /// </summary>
    [MaxLength(64, ErrorMessage = "时区 ID 不能超过 64 个字符")]
    public string? TimeZoneId { get; set; }

    /// <summary>
    /// CAP 触发 Topic
    /// </summary>
    [MaxLength(256, ErrorMessage = "CAP 触发 Topic 不能超过 256 个字符")]
    public string? TriggerTopic { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (TriggerType == OrchestrationTriggerType.Cron && string.IsNullOrWhiteSpace(CronExpression))
        {
            yield return new ValidationResult("Cron 触发器必须配置 Cron 表达式", [nameof(CronExpression)]);
        }

        if (TriggerType == OrchestrationTriggerType.Cap && string.IsNullOrWhiteSpace(TriggerTopic))
        {
            yield return new ValidationResult("CAP 触发器必须配置 Topic", [nameof(TriggerTopic)]);
        }
    }
}
