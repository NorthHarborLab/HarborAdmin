using System.Text.Json.Nodes;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.TaskContext;

/// <summary>
/// 节点执行结果
/// </summary>
/// <param name="Status">节点状态</param>
/// <param name="Output">节点输出</param>
public sealed record TaskNodeResult(string Status, JsonNode? Output);
