using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Contracts.Observability.Dto;
using HarborAdmin.Modules.AI.Contracts.Observability.Request;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services.Observability;

/// <summary>
/// AI 可观测性服务。
/// </summary>
public sealed class AiObservabilityService(IAiInvocationRepository invocationRepository, IAiQuotaRepository quotaRepository, IHarborMapper mapper)
{
    /// <summary>
    /// 列出调用日志。
    /// </summary>
    public async Task<IReadOnlyList<AiInvocationLogDto>> ListInvocationLogsAsync(CancellationToken cancellationToken = default) =>
        (await invocationRepository.ListInvocationLogsAsync(cancellationToken))
        .Select(mapper.Map<AiInvocationLogDto>)
        .ToList();

    /// <summary>
    /// 列出用量（兼容旧接口，返回原始日桶映射）。
    /// </summary>
    public async Task<IReadOnlyList<AiUsageLedgerDto>> ListUsageAsync(CancellationToken cancellationToken = default) =>
        (await quotaRepository.ListQuotaBucketsAsync(cancellationToken))
        .Select(mapper.Map<AiUsageLedgerDto>)
        .ToList();

    /// <summary>
    /// 获取用量概览 KPI。
    /// </summary>
    public async Task<AiUsageOverviewDto> GetUsageOverviewAsync(
        AiUsageSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var buckets = await LoadUsageBucketsAsync(query, cancellationToken);
        return BuildOverview(buckets);
    }

    /// <summary>
    /// 分页获取用量聚合明细。
    /// </summary>
    public async Task<PagedResult<AiUsageSummaryDto>> PageUsageSummaryAsync(
        AiUsageSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var buckets = await LoadUsageBucketsAsync(query, cancellationToken);
        var rows = AggregateUsageRows(buckets, query.GroupBy)
            .OrderByDescending(row => row.WindowStart ?? DateTimeOffset.MinValue)
            .ThenBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var pageItems = rows.Skip(query.Skip).Take(query.PageSize).ToList();
        return PagedResult<AiUsageSummaryDto>.From(pageItems, rows.Count);
    }

    /// <summary>
    /// 加载区间内日窗口桶。
    /// </summary>
    private async Task<IReadOnlyList<AiQuotaBucket>> LoadUsageBucketsAsync(
        AiUsageSummaryQuery query,
        CancellationToken cancellationToken)
    {
        var (dateFrom, dateToExclusive) = ResolveDateRange(query);
        return await quotaRepository.ListUsageQuotaBucketsAsync(
            dateFrom,
            dateToExclusive,
            query.BusinessKey,
            query.ProducerKey,
            query.ProviderKey,
            query.Model,
            cancellationToken);
    }

    /// <summary>
    /// 解析查询日期区间。
    /// </summary>
    private static (DateTimeOffset DateFrom, DateTimeOffset DateToExclusive) ResolveDateRange(AiUsageSummaryQuery query)
    {
        var utcNow = DateTimeOffset.UtcNow;
        var dateTo = query.DateTo ?? utcNow;
        var dateFrom = query.DateFrom ?? utcNow.AddDays(-6).Date;
        var dateToExclusive = new DateTimeOffset(dateTo.Year, dateTo.Month, dateTo.Day, 0, 0, 0, TimeSpan.Zero)
            .AddDays(1);
        var normalizedFrom = new DateTimeOffset(dateFrom.Year, dateFrom.Month, dateFrom.Day, 0, 0, 0, TimeSpan.Zero);
        return (normalizedFrom, dateToExclusive);
    }

    /// <summary>
    /// 构建概览 KPI。
    /// </summary>
    private static AiUsageOverviewDto BuildOverview(IReadOnlyList<AiQuotaBucket> buckets)
    {
        var successCount = buckets.Sum(bucket => bucket.SuccessRequests);
        var failedCount = buckets.Sum(bucket => bucket.FailedRequests);
        var requestCount = successCount + failedCount;
        return new AiUsageOverviewDto(
            requestCount,
            successCount,
            failedCount,
            CalculateSuccessRate(successCount, failedCount),
            buckets.Sum(bucket => bucket.TotalTokens),
            buckets.Sum(bucket => bucket.Cost));
    }

    /// <summary>
    /// 按维度聚合用量行。
    /// </summary>
    private static IEnumerable<AiUsageSummaryDto> AggregateUsageRows(
        IReadOnlyList<AiQuotaBucket> buckets,
        string? groupBy)
    {
        var normalizedGroupBy = string.IsNullOrWhiteSpace(groupBy)
            ? "day"
            : groupBy.Trim().ToLowerInvariant();

        return normalizedGroupBy switch
        {
            "business" => buckets
                .GroupBy(bucket => bucket.BusinessKey, StringComparer.OrdinalIgnoreCase)
                .Select(group => ToSummaryDto(
                    group.Key,
                    group.Key,
                    null,
                    null,
                    null,
                    null,
                    group)),
            "provider" => buckets
                .GroupBy(
                    bucket => $"{bucket.ProviderKey}\u001F{bucket.Model ?? string.Empty}",
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var sample = group.First();
                    return ToSummaryDto(
                        $"{sample.ProviderKey} / {sample.Model ?? "*"}",
                        null,
                        null,
                        sample.ProviderKey,
                        sample.Model,
                        null,
                        group);
                }),
            _ => buckets
                .GroupBy(
                    bucket =>
                        $"{bucket.WindowStart:yyyy-MM-dd}\u001F{bucket.BusinessKey}\u001F{bucket.ProducerKey}\u001F{bucket.ProviderKey}\u001F{bucket.Model ?? string.Empty}",
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var sample = group.First();
                    var dayStart = new DateTimeOffset(
                        sample.WindowStart.Year,
                        sample.WindowStart.Month,
                        sample.WindowStart.Day,
                        0,
                        0,
                        0,
                        TimeSpan.Zero);
                    return ToSummaryDto(
                        dayStart.ToString("yyyy-MM-dd"),
                        sample.BusinessKey,
                        sample.ProducerKey,
                        sample.ProviderKey,
                        sample.Model,
                        dayStart,
                        group);
                }),
        };
    }

    /// <summary>
    /// 将分组桶映射为汇总 DTO。
    /// </summary>
    private static AiUsageSummaryDto ToSummaryDto(
        string label,
        string? businessKey,
        string? producerKey,
        string? providerKey,
        string? model,
        DateTimeOffset? windowStart,
        IEnumerable<AiQuotaBucket> buckets)
    {
        var bucketList = buckets.ToList();
        var successCount = bucketList.Sum(bucket => bucket.SuccessRequests);
        var failedCount = bucketList.Sum(bucket => bucket.FailedRequests);
        return new AiUsageSummaryDto(
            label,
            businessKey,
            producerKey,
            providerKey,
            model,
            windowStart,
            successCount + failedCount,
            successCount,
            failedCount,
            CalculateSuccessRate(successCount, failedCount),
            bucketList.Sum(bucket => bucket.TotalTokens),
            bucketList.Sum(bucket => bucket.Cost));
    }

    /// <summary>
    /// 计算成功率。
    /// </summary>
    private static decimal CalculateSuccessRate(int successCount, int failedCount)
    {
        var total = successCount + failedCount;
        return total <= 0 ? 0m : Math.Round((decimal)successCount / total, 4);
    }
}
