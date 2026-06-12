namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;

/// <summary>
/// 编排任务 DTO
/// </summary>
/// <param name="Id">任务 ID</param>
/// <param name="TaskCode">任务编码</param>
/// <param name="Name">任务名称</param>
/// <param name="Description">任务说明</param>
/// <param name="Enabled">是否启用</param>
/// <param name="AllowConcurrentRuns">是否允许并发运行</param>
/// <param name="DefaultParamsJson">默认运行参数 JSON</param>
/// <param name="ParamSchemaJson">参数 Schema JSON</param>
/// <param name="Triggers">触发器集合</param>
/// <param name="Nodes">DAG 节点集合</param>
/// <param name="Edges">DAG 连线集合</param>
/// <param name="CreatedAt">创建时间</param>
/// <param name="UpdatedAt">更新时间</param>
public sealed record OrchestrationTaskDto(
    long Id,
    string TaskCode,
    string Name,
    string? Description,
    bool Enabled,
    bool AllowConcurrentRuns,
    string? DefaultParamsJson,
    string? ParamSchemaJson,
    IReadOnlyList<OrchestrationTaskTriggerDto> Triggers,
    IReadOnlyList<OrchestrationTaskNodeDto> Nodes,
    IReadOnlyList<OrchestrationTaskEdgeDto> Edges,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
