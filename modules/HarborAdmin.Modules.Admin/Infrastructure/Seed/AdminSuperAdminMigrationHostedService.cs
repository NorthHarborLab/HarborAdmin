using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HarborAdmin.Modules.Admin.Infrastructure.Seed;

/// <summary>
/// 一次性迁移：将绑定 super_admin 角色的用户标记为超级管理员。
/// </summary>
public sealed class AdminSuperAdminMigrationHostedService(
    IAdminDbContext db,
    ILogger<AdminSuperAdminMigrationHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var superAdminRole = await db.Orm.Select<AdminRole>()
            .Where(role => role.RoleCode == "super_admin")
            .ToOneAsync(cancellationToken);
        if (superAdminRole is null)
        {
            return;
        }

        var userIds = await db.Orm.Select<AdminUserRole>()
            .Where(link => link.RoleId == superAdminRole.Id)
            .ToListAsync(link => link.UserId, cancellationToken);
        if (userIds.Count == 0)
        {
            return;
        }

        var changed = await db.Orm.Update<AdminUser>()
            .Set(user => user.IsSuperAdmin, true)
            .Where(user => userIds.Contains(user.Id) && !user.IsSuperAdmin)
            .ExecuteAffrowsAsync(cancellationToken);
        if (changed > 0)
        {
            logger.LogInformation("已将 {Count} 个 super_admin 角色用户迁移为 IsSuperAdmin=true。", changed);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
