namespace HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

/// <summary>
/// DAG 汇聚策略
/// </summary>
public enum OrchestrationJoinPolicy
{
    /// <summary>
    /// 全部上游成功后继续
    /// </summary>
    AllSucceeded = 1,

    /// <summary>
    /// 任一上游成功后继续
    /// </summary>
    AnySucceeded = 2,

    /// <summary>
    /// 全部上游完成后继续
    /// </summary>
    AllCompleted = 3,
}
