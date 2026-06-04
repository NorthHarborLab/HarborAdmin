using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.BuildingBlocks.Secrets.Domain;

/// <summary>
/// 通用密钥当前元数据。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_harbor_secret_ref", "SecretRef", true)]
public class HarborSecret : EntityBase
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

    /// <summary>
    /// 创建时间。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
