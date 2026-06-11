namespace HarborAdmin.Client.AI.Clients;

/// <summary>
/// AI 业务签名密钥解析结果。
/// </summary>
/// <param name="RequiresSignature">业务是否要求签名校验。</param>
/// <param name="Secret">签名密钥明文；未配置时为空。</param>
public sealed record AiBusinessSigningSecretInfo(bool RequiresSignature, string? Secret);

/// <summary>
/// 按 AI 业务 Key 解析签名密钥。
/// </summary>
public interface IAiBusinessSigningSecretResolver
{
    /// <summary>
    /// 解析业务绑定的签名密钥。
    /// </summary>
    /// <param name="businessKey">业务 Key。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>解析结果；未知业务时返回 null。</returns>
    Task<AiBusinessSigningSecretInfo?> ResolveAsync(string businessKey, CancellationToken cancellationToken = default);
}
