namespace HarborAdmin.BuildingBlocks.Abstractions.Secrets;

/// <summary>
/// 密钥存储。
/// </summary>
public interface ISecretStore : ISecretResolver
{
    /// <summary>
    /// 列出所有密钥描述，不返回明文。
    /// </summary>
    Task<IReadOnlyList<SecretDescriptor>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按引用获取密钥描述，不返回明文。
    /// </summary>
    Task<SecretDescriptor?> GetAsync(string secretRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按引用和版本获取密钥版本描述，不返回明文。
    /// </summary>
    Task<SecretVersionDescriptor?> GetVersionAsync(string secretRef, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    Task<SecretDescriptor> SaveAsync(string secretRef, string displayName, string secretValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// 仅在明文变化时保存新版本；明文相同时只更新显示名并复用当前版本。
    /// </summary>
    Task<SecretDescriptor> SaveIfChangedAsync(string secretRef, string displayName, string secretValue, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用或禁用密钥。
    /// </summary>
    Task<SecretDescriptor> SetEnabledAsync(string secretRef, bool enabled, CancellationToken cancellationToken = default);
}
