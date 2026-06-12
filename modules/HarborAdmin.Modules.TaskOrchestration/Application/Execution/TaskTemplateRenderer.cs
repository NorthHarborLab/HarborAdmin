using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.RegularExpressions;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Execution;

/// <summary>
/// 任务模板变量渲染器
/// </summary>
public sealed partial class TaskTemplateRenderer : ITaskTemplateRenderer
{
    /// <summary>
    /// 渲染模板变量
    /// </summary>
    /// <param name="template">模板文本</param>
    /// <param name="context">任务执行上下文</param>
    /// <returns>渲染后的文本</returns>
    public string Render(string template, TaskExecutionContext context)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        return TemplateRegex().Replace(template, match =>
        {
            var path = match.Groups["path"].Value.Trim();
            var value = ResolvePath(path, context);
            return value switch
            {
                null => string.Empty,
                JsonValue jsonValue => jsonValue.ToString(),
                _ => value.ToJsonString(),
            };
        });
    }

    /// <summary>
    /// 从任务执行上下文解析变量路径
    /// </summary>
    /// <param name="path">变量路径</param>
    /// <param name="context">任务执行上下文</param>
    /// <returns>解析出的 JSON 节点</returns>
    internal static JsonNode? ResolvePath(string path, TaskExecutionContext context)
    {
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return null;
        }

        JsonNode? current = null;
        var start = 1;
        if (string.Equals(parts[0], "params", StringComparison.OrdinalIgnoreCase))
        {
            current = context.Params;
        }
        else if (parts.Length >= 3 && string.Equals(parts[0], "nodes", StringComparison.OrdinalIgnoreCase))
        {
            if (!context.Nodes.TryGetValue(parts[1], out var node))
            {
                return null;
            }
            if (string.Equals(parts[2], "status", StringComparison.OrdinalIgnoreCase))
            {
                current = JsonValue.Create(node.Status);
                start = 3;
            }
            else if (string.Equals(parts[2], "output", StringComparison.OrdinalIgnoreCase))
            {
                current = node.Output;
                start = 3;
            }
        }
        else if (string.Equals(parts[0], "context", StringComparison.OrdinalIgnoreCase))
        {
            current = JsonSerializer.SerializeToNode(new { nodes = context.Nodes });
        }

        for (var i = start; i < parts.Length && current is not null; i++)
        {
            current = current[parts[i]];
        }

        return current;
    }

    /// <summary>
    /// 获取模板变量正则
    /// </summary>
    /// <returns>模板变量正则</returns>
    [GeneratedRegex(@"\{\{\s*(?<path>[^}]+)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex TemplateRegex();
}
