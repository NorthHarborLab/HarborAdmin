using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Secrets.Domain.Entities;

/// <summary>
/// 通用密钥当前元数据。
/// </summary>
[Index("ux_harbor_secret_ref", nameof(SecretRef), true)]
public sealed class HarborSecret : AuditableEntity
{
    /// <summary>
    /// 密钥引用。
    /// </summary>
    public string SecretRef { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称。
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 当前版本密文。
    /// </summary>
    [Column(StringLength = -1)]
    public string CipherText { get; set; } = string.Empty;

    /// <summary>
    /// 当前版本号。
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;
}
