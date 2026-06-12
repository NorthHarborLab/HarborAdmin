using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Client.ConfigCenter;
using HarborAdmin.Modules.TaskOrchestration;
using HarborAdmin.Modules.TaskOrchestration.Application.Abstractions;
using HarborAdmin.Modules.TaskOrchestration.Application.Execution;
using HarborAdmin.Modules.TaskOrchestration.Contracts.Tasks.Dto;
using HarborAdmin.TaskWorker.Callables;
using HarborAdmin.TaskWorker.Scheduling;
using HarborAdmin.TaskWorker.Subscriptions;
using Quartz;

var builder = WebApplication.CreateBuilder(args);

var configCenterSection = builder.Configuration.GetSection(ConfigCenterOptions.DefaultSectionName);
var configCenterSource = await builder.Configuration.AddHarborConfigCenterAsync(configCenterSection);
var moduleAssemblies = HarborModuleAssemblyDiscovery.Discover([typeof(TaskOrchestrationStartUp).Assembly]);

builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName), options =>
{
    options.SnowflakeWorkerId = TaskWorkerStartupConfiguration.GetYitterWorkId(builder.Configuration);
    foreach (var moduleAssembly in moduleAssemblies)
    {
        options.AddModuleAssembly(moduleAssembly);
    }
});

builder.Services
    .AddHarborCap(builder.Configuration, cap => { cap.DefaultGroupName = "harbor.task.worker"; })
    .AddHarborCapSubscribers(typeof(TaskRunRequestSubscriber).Assembly);

builder.Services.AddHarborMapping(moduleAssemblies.Append(typeof(Program).Assembly).ToArray());
builder.Services.AddHarborModules(moduleAssemblies, builder.Configuration, HarborHostKinds.TaskWorker);
builder.Services.AddOptions<CallablePluginOptions>().BindConfiguration(CallablePluginOptions.SectionName);
builder.Services.AddHttpClient(TaskHttpStepExecutor.HttpClientName);
builder.Services.AddSingleton<ITaskCallableRegistry, DynamicTaskCallableRegistry>();
builder.Services.AddHostedService<TaskQuartzScheduleHostedService>();
builder.Services.AddQuartz(options =>
{
    var jobKey = TaskQuartzJob.JobKey;
    options.AddJob<TaskQuartzJob>(job => job.WithIdentity(jobKey).StoreDurably());
});
builder.Services.AddQuartzHostedService(options => { options.WaitForJobsToComplete = true; });
builder.Services.AddHarborConfigCenter(configCenterSource, configCenterSection);
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapHealthChecks("/health");
app.MapGet("/api/admin/task-orchestration/callables", (ITaskCallableRegistry registry, IHarborMapper mapper) =>
    ApiResult.Ok<IReadOnlyList<TaskCallableDescriptorDto>>(registry.List().Select(mapper.Map<TaskCallableDescriptorDto>).ToArray()));

app.Run();

/// <summary>
/// TaskWorker 启动配置辅助方法
/// </summary>
internal static class TaskWorkerStartupConfiguration
{
    /// <summary>
    /// 获取 TaskWorker 使用的雪花算法 WorkId
    /// </summary>
    /// <param name="configuration">应用配置</param>
    /// <returns>TaskWorker 雪花算法 WorkId</returns>
    public static ushort GetYitterWorkId(IConfiguration configuration) =>
        configuration.GetValue<ushort?>("Harbor:YitterWorkId") ?? 3;
}
