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

    /// <summary>
    /// 按指定版本解析密钥明文；<paramref name="version"/> 为空时解析当前版本。
    /// </summary>
    Task<string?> ResolveAsync(string secretRef, int? version, CancellationToken cancellationToken = default);
}
