using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;

/// <summary>
/// 显式注册的接口调用目录
/// </summary>
public interface ITaskCallableRegistry
{
    /// <summary>
    /// 列出可调用接口方法
    /// </summary>
    /// <returns>可调用接口方法描述集合</returns>
    IReadOnlyList<TaskCallableDescriptor> List();

    /// <summary>
    /// 调用指定接口方法
    /// </summary>
    /// <param name="fullClassName">Callable 实现完整类名</param>
    /// <param name="request">请求 JSON</param>
    /// <param name="executionContext">任务执行上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接口响应 JSON</returns>
    Task<JsonNode?> InvokeAsync(string fullClassName, JsonNode? request, TaskExecutionContext executionContext, CancellationToken cancellationToken);
}
