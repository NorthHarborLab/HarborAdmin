namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// 手动运行任务请求
/// </summary>
/// <param name="ParamsJson">运行参数 JSON</param>
/// <param name="CorrelationId">关联标识</param>
public sealed record RunOrchestrationTaskRequest(string? ParamsJson, string? CorrelationId);
