using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;
using HarborAdmin.Modules.TaskOrchestration.Infrastructure.Contexts;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Execution;

/// <summary>
/// 显式注册接口调用目录
/// </summary>
public sealed class TaskCallableRegistry(
    IEnumerable<ITaskCallableService> services,
    ITaskOrchestrationDbContext db,
    IServiceProvider serviceProvider) : ITaskCallableRegistry
{
    private readonly IReadOnlyDictionary<string, ITaskCallableService> _services = BuildServiceIndex(services);

    private readonly IReadOnlyList<TaskCallableDescriptor> _descriptors = services
        .Select(item => new TaskCallableDescriptor(item.ServiceKey, item.MethodKey, item.DisplayName, item.RequestType, item.ResponseType))
        .ToArray();

    /// <summary>
    /// 列出已注册的可调用接口方法
    /// </summary>
    /// <returns>可调用接口方法描述集合</returns>
    public IReadOnlyList<TaskCallableDescriptor> List() => _descriptors;

    /// <summary>
    /// 调用显式注册的接口方法
    /// </summary>
    /// <param name="serviceKey">服务键</param>
    /// <param name="methodKey">方法键</param>
    /// <param name="request">请求 JSON</param>
    /// <param name="executionContext">任务执行上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>接口调用响应</returns>
    public Task<JsonNode?> InvokeAsync(string serviceKey, string methodKey, JsonNode? request, TaskExecutionContext executionContext,
        CancellationToken cancellationToken)
    {
        var key = string.IsNullOrWhiteSpace(methodKey) ? serviceKey.Trim() : $"{serviceKey}.{methodKey}";
        return _services.TryGetValue(key, out var service)
            ? service.ExecuteAsync(new TaskCallableExecutionContext(request, executionContext, db.Orm, serviceProvider), cancellationToken)
            : throw new InvalidOperationException($"Callable '{key}' is not registered.");
    }

    /// <summary>
    /// 构建接口调用索引
    /// </summary>
    /// <param name="services">接口调用服务集合</param>
    /// <returns>接口调用索引</returns>
    private static IReadOnlyDictionary<string, ITaskCallableService> BuildServiceIndex(IEnumerable<ITaskCallableService> services)
    {
        var index = new Dictionary<string, ITaskCallableService>(StringComparer.OrdinalIgnoreCase);
        foreach (var service in services)
        {
            AddIndex(index, $"{service.ServiceKey}.{service.MethodKey}", service);
            AddIndex(index, service.GetType().FullName, service);
            AddIndex(index, service.GetType().AssemblyQualifiedName, service);
        }

        return index;
    }

    /// <summary>
    /// 添加接口调用索引项
    /// </summary>
    /// <param name="index">接口调用索引</param>
    /// <param name="key">索引键</param>
    /// <param name="service">接口调用服务</param>
    private static void AddIndex(IDictionary<string, ITaskCallableService> index, string? key, ITaskCallableService service)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            index[key.Trim()] = service;
        }
    }
}