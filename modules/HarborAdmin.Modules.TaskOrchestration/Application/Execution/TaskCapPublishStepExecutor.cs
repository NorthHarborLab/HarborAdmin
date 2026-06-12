using System.Text.Json;
using System.Text.Json.Nodes;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Node;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Execution;

/// <summary>
/// CAP 节点执行器
/// </summary>
public sealed class TaskCapPublishStepExecutor : ITaskStepExecutor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string RequestReplyMode = "requestReply";
    private readonly IEventPublisher _publisher;
    private readonly IEventRequestClient? _requestClient;
    private readonly ITaskTemplateRenderer _renderer;

    /// <summary>
    /// 初始化 CAP 节点执行器
    /// </summary>
    /// <param name="publisher">事件发布器</param>
    /// <param name="requestClients">可选 CAP 请求响应客户端</param>
    /// <param name="renderer">任务模板渲染器</param>
    public TaskCapPublishStepExecutor(IEventPublisher publisher, IEnumerable<IEventRequestClient> requestClients, ITaskTemplateRenderer renderer)
    {
        _publisher = publisher;
        _requestClient = requestClients.FirstOrDefault();
        _renderer = renderer;
    }

    /// <summary>
    /// 执行器类型
    /// </summary>
    public string ExecutorType => OrchestrationExecutorTypes.Cap;

    /// <summary>
    /// 执行 CAP 节点
    /// </summary>
    /// <param name="context">节点执行上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>CAP 发布摘要或请求响应结果</returns>
    public async Task<JsonNode?> ExecuteAsync(TaskNodeExecutionContext context, CancellationToken cancellationToken)
    {
        var config = JsonSerializer.Deserialize<CapNodeConfig>(context.ConfigJson ?? "{}", JsonOptions) ?? new CapNodeConfig();
        var topic = _renderer.Render(config.Topic ?? string.Empty, context.ExecutionContext);
        var payloadText = _renderer.Render(config.PayloadJson ?? "{}", context.ExecutionContext);
        var payload = JsonNode.Parse(string.IsNullOrWhiteSpace(payloadText) ? "{}" : payloadText) ?? new JsonObject();
        if (IsRequestReplyMode(config.Mode))
        {
            if (_requestClient is null)
            {
                throw new InvalidOperationException("CAP Request/Reply is not enabled.");
            }

            var timeout = TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds ?? context.TimeoutSeconds, 1, 3600));
            return await _requestClient.RequestAsync<JsonNode, JsonNode>(topic, payload, timeout, cancellationToken);
        }

        await _publisher.PublishAsync(topic, payload, cancellationToken);
        return JsonSerializer.SerializeToNode(new { topic, published = true });
    }

    /// <summary>
    /// 判断 CAP 节点是否使用请求响应模式
    /// </summary>
    /// <param name="mode">模式配置</param>
    /// <returns>是否请求响应模式</returns>
    private static bool IsRequestReplyMode(string? mode) => string.Equals(mode, RequestReplyMode, StringComparison.OrdinalIgnoreCase);
}
