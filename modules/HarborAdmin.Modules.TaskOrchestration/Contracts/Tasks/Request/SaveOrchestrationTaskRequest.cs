using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Request;

/// <summary>
/// 保存编排任务请求
/// </summary>
public sealed class SaveOrchestrationTaskRequest : IValidatableObject
{
    /// <summary>
    /// 任务编码
    /// </summary>
    [Required(ErrorMessage = "任务编码不能为空")]
    [MaxLength(64, ErrorMessage = "任务编码不能超过 64 个字符")]
    public string TaskCode { get; set; } = string.Empty;

    /// <summary>
    /// 任务名称
    /// </summary>
    [Required(ErrorMessage = "任务名称不能为空")]
    [MaxLength(128, ErrorMessage = "任务名称不能超过 128 个字符")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 任务说明
    /// </summary>
    [MaxLength(512, ErrorMessage = "任务说明不能超过 512 个字符")]
    public string? Description { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 是否允许并发运行
    /// </summary>
    public bool AllowConcurrentRuns { get; set; }

    /// <summary>
    /// 默认参数 JSON
    /// </summary>
    public string? DefaultParamsJson { get; set; }

    /// <summary>
    /// 参数 Schema JSON
    /// </summary>
    public string? ParamSchemaJson { get; set; }

    /// <summary>
    /// 触发器集合
    /// </summary>
    public List<SaveOrchestrationTaskTriggerRequest> Triggers { get; set; } = [];

    /// <summary>
    /// DAG 节点集合
    /// </summary>
    public List<SaveOrchestrationTaskNodeRequest> Nodes { get; set; } = [];

    /// <summary>
    /// DAG 连线集合
    /// </summary>
    public List<SaveOrchestrationTaskEdgeRequest> Edges { get; set; } = [];

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Nodes.Count == 0)
        {
            yield return new ValidationResult("任务至少需要一个节点", [nameof(Nodes)]);
            yield break;
        }

        foreach (var result in ValidateNodeCodes())
        {
            yield return result;
        }

        foreach (var result in ValidateEdges())
        {
            yield return result;
        }
    }

    /// <summary>
    /// 校验节点编码集合
    /// </summary>
    /// <returns>验证结果集合</returns>
    private IEnumerable<ValidationResult> ValidateNodeCodes()
    {
        var nodeCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < Nodes.Count; index++)
        {
            var node = Nodes[index];
            if (string.IsNullOrWhiteSpace(node.NodeCode))
            {
                yield return new ValidationResult($"第 {index + 1} 个节点编码不能为空", [nameof(Nodes)]);
                continue;
            }

            if (!nodeCodes.Add(node.NodeCode.Trim()))
            {
                yield return new ValidationResult($"节点编码 '{node.NodeCode}' 重复", [nameof(Nodes)]);
            }
        }
    }

    /// <summary>
    /// 校验节点连线
    /// </summary>
    /// <returns>验证结果集合</returns>
    private IEnumerable<ValidationResult> ValidateEdges()
    {
        var nodeCodes = Nodes
            .Where(item => !string.IsNullOrWhiteSpace(item.NodeCode))
            .Select(item => item.NodeCode.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var edgePairs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var edge in Edges.Where(item => item.Enabled))
        {
            if (string.IsNullOrWhiteSpace(edge.SourceNodeCode) || string.IsNullOrWhiteSpace(edge.TargetNodeCode))
            {
                yield return new ValidationResult("启用连线必须配置上游和下游节点编码", [nameof(Edges)]);
                continue;
            }

            var source = edge.SourceNodeCode.Trim();
            var target = edge.TargetNodeCode.Trim();
            if (!nodeCodes.Contains(source) || !nodeCodes.Contains(target))
            {
                yield return new ValidationResult($"连线 '{edge.EdgeCode}' 引用了不存在的节点", [nameof(Edges)]);
            }

            if (string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
            {
                yield return new ValidationResult($"连线 '{edge.EdgeCode}' 不能连接到自身", [nameof(Edges)]);
            }

            if (!edgePairs.Add($"{source}->{target}"))
            {
                yield return new ValidationResult($"连线 '{source}->{target}' 重复", [nameof(Edges)]);
            }
        }
    }
}
