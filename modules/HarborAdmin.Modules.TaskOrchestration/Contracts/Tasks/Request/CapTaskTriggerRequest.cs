namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// CAP 触发消息
/// </summary>
/// <param name="TaskCode">任务编码</param>
/// <param name="ParamsJson">运行参数 JSON</param>
/// <param name="CorrelationId">关联标识</param>
/// <param name="RequestedBy">请求人</param>
public sealed record CapTaskTriggerRequest(string? TaskCode, string? ParamsJson, string? CorrelationId, string? RequestedBy);
