// HarborAdmin.Host 入口：管理后台 HTTP API（配置中心草稿 CRUD、发布等）。

using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.EventBus;
using HarborAdmin.ConfigCenter.Client;
using HarborAdmin.Host.Infrastructure;
using HarborAdmin.Modules.ConfigCenter;
using HarborAdmin.Modules.ConfigCenter.Contracts;
using HarborAdmin.Modules.ConfigCenter.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var configCenterSection = builder.Configuration.GetSection(ConfigCenterOptions.DefaultSectionName);
var configCenterSource = await builder.Configuration.AddHarborConfigCenterAsync(configCenterSection);

var mvcBuilder = builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilter>();
    options.Filters.Add<ApiResultFilter>();
});
mvcBuilder.AddHarborModuleApplicationParts();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:5667", "http://127.0.0.1:5667")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName));

builder.Services.AddHarborCap(builder.Configuration, cap =>
{
    cap.DefaultGroupName = "harbor.admin.host";
});

builder.Services.AddSingleton<IConfigCenterNotifyClient, TcpConfigCenterNotifyClient>();
builder.Services.AddHarborConfigCenter(configCenterSource, configCenterSection);
builder.Services.AddConfigCenterModule(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
