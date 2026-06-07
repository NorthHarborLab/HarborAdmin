using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace HarborAdmin.Modules.Admin.Infrastructure.Seed;

/// <summary>
/// 一次性迁移 AdminFeatureApi.Path 至统一 /api/admin/* 前缀。
/// </summary>
public sealed class AdminApiPathMigrationHostedService(
    IAdminDbContext db,
    ILogger<AdminApiPathMigrationHostedService> logger) : IHostedService
{
    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var apis = await db.Orm.Select<AdminFeatureApi>().ToListAsync(cancellationToken);
        var changed = 0;
        foreach (var api in apis)
        {
            var migrated = MigratePath(api.Path);
            if (string.Equals(migrated, api.Path, StringComparison.Ordinal))
            {
                continue;
            }

            api.Path = migrated;
            await db.Orm.Update<AdminFeatureApi>().SetSource(api).ExecuteAffrowsAsync(cancellationToken);
            changed++;
        }

        if (changed > 0)
        {
            logger.LogInformation("已迁移 {Count} 条 AdminFeatureApi 路径至 /api/admin/* 前缀。", changed);
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    internal static string MigratePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        var normalized = path.Trim();
        if (normalized.StartsWith("/system/", StringComparison.OrdinalIgnoreCase))
        {
            return $"/api/admin{normalized}";
        }

        if (normalized.StartsWith("system/", StringComparison.OrdinalIgnoreCase))
        {
            return $"/api/admin/{normalized}";
        }

        if (normalized.StartsWith("/api/user", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Replace("/api/user", "/api/admin/access", StringComparison.OrdinalIgnoreCase);
        }

        if (normalized.StartsWith("/menu/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.Replace("/menu/", "/api/admin/access/", StringComparison.OrdinalIgnoreCase);
        }

        return normalized;
    }
}
