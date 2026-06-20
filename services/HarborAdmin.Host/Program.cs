// HarborAdmin.Host 入口：管理后台 HTTP API（配置中心草稿 CRUD、发布等）。

using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.BuildingBlocks.Caching;
using HarborAdmin.BuildingBlocks.Caching.Options;
using HarborAdmin.BuildingBlocks.Data.Extends;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.BuildingBlocks.Secrets.DependencyInjection;
using HarborAdmin.Client.AI;
using HarborAdmin.Client.ConfigCenter;
using HarborAdmin.Host.Infrastructure;
using HarborAdmin.Host.Filter;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Clients;
using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Modules.Secrets;

var builder = WebApplication.CreateBuilder(args);

var configCenterSection = builder.Configuration.GetSection(ConfigCenterOptions.DefaultSectionName);
var configCenterSource = await builder.Configuration.AddHarborConfigCenterAsync(configCenterSection);
EnsureConfigCenterStartupConfiguration(builder.Configuration, configCenterSource);
// 显式追加 Secrets 模块，确保 ProviderService 等依赖 ISecretStore 的服务可解析。
var moduleAssemblies = HarborModuleAssemblyDiscovery.Discover([typeof(SecretsStartUp).Assembly]);

var mvcBuilder = builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
    options.Filters.Add<ApiValidationFilter>();
    options.Filters.Add<FieldPermissionResultFilter>();
});

mvcBuilder.ConfigureApiBehaviorOptions(options => { options.SuppressModelStateInvalidFilter = true; });
mvcBuilder.AddHarborModuleApplicationParts(moduleAssemblies);
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:18200", "http://127.0.0.1:18200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
builder.Services.AddHarborCaching(builder.Configuration.GetSection(HarborCacheOptions.SectionName));
builder.Services.AddHarborMapping(moduleAssemblies.ToArray());

builder.Services.AddAdminSecurity();
builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName), options =>
{
    options.SnowflakeWorkerId = GetYitterWorkId(builder.Configuration);
    foreach (var moduleAssembly in moduleAssemblies)
    {
        options.AddModuleAssembly(moduleAssembly);
    }

    options.AddCurdAfterHandler(CacheInvalidationAopBridge.Dispatch);
});
builder.Services.AddHarborSecrets();
builder.Services
    .AddHarborCap(builder.Configuration, cap => { cap.DefaultGroupName = "harbor.admin.host"; })
    .AddHarborCapSubscribers(typeof(InternationalTranslationSubscriber).Assembly);

builder.Services.AddSingleton<IConfigCenterNotifyClient, TcpConfigCenterNotifyClient>();
builder.Services.AddHarborConfigCenter(configCenterSource, configCenterSection);
builder.Services.AddAiClient(builder.Configuration);
builder.Services.AddHarborModules(moduleAssemblies, builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseRouting();
app.UseAdminAuthentication();
app.UseAuthorization();
app.UseAdminApiAuthorization();
app.MapControllers();

app.Run();

static ushort GetYitterWorkId(IConfiguration configuration) =>
    configuration.GetValue<ushort?>("Harbor:YitterWorkId") ?? 1;

static void EnsureConfigCenterStartupConfiguration(IConfiguration configuration, ConfigCenterConfigurationSource configCenterSource)
{
    if (!configCenterSource.Options.Required)
    {
        return;
    }

    if (configCenterSource.Provider.Version <= 0)
    {
        throw new InvalidOperationException(
            $"Harbor Host requires a published ConfigCenter configuration for AppId '{configCenterSource.Options.AppId}'. " +
            "Start HarborAdmin.ConfigCenter and publish the application configuration before starting Host.");
    }

    if (!configuration.GetSection($"{DbConfig.SectionName}:Databases").GetChildren().Any())
    {
        throw new InvalidOperationException(
            $"Harbor Host loaded ConfigCenter version {configCenterSource.Provider.Version}, but '{DbConfig.SectionName}:Databases' is empty.");
    }
}
