namespace HarborAdmin.Modules.AI.Contracts.Snapshots;

/// <summary>
/// AI 已发布配置快照。
/// </summary>
public sealed record AiConfigSnapshot(
    int Version,
    IReadOnlyList<AiProviderSnapshot> Providers,
    IReadOnlyList<AiBusinessSnapshot> Businesses,
    IReadOnlyList<AiPromptSnapshot> Prompts,
    IReadOnlyList<AiKnowledgeSnapshot> KnowledgeBases,
    IReadOnlyList<AiProviderQuotaSnapshot> ProviderQuotas,
    IReadOnlyList<AiModelQuotaSnapshot> ModelQuotas);
