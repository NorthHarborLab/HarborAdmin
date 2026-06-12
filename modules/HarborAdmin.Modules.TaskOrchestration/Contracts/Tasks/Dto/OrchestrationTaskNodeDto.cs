using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;

/// <summary>
/// DAG 节点 DTO
/// </summary>
/// <param name="Id">节点 ID</param>
/// <param name="NodeCode">节点编码</param>
/// <param name="Name">节点名称</param>
/// <param name="ExecutorType">执行器类型</param>
/// <param name="ConfigJson">节点配置 JSON</param>
/// <param name="PositionX">画布 X 坐标</param>
/// <param name="PositionY">画布 Y 坐标</param>
/// <param name="TimeoutSeconds">超时秒数</param>
/// <param name="RetryCount">重试次数</param>
/// <param name="FailurePolicy">失败策略</param>
/// <param name="Enabled">是否启用</param>
public sealed record OrchestrationTaskNodeDto(
    long Id,
    string NodeCode,
    string Name,
    string ExecutorType,
    string? ConfigJson,
    int PositionX,
    int PositionY,
    int TimeoutSeconds,
    int RetryCount,
    OrchestrationFailurePolicy FailurePolicy,
    bool Enabled);
