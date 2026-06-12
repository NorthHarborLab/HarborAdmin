namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// 设置任务启停请求
/// </summary>
/// <param name="Enabled">是否启用</param>
public sealed record SetOrchestrationTaskEnabledRequest(bool Enabled);
