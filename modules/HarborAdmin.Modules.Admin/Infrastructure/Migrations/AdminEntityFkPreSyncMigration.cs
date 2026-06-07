using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Admin.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace HarborAdmin.Modules.Admin.Infrastructure.Migrations;

/// <summary>
/// 在 CodeFirst 同步前，将角色权限/字段权限的字符串弱关联回填为 FK，并移除旧唯一索引。
/// </summary>
public sealed class AdminEntityFkPreSyncMigration(ILogger<AdminEntityFkPreSyncMigration> logger) : IHarborFreeSqlPreSyncHook
{
    private static readonly Type[] AdminEntityTypes =
    [
        typeof(AdminMenu),
        typeof(AdminRolePermission),
        typeof(AdminRoleFieldPermission),
    ];

    /// <inheritdoc />
    public void BeforeSyncStructure(IFreeSql freeSql, string dbKey, IReadOnlyList<Type> entityTypes)
    {
        if (!entityTypes.Any(type => AdminEntityTypes.Contains(type)))
        {
            return;
        }

        MigrateMenuFeatureIds(freeSql);
        MigrateRolePermissions(freeSql);
        MigrateRoleFieldPermissions(freeSql);
    }

    private void MigrateMenuFeatureIds(IFreeSql freeSql)
    {
        ExecuteIgnoreMissing(freeSql,
            """
            ALTER TABLE "AdminMenu"
            ADD COLUMN IF NOT EXISTS "AdminFeatureId" bigint NULL
            """);
        ExecuteIgnoreMissing(freeSql,
            """
            UPDATE "AdminMenu" AS menu
            SET "AdminFeatureId" = feature."Id"
            FROM "AdminFeature" AS feature
            WHERE menu."AdminFeatureId" IS NULL
              AND menu."FeatureCode" IS NOT NULL
              AND menu."FeatureCode" <> ''
              AND lower(menu."FeatureCode") = lower(feature."FeatureCode")
            """);
    }

    private void MigrateRolePermissions(IFreeSql freeSql)
    {
        ExecuteIgnoreMissing(freeSql, "DROP INDEX IF EXISTS ux_admin_role_permission");
        ExecuteIgnoreMissing(freeSql,
            """
            ALTER TABLE "AdminRolePermission"
            ADD COLUMN IF NOT EXISTS "AdminFeatureActionId" bigint NOT NULL DEFAULT 0
            """);
        ExecuteIgnoreMissing(freeSql,
            """
            UPDATE "AdminRolePermission" AS permission
            SET "AdminFeatureActionId" = action."Id"
            FROM "AdminFeatureAction" AS action
            WHERE permission."AdminFeatureActionId" = 0
              AND permission."PermissionCode" IS NOT NULL
              AND permission."PermissionCode" <> ''
              AND lower(permission."PermissionCode") = lower(action."PermissionCode")
            """);
        ExecuteIgnoreMissing(freeSql,
            """
            DELETE FROM "AdminRolePermission"
            WHERE "AdminFeatureActionId" = 0
            """);
        ExecuteIgnoreMissing(freeSql,
            """
            DELETE FROM "AdminRolePermission" AS current
            USING "AdminRolePermission" AS duplicate
            WHERE current."Id" > duplicate."Id"
              AND current."RoleId" = duplicate."RoleId"
              AND current."AdminFeatureActionId" = duplicate."AdminFeatureActionId"
              AND current."AdminFeatureActionId" <> 0
            """);
    }

    private void MigrateRoleFieldPermissions(IFreeSql freeSql)
    {
        ExecuteIgnoreMissing(freeSql, "DROP INDEX IF EXISTS ux_admin_role_field_permission");
        ExecuteIgnoreMissing(freeSql,
            """
            ALTER TABLE "AdminRoleFieldPermission"
            ADD COLUMN IF NOT EXISTS "AdminFeatureFieldId" bigint NOT NULL DEFAULT 0
            """);
        ExecuteIgnoreMissing(freeSql,
            """
            UPDATE "AdminRoleFieldPermission" AS policy
            SET "AdminFeatureFieldId" = field."Id"
            FROM "AdminFeatureField" AS field
            WHERE policy."AdminFeatureFieldId" = 0
              AND policy."FeatureCode" IS NOT NULL
              AND policy."FeatureCode" <> ''
              AND lower(policy."FeatureCode") = lower(field."FeatureCode")
              AND lower(policy."FieldName") = lower(field."FieldCode")
            """);
        ExecuteIgnoreMissing(freeSql,
            """
            DELETE FROM "AdminRoleFieldPermission"
            WHERE "AdminFeatureFieldId" = 0
            """);
        ExecuteIgnoreMissing(freeSql,
            """
            DELETE FROM "AdminRoleFieldPermission" AS current
            USING "AdminRoleFieldPermission" AS duplicate
            WHERE current."Id" > duplicate."Id"
              AND current."RoleId" = duplicate."RoleId"
              AND current."AdminFeatureFieldId" = duplicate."AdminFeatureFieldId"
              AND current."AdminFeatureFieldId" <> 0
            """);
    }

    private void ExecuteIgnoreMissing(IFreeSql freeSql, string sql)
    {
        try
        {
            freeSql.Ado.ExecuteNonQuery(sql);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Skipped Admin FK pre-sync migration SQL: {Sql}", sql);
        }
    }
}
