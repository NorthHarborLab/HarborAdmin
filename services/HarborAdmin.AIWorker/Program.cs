using System.Text.Encodings.Web;
using HarborAdmin.AIWorker.Application;
using HarborAdmin.AIWorker.Infrastructure;
using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.BuildingBlocks.Secrets.DependencyInjection;
using HarborAdmin.Client.ConfigCenter;
using HarborAdmin.Modules.AI;
using HarborAdmin.Modules.Secrets;

var builder = WebApplication.CreateBuilder(args);

var configCenterSection = builder.Configuration.GetSection(ConfigCenterOptions.DefaultSectionName);
// Worker 自身配置优先从 ConfigCenter 拉取，保持供应商执行进程也能热更新基础配置。
var configCenterSource = await builder.Configuration.AddHarborConfigCenterAsync(configCenterSection);
// AIWorker 必须显式加载 AI 与 Secrets 模块，以便注册仓储与 ISecretResolver。
var moduleAssemblies = HarborModuleAssemblyDiscovery.Discover([
    typeof(AiStartUp).Assembly,
    typeof(SecretsStartUp).Assembly,
]);

builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName), options =>
{
    options.SnowflakeWorkerId = GetYitterWorkId(builder.Configuration);
    foreach (var moduleAssembly in moduleAssemblies)
    {
        options.AddModuleAssembly(moduleAssembly);
    }
});
builder.Services.AddHarborSecrets();

builder.Services
    .AddHarborCap(builder.Configuration, cap => { cap.DefaultGroupName = "harbor.ai.worker"; })
    .AddHarborCapSubscribers(typeof(AiConfigPublishedSubscriber).Assembly);

builder.Services.AddHarborMapping(moduleAssemblies.Append(typeof(Program).Assembly).ToArray());
builder.Services.AddHarborModules(moduleAssemblies, builder.Configuration, HarborHostKinds.AIWorker);
builder.Services.AddSingleton<AiRuntimeConfigCache>();
builder.Services.AddScoped<AiRequestSignatureValidator>();
builder.Services.AddScoped<AiPromptComposer>();
builder.Services.AddScoped<IAiQuotaService, AiQuotaService>();
builder.Services.AddScoped<AiExecutionService>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<IAiProviderAdapter>(sp =>
    new OpenAiChatCompletionsAdapter(sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OpenAiChatCompletionsAdapter))));
builder.Services.AddSingleton<IAiProviderAdapter>(sp =>
    new GoogleGeminiGenerateContentAdapter(sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(GoogleGeminiGenerateContentAdapter))));
builder.Services.AddSingleton<IAiProviderAdapter>(sp =>
    new OpenAiResponsesAdapter(sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OpenAiResponsesAdapter))));
builder.Services.AddSingleton<AiProviderAdapterResolver>();
builder.Services.AddHarborConfigCenter(configCenterSource, configCenterSection);
builder.Services
    .AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping; });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    // 启动后先加载最新 AI 发布快照，避免首个请求承担冷启动读取成本。
    await scope.ServiceProvider.GetRequiredService<AiRuntimeConfigCache>().ReloadLatestAsync();
}

app.MapControllers();

app.Run();

// AIWorker 与 Host 使用不同默认 WorkId，避免同库雪花 ID 冲突。
static ushort GetYitterWorkId(IConfiguration configuration) =>
    configuration.GetValue<ushort?>("Harbor:YitterWorkId") ?? 2;
