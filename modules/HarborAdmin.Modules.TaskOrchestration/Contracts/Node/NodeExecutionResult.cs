using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Node;

/// <summary>
/// 节点执行结果
/// </summary>
/// <param name="NodeCode">节点编码</param>
/// <param name="Status">节点状态</param>
/// <param name="Output">节点输出</param>
internal sealed record NodeExecutionResult(string NodeCode, OrchestrationRunStatus Status, JsonNode? Output);
