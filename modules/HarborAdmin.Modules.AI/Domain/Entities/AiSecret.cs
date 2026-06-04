using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI 密钥。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_ai_secret_ref", "SecretRef", true)]
public class AiSecret : EntityBase
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
    /// 加密密文。
    /// </summary>
    [Column(StringLength = -1)]
    public string CipherText { get; set; } = string.Empty;

    /// <summary>
    /// 密钥版本。
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
