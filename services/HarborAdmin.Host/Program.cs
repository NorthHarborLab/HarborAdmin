// HarborAdmin.Host 入口：管理后台 HTTP API（配置中心草稿 CRUD、发布等）。

using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.BuildingBlocks.Caching;
using HarborAdmin.BuildingBlocks.Caching.Options;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.BuildingBlocks.Secrets.DependencyInjection;
using HarborAdmin.BuildingBlocks.Secrets.Domain;
using HarborAdmin.Client.AI;
using HarborAdmin.Client.ConfigCenter;
using HarborAdmin.Host.Infrastructure;
using HarborAdmin.Modules.ConfigCenter;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Infrastructure.Clients;
using HarborAdmin.Modules.AI;
using HarborAdmin.Modules.Admin;
using HarborAdmin.Modules.International;
using HarborAdmin.Modules.International.Application.Services;
using HarborAdmin.Modules.Secrets;

var builder = WebApplication.CreateBuilder(args);

var configCenterSection = builder.Configuration.GetSection(ConfigCenterOptions.DefaultSectionName);
var configCenterSource = await builder.Configuration.AddHarborConfigCenterAsync(configCenterSection);
var moduleAssemblies = ModuleApplicationPartExtensions.DiscoverHarborModuleAssemblies();

var mvcBuilder = builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
    options.Filters.Add<ApiValidationFilter>();
});

mvcBuilder.ConfigureApiBehaviorOptions(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
mvcBuilder.AddHarborModuleApplicationParts(moduleAssemblies);
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5667", "http://127.0.0.1:5667")
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
    options.AddEntityAssembly(typeof(HarborSecret).Assembly);
    options.AddCurdAfterHandler(CacheInvalidationAopBridge.Dispatch);
});
builder.Services.AddHarborSecrets();
builder.Services
    .AddHarborCap(builder.Configuration, cap =>
    {
        cap.DefaultGroupName = "harbor.admin.host";
    })
    .AddHarborCapSubscribers(typeof(InternationalTranslationSubscriber).Assembly);

builder.Services.AddSingleton<IConfigCenterNotifyClient, TcpConfigCenterNotifyClient>();
builder.Services.AddHarborConfigCenter(configCenterSource, configCenterSection);
builder.Services.AddAiClient(builder.Configuration);
builder.Services.AddAiModule(builder.Configuration);
builder.Services.AddAdminModule();
builder.Services.AddInternationalModule();
builder.Services.AddConfigCenterModule(builder.Configuration);
builder.Services.AddSecretsModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAdminAuthentication();
app.UseAuthorization();
app.UseAdminApiAuthorization();
app.MapControllers();

app.Run();

static ushort GetYitterWorkId(IConfiguration configuration) =>
    configuration.GetValue<ushort?>("Harbor:YitterWorkId") ?? 1;
