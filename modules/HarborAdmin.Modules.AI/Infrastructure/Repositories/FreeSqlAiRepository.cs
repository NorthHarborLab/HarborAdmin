using HarborAdmin.Modules.AI.Application.Abstractions;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Infrastructure.Repositories;

/// <summary>
/// 基于 FreeSql 的 AI 仓储。
/// </summary>
public sealed partial class FreeSqlAiRepository : IAiRepository
{
    private readonly IAiDbContext db;

    /// <summary>
    /// 初始化 AI 仓储。
    /// </summary>
    public FreeSqlAiRepository(IAiDbContext db)
    {
        this.db = db;
    }

    private IFreeSql FreeSql => db.Orm;
}
