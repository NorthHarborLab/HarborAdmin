using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Node;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Execution;

/// <summary>
/// HTTP 节点执行器
/// </summary>
public sealed class TaskHttpStepExecutor(IHttpClientFactory httpClientFactory, ITaskTemplateRenderer renderer) : ITaskStepExecutor
{
    /// <summary>
    /// HTTP 客户端名称
    /// </summary>
    public const string HttpClientName = "task-orchestration-http";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 执行器类型
    /// </summary>
    public string ExecutorType => OrchestrationExecutorTypes.Http;

    /// <summary>
    /// 执行 HTTP 节点
    /// </summary>
    /// <param name="context">节点执行上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>HTTP 响应摘要</returns>
    public async Task<JsonNode?> ExecuteAsync(TaskNodeExecutionContext context, CancellationToken cancellationToken)
    {
        var config = JsonSerializer.Deserialize<HttpNodeConfig>(context.ConfigJson ?? "{}", JsonOptions) ?? new HttpNodeConfig();
        var method = string.IsNullOrWhiteSpace(config.Method) ? HttpMethod.Get : new HttpMethod(config.Method.Trim().ToUpperInvariant());
        var url = renderer.Render(config.Url ?? string.Empty, context.ExecutionContext);
        using var request = new HttpRequestMessage(method, url);

        foreach (var header in config.Headers ?? [])
        {
            var value = renderer.Render(header.Value ?? string.Empty, context.ExecutionContext);
            if (!request.Headers.TryAddWithoutValidation(header.Key, value))
            {
                request.Content ??= new StringContent(string.Empty);
                request.Content.Headers.TryAddWithoutValidation(header.Key, value);
            }
        }

        if (!string.IsNullOrWhiteSpace(config.Body))
        {
            var body = renderer.Render(config.Body, context.ExecutionContext);
            request.Content = new StringContent(body, Encoding.UTF8, config.ContentType ?? "application/json");
        }

        var client = httpClientFactory.CreateClient(HttpClientName);
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(config.TimeoutSeconds ?? 30, 1, 600));
        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.SerializeToNode(new
        {
            statusCode = (int)response.StatusCode,
            success = response.IsSuccessStatusCode,
            body = Truncate(responseBody),
        });
    }

    /// <summary>
    /// 截断过长响应文本
    /// </summary>
    /// <param name="value">响应文本</param>
    /// <returns>截断后的文本</returns>
    private static string Truncate(string value) => value.Length <= 8000 ? value : value[..8000];
}
