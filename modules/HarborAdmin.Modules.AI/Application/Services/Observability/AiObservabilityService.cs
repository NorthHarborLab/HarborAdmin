using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Contracts.Observability.Dto;

namespace HarborAdmin.Modules.AI.Application.Services.Observability;

/// <summary>
/// AI 可观测性服务。
/// </summary>
public sealed class AiObservabilityService(IAiRepository repository, IHarborMapper mapper)
{
    /// <summary>
    /// 列出调用日志。
    /// </summary>
    public async Task<IReadOnlyList<AiInvocationLogDto>> ListInvocationLogsAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListInvocationLogsAsync(cancellationToken))
        .Select(mapper.Map<AiInvocationLogDto>)
        .ToList();

    /// <summary>
    /// 列出用量。
    /// </summary>
    public async Task<IReadOnlyList<AiUsageLedgerDto>> ListUsageAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListQuotaBucketsAsync(cancellationToken))
        .Select(mapper.Map<AiUsageLedgerDto>)
        .ToList();
}
