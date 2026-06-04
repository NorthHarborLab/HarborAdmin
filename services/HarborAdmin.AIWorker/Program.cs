using System.Text.Encodings.Web;
using HarborAdmin.AIWorker.Application;
using HarborAdmin.AIWorker.Infrastructure;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.BuildingBlocks.Secrets.DependencyInjection;
using HarborAdmin.BuildingBlocks.Secrets.Domain;
using HarborAdmin.Client.ConfigCenter;
using HarborAdmin.Modules.AI;

var builder = WebApplication.CreateBuilder(args);

var configCenterSection = builder.Configuration.GetSection(ConfigCenterOptions.DefaultSectionName);
var configCenterSource = await builder.Configuration.AddHarborConfigCenterAsync(configCenterSection);

builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName), options =>
{
    options.SnowflakeWorkerId = GetYitterWorkId(builder.Configuration);
    options.AddEntityAssembly(typeof(HarborSecret).Assembly);
});
builder.Services.AddHarborSecrets();

builder.Services
    .AddHarborCap(builder.Configuration, cap =>
    {
        cap.DefaultGroupName = "harbor.ai.worker";
    })
    .AddHarborCapSubscribers(typeof(AiConfigPublishedSubscriber).Assembly);

builder.Services.AddHarborMapping(typeof(AiModuleExtensions).Assembly, typeof(Program).Assembly);
builder.Services.AddAiModule(builder.Configuration);
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
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<AiRuntimeConfigCache>().ReloadLatestAsync();
}

app.MapControllers();

app.Run();

static ushort GetYitterWorkId(IConfiguration configuration) =>
    configuration.GetValue<ushort?>("Harbor:YitterWorkId") ?? 2;
