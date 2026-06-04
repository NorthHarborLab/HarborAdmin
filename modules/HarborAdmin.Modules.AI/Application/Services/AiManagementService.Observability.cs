using HarborAdmin.Modules.AI.Contracts.Dtos;
using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Services;

public sealed partial class AiManagementService
{
    /// <summary>
    /// 列出调用日志。
    /// </summary>
    public async Task<IReadOnlyList<AiInvocationLogDto>> ListInvocationLogsAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListInvocationLogsAsync(cancellationToken))
        .Select(log => mapper.Map<AiInvocationLogDto>(log))
        .ToList();

    /// <summary>
    /// 列出用量。
    /// </summary>
    public async Task<IReadOnlyList<AiUsageLedgerDto>> ListUsageAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListQuotaBucketsAsync(cancellationToken))
        .Select(bucket => mapper.Map<AiUsageLedgerDto>(bucket))
        .ToList();
}
