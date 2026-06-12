using System.Text.Json;
using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Node;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Execution;

/// <summary>
/// 显式注册接口调用节点执行器
/// </summary>
public sealed class TaskCallableStepExecutor(ITaskCallableRegistry registry, ITaskTemplateRenderer renderer) : ITaskStepExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 执行器类型
    /// </summary>
    public string ExecutorType => OrchestrationExecutorTypes.Callable;

    /// <summary>
    /// 执行显式注册接口调用节点
    /// </summary>
    /// <param name="context">节点执行上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接口调用响应</returns>
    public Task<JsonNode?> ExecuteAsync(TaskNodeExecutionContext context, CancellationToken cancellationToken)
    {
        var config = JsonSerializer.Deserialize<CallableNodeConfig>(context.ConfigJson ?? "{}", JsonOptions) ?? new CallableNodeConfig();
        var requestText = renderer.Render(config.RequestJson ?? "{}", context.ExecutionContext);
        var request = JsonNode.Parse(string.IsNullOrWhiteSpace(requestText) ? "{}" : requestText);
        var serviceKey = config.FullClassName ?? config.ClassName ?? config.TypeName ?? config.ServiceKey ?? string.Empty;
        return registry.InvokeAsync(serviceKey, config.MethodKey ?? string.Empty, request, context.ExecutionContext, cancellationToken);
    }
}
