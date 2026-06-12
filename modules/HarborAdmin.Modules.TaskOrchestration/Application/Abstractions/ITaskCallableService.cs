using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Contracts.TaskContext;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;

/// <summary>
/// 显式注册的任务内部接口调用服务
/// </summary>
public interface ITaskCallableService
{
    /// <summary>
    /// 服务键
    /// </summary>
    string ServiceKey { get; }

    /// <summary>
    /// 方法键
    /// </summary>
    string MethodKey { get; }

    /// <summary>
    /// 显示名称
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// 请求类型
    /// </summary>
    Type RequestType { get; }

    /// <summary>
    /// 响应类型
    /// </summary>
    Type ResponseType { get; }

    /// <summary>
    /// 执行内部接口调用
    /// </summary>
    /// <param name="context">接口调用上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>调用响应 JSON</returns>
    Task<JsonNode?> ExecuteAsync(TaskCallableExecutionContext context, CancellationToken cancellationToken);
}
