using HarborAdmin.Modules.TaskOrchestration.Domain.Enums;

namespace HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;

/// <summary>
/// 节点运行日志 DTO
/// </summary>
/// <param name="Id">节点运行记录 ID</param>
/// <param name="NodeCode">节点编码</param>
/// <param name="NodeName">节点名称</param>
/// <param name="ExecutorType">执行器类型</param>
/// <param name="Status">运行状态</param>
/// <param name="CreatedAt">创建时间</param>
/// <param name="StartedAt">开始时间</param>
/// <param name="FinishedAt">完成时间</param>
/// <param name="DurationMilliseconds">执行耗时毫秒数</param>
/// <param name="InputJson">节点输入 JSON</param>
/// <param name="OutputJson">节点输出 JSON</param>
/// <param name="ErrorMessage">错误信息</param>
public sealed record OrchestrationNodeRunDto(
    long Id,
    string NodeCode,
    string NodeName,
    string ExecutorType,
    OrchestrationRunStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int DurationMilliseconds,
    string? InputJson,
    string? OutputJson,
    string? ErrorMessage);
