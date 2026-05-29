// HarborAdmin.ConfigCenter 服务入口：注册 ConfigCenter 模块并启动 TCP JSON 监听（默认端口 9500）。

using HarborAdmin.ConfigCenter.Tcp;
using HarborAdmin.Modules.ConfigCenter;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddConfigCenterModule(builder.Configuration, registerNotifyClient: false);
builder.Services.AddSingleton<ConfigSubscriptionHub>();
builder.Services.AddHostedService<ConfigCenterTcpHostedService>();

var host = builder.Build();
host.Run();
