// HarborAdmin.ConfigCenter 服务入口：FreeSql 只读查询 + TCP JSON 监听（默认端口 50000）。

using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.ConfigCenter.Tcp;
using HarborAdmin.Modules.ConfigCenter;
using System.Text.RegularExpressions;

var builder = Host.CreateApplicationBuilder(args);

ResolveEnvironmentPlaceholders(builder.Configuration);

builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName), options =>
{
    options.SnowflakeWorkerId = GetYitterWorkId(builder.Configuration);
});

builder.Services.AddConfigCenterModule(builder.Configuration);
builder.Services.AddSingleton<ConfigSubscriptionHub>();
builder.Services.AddHostedService<ConfigCenterTcpHostedService>();

var host = builder.Build();
host.Run();

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

static ushort GetYitterWorkId(IConfiguration configuration) =>
    configuration.GetValue<ushort?>("Harbor:YitterWorkId") ?? 1;

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
