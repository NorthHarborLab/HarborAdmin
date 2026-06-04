using FreeSql;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.ConfigCenter.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Migrations;

/// <summary>
/// 清理配置中心旧 Environment 维度数据,并在结构同步前尽量移除旧列。
/// </summary>
public sealed class ConfigCenterLegacyEnvironmentMigration(
    ILogger<ConfigCenterLegacyEnvironmentMigration> logger) : IHarborFreeSqlPreSyncHook
{
    private static readonly Type[] ConfigCenterEntityTypes =
    [
        typeof(ConfigItem),
        typeof(ConfigRelease),
        typeof(ConfigReleaseItem),
        typeof(ConfigApplication)
    ];

    /// <inheritdoc />
    public void BeforeSyncStructure(IFreeSql freeSql, string dbKey, IReadOnlyList<Type> entityTypes)
    {
        if (!entityTypes.Any(type => ConfigCenterEntityTypes.Contains(type)))
        {
            return;
        }

        ExecuteIgnoreMissing(freeSql,
            """
            DELETE FROM "ConfigReleaseItem"
            WHERE "ReleaseId" IN (
                SELECT "Id"
                FROM "ConfigRelease"
                WHERE "Environment" IS NOT NULL AND "Environment" <> 'Development'
            )
            """);
        ExecuteIgnoreMissing(freeSql,
            """
            DELETE FROM "ConfigRelease"
            WHERE "Environment" IS NOT NULL AND "Environment" <> 'Development'
            """);
        ExecuteIgnoreMissing(freeSql,
            """
            DELETE FROM "ConfigItem"
            WHERE "Environment" IS NOT NULL AND "Environment" <> 'Development'
            """);
        ExecuteIgnoreMissing(freeSql, "ALTER TABLE \"ConfigItem\" DROP COLUMN \"Environment\"");
        ExecuteIgnoreMissing(freeSql, "ALTER TABLE \"ConfigRelease\" DROP COLUMN \"Environment\"");
    }

    private void ExecuteIgnoreMissing(IFreeSql freeSql, string sql)
    {
        try
        {
            freeSql.Ado.ExecuteNonQuery(sql);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Skipped ConfigCenter legacy Environment migration SQL: {Sql}", sql);
        }
    }
}
