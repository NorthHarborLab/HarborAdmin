using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Application.Execution;
using HarborAdmin.Modules.TaskOrchestration.Application.Services;
using HarborAdmin.Modules.TaskOrchestration.Infrastructure.Contexts;
using HarborAdmin.Modules.TaskOrchestration.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.Modules.TaskOrchestration;

/// <summary>
/// 任务编排模块启动入口
/// </summary>
public sealed class TaskOrchestrationStartUp : HarborModuleMetadataBase, IHarborModuleStartup
{
    /// <inheritdoc />
    public override string ModuleName => "TaskOrchestration";

    /// <inheritdoc />
    public override string GetDbKey() => "AdminDb";

    /// <summary>
    /// 注册任务编排模块
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="context">模块注册上下文</param>
    public void AddModule(IServiceCollection services, HarborModuleRegistrationContext context)
    {
        services.AddSingleton<ITaskOrchestrationDbContext, TaskOrchestrationDbContext>();
        services.AddScoped<ITaskDefinitionRepository, TaskDefinitionRepository>();
        services.AddScoped<ITaskRunRepository, TaskRunRepository>();
        services.AddScoped<ITaskCallableRegistry, TaskCallableRegistry>();
        services.AddScoped<ITaskTemplateRenderer, TaskTemplateRenderer>();
        services.AddScoped<TaskDagValidator>();
        services.AddScoped<TaskConditionEvaluator>();
        services.AddScoped<TaskNodeExecutionService>();
        services.AddScoped<TaskOrchestrationService>();
        services.AddScoped<TaskTriggerDispatcher>();
    }
}
