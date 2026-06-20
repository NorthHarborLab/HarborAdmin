using System.Text.Json.Nodes;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

/// <summary>
/// 任务内部接口调用上下文
/// </summary>
/// <param name="Request">请求 JSON</param>
/// <param name="ExecutionContext">任务执行上下文</param>
/// <param name="ServiceProvider">当前作用域服务提供器，用于解析业务服务或仓储接口</param>
public sealed record TaskCallableExecutionContext(JsonNode? Request, TaskExecutionContext ExecutionContext, IServiceProvider ServiceProvider);
