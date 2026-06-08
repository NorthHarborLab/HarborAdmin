using System.Text.Json;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.AI.Infrastructure.Contexts;

namespace HarborAdmin.Modules.AI.Application.Services.Shared;

/// <summary>
/// AI 模块共享服务上下文。
/// </summary>
public sealed class AiServiceContext(IAiDbContext dbContext, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
{
    /// <summary>
    /// AI 模块 ORM 上下文。
    /// </summary>
    public IAiDbContext DbContext { get; } = dbContext;

    /// <summary>
    /// 实体库注册表。
    /// </summary>
    public DbEntityRegistry EntityRegistry { get; } = entityRegistry;

    /// <summary>
    /// 工作单元管理器。
    /// </summary>
    public UnitOfWorkManagerCloud UnitOfWorkManager { get; } = unitOfWorkManager;

    /// <summary>
    /// 快照序列化选项。
    /// </summary>
    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };
}