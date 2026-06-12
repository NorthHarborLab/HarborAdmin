using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Execution;

/// <summary>
/// 空任务接口调用目录
/// </summary>
public sealed class TaskCallableRegistry : ITaskCallableRegistry
{
    /// <summary>
    /// 列出已注册的可调用接口方法
    /// </summary>
    /// <returns>可调用接口方法描述集合</returns>
    public IReadOnlyList<TaskCallableDescriptor> List() => [];

    /// <summary>
    /// 调用显式注册的接口方法
    /// </summary>
    /// <param name="fullClassName">Callable 实现完整类名</param>
    /// <param name="request">请求 JSON</param>
    /// <param name="executionContext">任务执行上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接口调用响应</returns>
    public Task<JsonNode?> InvokeAsync(string fullClassName, JsonNode? request, TaskExecutionContext executionContext, CancellationToken cancellationToken) =>
        throw new InvalidOperationException($"Callable '{fullClassName}' is not registered in this host.");
}
