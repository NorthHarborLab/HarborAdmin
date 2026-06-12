namespace HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

/// <summary>
/// 任务触发器类型
/// </summary>
public enum OrchestrationTriggerType
{
    /// <summary>
    /// Cron 定时触发
    /// </summary>
    Cron = 1,

    /// <summary>
    /// CAP 消息触发
    /// </summary>
    Cap = 2,
}
