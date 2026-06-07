using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Application.Services.Shared;

/// <summary>
/// Admin 模块共享服务上下文。
/// </summary>
public sealed class AdminServiceContext(IAdminDbContext db)
{
    private const string SessionVersionKey = "global";

    /// <summary>
    /// Admin 模块 ORM 实例。
    /// </summary>
    public IFreeSql Orm => db.Orm;

    /// <summary>
    /// 读取全局会话版本号，不存在时初始化为 1。
    /// </summary>
    public async Task<long> GetSessionVersionValueAsync(CancellationToken cancellationToken)
    {
        var version = await Orm.Select<AdminSessionVersion>()
            .Where(item => item.VersionKey == SessionVersionKey)
            .ToOneAsync(cancellationToken);
        if (version is not null)
        {
            return version.Version;
        }

        await Orm.Insert(new AdminSessionVersion
        {
            VersionKey = SessionVersionKey,
            Version = 1,
            UpdatedAt = DateTimeOffset.UtcNow,
        }).ExecuteAffrowsAsync(cancellationToken);
        return 1;
    }

    /// <summary>
    /// 递增全局会话版本号，通知前端刷新权限与菜单。
    /// </summary>
    public async Task BumpSessionVersionAsync(CancellationToken cancellationToken)
    {
        var version = await Orm.Select<AdminSessionVersion>()
            .Where(item => item.VersionKey == SessionVersionKey)
            .ToOneAsync(cancellationToken);
        if (version is null)
        {
            await Orm.Insert(new AdminSessionVersion
            {
                VersionKey = SessionVersionKey,
                Version = 2,
                UpdatedAt = DateTimeOffset.UtcNow,
            }).ExecuteAffrowsAsync(cancellationToken);
            return;
        }

        version.Version++;
        version.UpdatedAt = DateTimeOffset.UtcNow;
        await Orm.Update<AdminSessionVersion>().SetSource(version).ExecuteAffrowsAsync(cancellationToken);
    }
}
