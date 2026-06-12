using System.Globalization;
using System.Text.RegularExpressions;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Context;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Execution;

/// <summary>
/// 白名单条件表达式求值器
/// </summary>
public sealed partial class TaskConditionEvaluator
{
    /// <summary>
    /// 校验条件表达式语法是否属于白名单
    /// </summary>
    /// <param name="expression">条件表达式</param>
    public void ValidateSyntax(string? expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return;
        }

        var trimmed = expression.Trim();
        if (trimmed.StartsWith("exists(", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(')'))
        {
            ValidatePath(trimmed[7..^1].Trim());
            return;
        }
        if (trimmed.StartsWith("empty(", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(')'))
        {
            ValidatePath(trimmed[6..^1].Trim());
            return;
        }
        if (!CompareRegex().IsMatch(trimmed))
        {
            throw new ValidationDomainException("条件表达式只支持 == != > >= < <= contains exists() empty()");
        }
    }

    /// <summary>
    /// 求值条件表达式
    /// </summary>
    /// <param name="expression">条件表达式</param>
    /// <param name="context">任务执行上下文</param>
    /// <returns>条件是否成立</returns>
    public bool Evaluate(string? expression, TaskExecutionContext context)
    {
        if (string.IsNullOrWhiteSpace(expression))
        {
            return true;
        }

        ValidateSyntax(expression);
        var trimmed = expression.Trim();
        if (trimmed.StartsWith("exists(", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(')'))
        {
            var path = trimmed[7..^1].Trim();
            return TaskTemplateRenderer.ResolvePath(path, context) is not null;
        }
        if (trimmed.StartsWith("empty(", StringComparison.OrdinalIgnoreCase) && trimmed.EndsWith(')'))
        {
            var path = trimmed[6..^1].Trim();
            return string.IsNullOrWhiteSpace(TaskTemplateRenderer.ResolvePath(path, context)?.ToString());
        }

        var match = CompareRegex().Match(trimmed);
        if (!match.Success)
        {
            throw new ValidationDomainException("条件表达式只支持 == != > >= < <= contains exists() empty()");
        }

        var left = TaskTemplateRenderer.ResolvePath(match.Groups["left"].Value, context)?.ToString();
        var op = match.Groups["op"].Value;
        var right = Unquote(match.Groups["right"].Value);

        return op switch
        {
            "==" => string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "!=" => !string.Equals(left, right, StringComparison.OrdinalIgnoreCase),
            "contains" => left?.Contains(right, StringComparison.OrdinalIgnoreCase) == true,
            ">" => CompareNumber(left, right) > 0,
            ">=" => CompareNumber(left, right) >= 0,
            "<" => CompareNumber(left, right) < 0,
            "<=" => CompareNumber(left, right) <= 0,
            _ => false,
        };
    }

    /// <summary>
    /// 按十进制数字比较左右值
    /// </summary>
    /// <param name="left">左值文本</param>
    /// <param name="right">右值文本</param>
    /// <returns>数字比较结果</returns>
    private static int CompareNumber(string? left, string right)
    {
        _ = decimal.TryParse(left, NumberStyles.Any, CultureInfo.InvariantCulture, out var leftValue);
        _ = decimal.TryParse(right, NumberStyles.Any, CultureInfo.InvariantCulture, out var rightValue);
        return leftValue.CompareTo(rightValue);
    }

    /// <summary>
    /// 去除字符串字面量外层引号
    /// </summary>
    /// <param name="value">字符串字面量</param>
    /// <returns>去除引号后的文本</returns>
    private static string Unquote(string value)
    {
        var trimmed = value.Trim();
        return trimmed.Length >= 2 && ((trimmed[0] == '"' && trimmed[^1] == '"') || (trimmed[0] == '\'' && trimmed[^1] == '\''))
            ? trimmed[1..^1]
            : trimmed;
    }

    /// <summary>
    /// 校验变量路径是否属于允许访问的上下文范围
    /// </summary>
    /// <param name="path">变量路径</param>
    private static void ValidatePath(string path)
    {
        if (!PathRegex().IsMatch(path))
        {
            throw new ValidationDomainException("条件表达式只允许访问 params、context、nodes.{code}.output/status 路径");
        }
    }

    /// <summary>
    /// 获取条件比较表达式正则
    /// </summary>
    /// <returns>条件比较表达式正则</returns>
    [GeneratedRegex("^(?<left>(params|context|nodes)\\.[A-Za-z0-9_.-]+)\\s*(?<op>==|!=|>=|<=|>|<|contains)\\s*(?<right>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex CompareRegex();

    /// <summary>
    /// 获取允许变量路径正则
    /// </summary>
    /// <returns>允许变量路径正则</returns>
    [GeneratedRegex("^(params|context|nodes)\\.[A-Za-z0-9_.-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex PathRegex();
}
