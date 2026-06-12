using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;

/// <summary>
/// DAG 连线 DTO
/// </summary>
/// <param name="Id">连线 ID</param>
/// <param name="EdgeCode">连线编码</param>
/// <param name="SourceNodeCode">上游节点编码</param>
/// <param name="TargetNodeCode">下游节点编码</param>
/// <param name="ConditionExpression">条件表达式</param>
/// <param name="JoinPolicy">汇聚策略</param>
/// <param name="Enabled">是否启用</param>
public sealed record OrchestrationTaskEdgeDto(
    long Id,
    string EdgeCode,
    string SourceNodeCode,
    string TargetNodeCode,
    string? ConditionExpression,
    OrchestrationJoinPolicy JoinPolicy,
    bool Enabled);
