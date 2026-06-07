using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HarborAdmin.Modules.Admin.Infrastructure.Seed;

/// <summary>
/// 一次性迁移：将 Menu/Role 权限相关字符串弱关联回填为强 FK。
/// </summary>
public sealed class AdminEntityFkMigrationHostedService(
    IAdminDbContext db,
    ILogger<AdminEntityFkMigrationHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await MigrateMenuFeatureIdsAsync(cancellationToken);
        await MigrateRolePermissionActionIdsAsync(cancellationToken);
        await MigrateRoleFieldPermissionFieldIdsAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task MigrateMenuFeatureIdsAsync(CancellationToken cancellationToken)
    {
        var features = await db.Orm.Select<AdminFeature>().ToListAsync(cancellationToken);
        var featureMap = features.ToDictionary(feature => feature.FeatureCode, StringComparer.OrdinalIgnoreCase);
        var menus = await db.Orm.Select<AdminMenu>()
            .Where(menu => menu.AdminFeatureId == null && menu.FeatureCode != null && menu.FeatureCode != string.Empty)
            .ToListAsync(cancellationToken);
        var changed = 0;
        foreach (var menu in menus)
        {
            if (!featureMap.TryGetValue(menu.FeatureCode!, out var feature))
            {
                logger.LogWarning("菜单 {MenuId} 的 FeatureCode '{FeatureCode}' 无法匹配功能，跳过 AdminFeatureId 回填。", menu.Id, menu.FeatureCode);
                continue;
            }

            menu.AdminFeatureId = feature.Id;
            await db.Orm.Update<AdminMenu>().SetSource(menu).ExecuteAffrowsAsync(cancellationToken);
            changed++;
        }

        if (changed > 0)
        {
            logger.LogInformation("已回填 {Count} 条 AdminMenu.AdminFeatureId。", changed);
        }
    }

    private async Task MigrateRolePermissionActionIdsAsync(CancellationToken cancellationToken)
    {
        var actions = await db.Orm.Select<AdminFeatureAction>().ToListAsync(cancellationToken);
        var actionMap = actions.ToDictionary(action => action.PermissionCode, StringComparer.OrdinalIgnoreCase);
        var permissions = await db.Orm.Select<AdminRolePermission>()
            .Where(link => link.AdminFeatureActionId == 0 && link.PermissionCode != string.Empty)
            .ToListAsync(cancellationToken);
        var changed = 0;
        foreach (var permission in permissions)
        {
            if (!actionMap.TryGetValue(permission.PermissionCode, out var action))
            {
                logger.LogWarning("角色权限 {PermissionId} 的 PermissionCode '{PermissionCode}' 无法匹配动作，跳过 AdminFeatureActionId 回填。", permission.Id, permission.PermissionCode);
                continue;
            }

            permission.AdminFeatureActionId = action.Id;
            await db.Orm.Update<AdminRolePermission>().SetSource(permission).ExecuteAffrowsAsync(cancellationToken);
            changed++;
        }

        if (changed > 0)
        {
            logger.LogInformation("已回填 {Count} 条 AdminRolePermission.AdminFeatureActionId。", changed);
        }
    }

    private async Task MigrateRoleFieldPermissionFieldIdsAsync(CancellationToken cancellationToken)
    {
        var fields = await db.Orm.Select<AdminFeatureField>().ToListAsync(cancellationToken);
        var fieldMap = fields.ToDictionary(
            field => $"{field.FeatureCode}\u001F{field.FieldCode}",
            StringComparer.OrdinalIgnoreCase);
        var policies = await db.Orm.Select<AdminRoleFieldPermission>()
            .Where(policy => policy.AdminFeatureFieldId == 0)
            .ToListAsync(cancellationToken);
        var changed = 0;
        foreach (var policy in policies)
        {
            var key = $"{policy.FeatureCode}\u001F{policy.FieldName}";
            if (!fieldMap.TryGetValue(key, out var field))
            {
                logger.LogWarning(
                    "角色字段权限 {PolicyId} 的 FeatureCode/FieldName '{FeatureCode}/{FieldName}' 无法匹配字段，跳过 AdminFeatureFieldId 回填。",
                    policy.Id,
                    policy.FeatureCode,
                    policy.FieldName);
                continue;
            }

            policy.AdminFeatureFieldId = field.Id;
            await db.Orm.Update<AdminRoleFieldPermission>().SetSource(policy).ExecuteAffrowsAsync(cancellationToken);
            changed++;
        }

        if (changed > 0)
        {
            logger.LogInformation("已回填 {Count} 条 AdminRoleFieldPermission.AdminFeatureFieldId。", changed);
        }
    }
}
