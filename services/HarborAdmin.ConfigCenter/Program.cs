// HarborAdmin.ConfigCenter 服务入口：FreeSql 快照读取 + TCP JSON 监听（默认端口 18100）。

using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.BuildingBlocks.Secrets.DependencyInjection;
using HarborAdmin.ConfigCenter.Tcp;
using HarborAdmin.Modules.ConfigCenter;
using HarborAdmin.Modules.Secrets;
using System.Text.RegularExpressions;
using HarborAdmin.BuildingBlocks.Data.Extends;

var builder = Host.CreateApplicationBuilder(args);

ResolveEnvironmentPlaceholders(builder.Configuration);
// ConfigCenter 进程必须显式加载 ConfigCenter 与 Secrets 模块，以便注册仓储与 ISecretStore。
var moduleAssemblies = HarborModuleAssemblyDiscovery.Discover([
    typeof(ConfigCenterStartUp).Assembly,
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
builder.Services.AddHarborMapping(moduleAssemblies.ToArray());
builder.Services.AddHarborModules(moduleAssemblies, builder.Configuration, HarborHostKinds.ConfigCenter);
builder.Services.AddMemoryCache();
builder.Services.AddScoped<PublishedConfigCache>();
builder.Services.AddSingleton<ConfigSubscriptionHub>();
builder.Services.AddHostedService<ConfigCenterTcpHostedService>();

var host = builder.Build();
host.Run();

// 启动早期解析数据库连接字符串中的环境变量占位符，避免 FreeSql 初始化后才暴露缺失变量。
static void ResolveEnvironmentPlaceholders(ConfigurationManager configuration)
{
    foreach (var database in configuration.GetSection($"{DbConfig.SectionName}:Databases").GetChildren())
    {
        ResolveValue(configuration, $"{database.Path}:ConnectionString");
        foreach (var slave in database.GetSection("SlaveList").GetChildren())
        {
            ResolveValue(configuration, $"{slave.Path}:ConnectionString");
        }
    }
}

// ConfigCenter 也会参与读取 Secret 实体和快照实体，保持雪花 ID 生成器与 Host 使用同一配置入口。
static ushort GetYitterWorkId(IConfiguration configuration) =>
    configuration.GetValue<ushort?>("Harbor:YitterWorkId") ?? 1;

// 支持形如 ${ENV:HARBORADMIN_DEV_POSTGRES_PASSWORD} 的占位符；缺失变量直接失败，避免静默连错库。
static void ResolveValue(ConfigurationManager configuration, string key)
{
    var value = configuration[key];
    if (string.IsNullOrWhiteSpace(value))
    {
        return;
    }

    configuration[key] = new Regex(@"\$\{ENV:(?<name>[A-Za-z_][A-Za-z0-9_]*)\}").Replace(value, match =>
    {
        var variableName = match.Groups["name"].Value;
        var variableValue = Environment.GetEnvironmentVariable(variableName);
        if (string.IsNullOrEmpty(variableValue))
        {
            throw new InvalidOperationException($"Environment variable '{variableName}' is required by configuration '{key}'.");
        }

        return variableValue;
    });
}
