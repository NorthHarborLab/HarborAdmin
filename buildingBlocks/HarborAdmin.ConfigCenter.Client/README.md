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

## Options 整块配置

配置中心支持把一个 JSON 配置项作为完整 options model 发布。创建配置项时：

```text
Group:      (留空)
Key:        JwtOptions
ValueType:  options
Value:      {"Issuer":"harbor","ExpireMinutes":120,"Audiences":["admin","api"]}
```

发布后客户端会收到扁平化配置：

```text
JwtOptions:Issuer = harbor
JwtOptions:ExpireMinutes = 120
JwtOptions:Audiences:0 = admin
JwtOptions:Audiences:1 = api
```

业务服务可直接绑定 model：

```csharp
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("JwtOptions"));

var options = builder.Configuration
    .GetSection("JwtOptions")
    .Get<JwtOptions>();
```

如果需要挂在某个分组下，也可以设置：

```text
Group:      Security
Key:        JwtOptions
ValueType:  options
```

对应读取：

```csharp
builder.Services.Configure<JwtOptions>(
    builder.Configuration.GetSection("Security:JwtOptions"));
```

`ValueType` 为 `json`、`object`、`options`、`model` 时都会按 JSON 对象/数组展开；其他类型保持普通字符串键值。
