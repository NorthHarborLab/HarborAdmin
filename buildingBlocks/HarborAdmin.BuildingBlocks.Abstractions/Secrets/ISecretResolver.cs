namespace HarborAdmin.BuildingBlocks.Abstractions.Secrets;

/// <summary>
/// 密钥解析器。
/// </summary>
public interface ISecretResolver
{
    /// <summary>
    /// 解析密钥明文。
    /// </summary>
    Task<string?> ResolveAsync(string secretRef, CancellationToken cancellationToken = default);
}
