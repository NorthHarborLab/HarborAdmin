using HarborAdmin.Modules.Secrets.Domain.Entities;

namespace HarborAdmin.Modules.Secrets.Application.Abstractions;

/// <summary>
/// Secrets 密钥仓储。
/// </summary>
public partial interface ISecretsRepository
{
    /// <summary>
    /// 按引用排序列出全部密钥。
    /// </summary>
    Task<IReadOnlyList<HarborSecret>> ListOrderedByRefAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按引用获取密钥。
    /// </summary>
    Task<HarborSecret?> GetByRefAsync(string secretRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按引用与版本获取历史版本。
    /// </summary>
    Task<HarborSecretVersion?> GetVersionByRefAndVersionAsync(string secretRef, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定引用的最大版本号。
    /// </summary>
    Task<int> GetMaxVersionAsync(string secretRef, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增密钥。
    /// </summary>
    Task<HarborSecret> InsertSecretAsync(HarborSecret secret, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新密钥。
    /// </summary>
    Task UpdateSecretAsync(HarborSecret secret, CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增历史版本。
    /// </summary>
    Task InsertVersionAsync(HarborSecretVersion version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 保存或轮换密钥密文（事务内写入版本与当前记录）。
    /// </summary>
    Task<HarborSecret> SaveRotationAsync(string secretRef, string displayName, string cipherText, CancellationToken cancellationToken = default);
}
