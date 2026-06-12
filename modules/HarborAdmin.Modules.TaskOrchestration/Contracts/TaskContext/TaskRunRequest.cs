namespace HarborAdmin.Modules.TaskOrchestration.Contracts.TaskContext;

/// <summary>
/// 任务运行请求
/// </summary>
/// <param name="TaskId">任务标识</param>
/// <param name="TriggerType">触发类型</param>
/// <param name="TriggerCode">触发器编码</param>
/// <param name="ParamsJson">运行参数 JSON</param>
/// <param name="TriggerPayloadJson">触发消息 JSON</param>
/// <param name="CorrelationId">关联标识</param>
public sealed record TaskRunRequest(long TaskId, string TriggerType, string? TriggerCode, string? ParamsJson, string? TriggerPayloadJson, string? CorrelationId);
