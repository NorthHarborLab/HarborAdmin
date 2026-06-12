using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.TaskOrchestration.Domain.Entities;
using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Application.Execution;

/// <summary>
/// DAG 定义校验器
/// </summary>
public sealed class TaskDagValidator(TaskConditionEvaluator conditionEvaluator)
{
    /// <summary>
    /// 校验任务 DAG 定义
    /// </summary>
    /// <param name="task">任务聚合</param>
    public void Validate(OrchestrationTask task)
    {
        if (string.IsNullOrWhiteSpace(task.TaskCode))
        {
            throw new ValidationDomainException("任务编码不能为空");
        }
        if (string.IsNullOrWhiteSpace(task.Name))
        {
            throw new ValidationDomainException("任务名称不能为空");
        }
        if (task.Nodes.Count == 0)
        {
            throw new ValidationDomainException("任务至少需要一个节点");
        }

        var nodeCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in task.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.NodeCode))
            {
                throw new ValidationDomainException("节点编码不能为空");
            }
            if (!nodeCodes.Add(node.NodeCode))
            {
                throw new ValidationDomainException($"节点编码 '{node.NodeCode}' 重复");
            }
            if (string.IsNullOrWhiteSpace(node.ExecutorType))
            {
                throw new ValidationDomainException($"节点 '{node.NodeCode}' 未配置执行器类型");
            }
        }

        foreach (var edge in task.Edges.Where(edge => edge.Enabled))
        {
            if (!nodeCodes.Contains(edge.SourceNodeCode) || !nodeCodes.Contains(edge.TargetNodeCode))
            {
                throw new ValidationDomainException($"连线 '{edge.EdgeCode}' 引用了不存在的节点");
            }
            if (string.Equals(edge.SourceNodeCode, edge.TargetNodeCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationDomainException($"连线 '{edge.EdgeCode}' 不能连接到自身");
            }
            conditionEvaluator.ValidateSyntax(edge.ConditionExpression);
        }

        var incoming = task.Edges.Where(edge => edge.Enabled)
            .GroupBy(edge => edge.TargetNodeCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
        if (task.Nodes.Where(node => node.Enabled).All(node => incoming.ContainsKey(node.NodeCode)))
        {
            throw new ValidationDomainException("DAG 至少需要一个起始节点");
        }

        EnsureAcyclic(task);
        ValidateTriggers(task);
    }

    /// <summary>
    /// 校验触发器定义
    /// </summary>
    /// <param name="task">任务聚合</param>
    private static void ValidateTriggers(OrchestrationTask task)
    {
        foreach (var trigger in task.Triggers)
        {
            if (string.IsNullOrWhiteSpace(trigger.TriggerCode))
            {
                throw new ValidationDomainException("触发器编码不能为空");
            }
            if (trigger.TriggerType == OrchestrationTriggerType.Cron && string.IsNullOrWhiteSpace(trigger.CronExpression))
            {
                throw new ValidationDomainException($"Cron 触发器 '{trigger.TriggerCode}' 未配置 Cron 表达式");
            }
            if (trigger.TriggerType == OrchestrationTriggerType.Cap && string.IsNullOrWhiteSpace(trigger.TriggerTopic))
            {
                throw new ValidationDomainException($"CAP 触发器 '{trigger.TriggerCode}' 未配置 topic");
            }
        }
    }

    /// <summary>
    /// 校验 DAG 不包含环
    /// </summary>
    /// <param name="task">任务聚合</param>
    private static void EnsureAcyclic(OrchestrationTask task)
    {
        var outgoing = task.Edges.Where(edge => edge.Enabled)
            .GroupBy(edge => edge.SourceNodeCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Select(edge => edge.TargetNodeCode).ToArray(), StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in task.Nodes.Where(node => node.Enabled))
        {
            Visit(node.NodeCode);
        }

        // 本地 DFS 只服务当前无环校验，避免把递归状态暴露到类级别
        void Visit(string nodeCode)
        {
            if (visited.Contains(nodeCode))
            {
                return;
            }
            if (!visiting.Add(nodeCode))
            {
                throw new ValidationDomainException("DAG 不能包含环");
            }
            if (outgoing.TryGetValue(nodeCode, out var targets))
            {
                foreach (var target in targets)
                {
                    Visit(target);
                }
            }
            visiting.Remove(nodeCode);
            visited.Add(nodeCode);
        }
    }
}
