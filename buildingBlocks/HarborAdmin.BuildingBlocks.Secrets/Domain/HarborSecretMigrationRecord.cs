using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.BuildingBlocks.Secrets.Domain;

/// <summary>
/// 密钥迁移审计记录，仅记录来源和明文哈希，不保存明文。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_harbor_secret_migration_source", "SourceTable,SourceField,SourceId,SourcePath,PlainTextHash", true)]
public class HarborSecretMigrationRecord : EntityBase
{
    /// <summary>
    /// 来源表。
    /// </summary>
    public string SourceTable { get; set; } = string.Empty;

    /// <summary>
    /// 来源字段。
    /// </summary>
    public string SourceField { get; set; } = string.Empty;

    /// <summary>
    /// 来源数据主键。
    /// </summary>
    public string SourceId { get; set; } = string.Empty;

    /// <summary>
    /// 来源路径；JSON 内部字段使用该字段记录路径。
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// 目标 SecretRef。
    /// </summary>
    public string SecretRef { get; set; } = string.Empty;

    /// <summary>
    /// 目标版本。
    /// </summary>
    public int SecretVersion { get; set; }

    /// <summary>
    /// 明文 SHA-256 哈希。
    /// </summary>
    public string PlainTextHash { get; set; } = string.Empty;

    /// <summary>
    /// 迁移时间。
    /// </summary>
    public DateTimeOffset MigratedAt { get; set; }
}
