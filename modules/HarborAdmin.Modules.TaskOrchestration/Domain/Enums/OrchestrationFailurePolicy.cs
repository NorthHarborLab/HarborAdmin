namespace HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

/// <summary>
/// 节点失败策略
/// </summary>
public enum OrchestrationFailurePolicy
{
    /// <summary>
    /// 阻塞下游节点
    /// </summary>
    BlockDependents = 1,

    /// <summary>
    /// 停止全部节点
    /// </summary>
    StopAll = 2,

    /// <summary>
    /// 继续执行独立分支
    /// </summary>
    ContinueIndependent = 3,
}
