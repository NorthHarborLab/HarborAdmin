namespace HarborAdmin.AIWorker.Infrastructure;

/// <summary>
/// AI 供应商适配器解析器。
/// </summary>
public sealed class AiProviderAdapterResolver(IEnumerable<IAiProviderAdapter> adapters)
{
    private readonly IReadOnlyDictionary<string, IAiProviderAdapter> _adapters =
        adapters.ToDictionary(adapter => adapter.AdapterType, StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 解析适配器。
    /// </summary>
    public IAiProviderAdapter Resolve(string adapterType) =>
        _adapters.TryGetValue(adapterType, out var adapter)
            ? adapter
            : throw new InvalidOperationException($"AI adapter '{adapterType}' was not registered.");
}


