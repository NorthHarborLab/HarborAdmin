using HarborAdmin.BuildingBlocks.Data;

namespace HarborAdmin.Modules.TaskOrchestration.Infrastructure.Contexts;

/// <summary>
/// 任务编排模块数据库上下文实现
/// </summary>
public sealed class TaskOrchestrationDbContext(HarborFreeSqlCloud cloud, DbModuleRegistry moduleRegistry)
    : HarborModuleDbContext<TaskOrchestrationStartUp>(cloud, moduleRegistry), ITaskOrchestrationDbContext
{
}
