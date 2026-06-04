using HarborAdmin.Modules.AI.Domain.Entities;

namespace HarborAdmin.Modules.AI.Application.Abstractions;

public partial interface IAiRepository
{
    /// <summary>
    /// 列出密钥。
    /// </summary>
    Task<IReadOnlyList<AiSecret>> ListSecretsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按引用获取密钥。
    /// </summary>
    Task<AiSecret?> GetSecretByRefAsync(string secretRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存密钥。
    /// </summary>
    Task<AiSecret> SaveSecretAsync(AiSecret secret, CancellationToken cancellationToken = default);
}
