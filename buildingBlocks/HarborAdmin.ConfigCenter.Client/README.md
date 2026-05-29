# HarborAdmin.ConfigCenter.Client

HarborLab 配置中心客户端 NuGet 包：通过 TCP JSON 拉取配置并接入 `IConfiguration` 热更新。

## 接入

`appsettings.json`：

```json
{
  "Harbor": {
    "ConfigCenter": {
      "Host": "127.0.0.1",
      "Port": 9500,
      "AppId": "my-service",
      "Environment": "Development"
    }
  }
}
```

`Program.cs`：

```csharp
var configSection = builder.Configuration.GetSection("Harbor:ConfigCenter");
var configSource = builder.Configuration.AddHarborConfigCenter(configSection);
builder.Services.AddHarborConfigCenter(configSection);
builder.Services.AddSingleton(configSource);
```

业务代码照常使用 `IConfiguration` / `IOptionsMonitor<T>`。
