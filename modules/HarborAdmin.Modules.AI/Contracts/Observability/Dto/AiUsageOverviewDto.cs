namespace HarborAdmin.Modules.AI.Contracts.Observability.Dto;

/// <summary>
/// AI 用量概览 KPI。
/// </summary>
public sealed record AiUsageOverviewDto(
    int RequestCount,
    int SuccessCount,
    int FailedCount,
    decimal SuccessRate,
    int TotalTokens,
    decimal Cost);
