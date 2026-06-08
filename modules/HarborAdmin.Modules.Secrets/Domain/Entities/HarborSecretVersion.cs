using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Secrets.Domain.Entities;

/// <summary>
/// 通用密钥历史版本密文。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_harbor_secret_version_ref_version", $"{nameof(SecretRef)},{nameof(Version)}", true)]
public sealed class HarborSecretVersion : EntityBase
{
    /// <summary>
    /// 密钥引用。
    /// </summary>
    public string SecretRef { get; set; } = string.Empty;

    /// <summary>
    /// 版本号。
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// 版本密文。
    /// </summary>
    [Column(StringLength = -1)]
    public string CipherText { get; set; } = string.Empty;

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
}
