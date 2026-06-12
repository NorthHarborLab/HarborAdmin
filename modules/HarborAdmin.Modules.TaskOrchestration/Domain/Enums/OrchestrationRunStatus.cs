namespace HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

/// <summary>
/// 任务运行状态
/// </summary>
public enum OrchestrationRunStatus
{
    /// <summary>
    /// 等待运行
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 运行中
    /// </summary>
    Running = 1,

    /// <summary>
    /// 已成功
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// 已失败
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 部分失败
    /// </summary>
    PartialFailed = 4,

    /// <summary>
    /// 已跳过
    /// </summary>
    Skipped = 5,
}
