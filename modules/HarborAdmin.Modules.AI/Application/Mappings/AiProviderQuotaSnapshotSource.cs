using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Mappings;

/// <summary>
/// 供应商配额快照映射源。
/// </summary>
public sealed record AiProviderQuotaSnapshotSource(AiProviderQuota Quota, string ProviderKey);
