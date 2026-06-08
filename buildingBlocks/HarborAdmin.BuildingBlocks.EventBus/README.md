# HarborAdmin.BuildingBlocks.EventBus

HarborAdmin 的事件总线基础包，基于 DotNetCore.CAP 提供可靠消息发布、订阅程序集注册、FreeSql 工作单元事务桥接，以及 CAP Request/Reply 请求响应封装。

## 功能范围

- 注册 CAP 基础设施。
- 支持 RabbitMQ 和 InMemory 传输。
- 支持 Sqlite、PostgreSQL、InMemory CAP 存储。
- 提供 `IEventPublisher` 发布抽象。
- 支持 string topic + 任意 payload 发布（当前业务模块主要用法）。
- 支持 `IIntegrationEvent` + `[EventName]` 强类型集成事件发布。
- 支持 CAP 订阅程序集注册。
- 支持 FreeSql `IUnitOfWork` 与 CAP outbox 事务桥接。
- 集成 `DotNetCore.Cap.RequestReply`，提供 `IEventRequestClient` 请求响应抽象。

## 边界约束

本包不负责：

- 定义业务事件类型。
- 实现业务订阅处理器。
- 访问数据库仓储。
- 处理缓存失效。
- 注册 Host、Controller 或业务模块。
- 实现幂等消费持久化表。
- 替代 CAP Dashboard 的失败消息运维能力。


## 项目结构

```text
HarborAdmin.BuildingBlocks.EventBus/
  Configs/                          HarborCapOptions 及 CAP / RequestReply 配置对象
  CapServiceCollectionExtensions.cs AddHarborCap、AddHarborCapSubscribers
  CapEventPublisher.cs              IEventPublisher 实现
  CapEventRequestClient.cs          IEventRequestClient 实现
  EventNameAttribute.cs             强类型集成事件 topic 声明
  IntegrationEventBase.cs           集成事件基类
  IIntegrationEvent.cs              集成事件契约
  IntegrationEventNameResolver.cs   [EventName] 解析
  IEventPublisher.cs
  IEventRequestClient.cs
  UnitOfWorkCapExtensions.cs        FreeSql IUnitOfWork 与 CAP 事务桥接
```

## DI 接入

```csharp
builder.Services
    .AddHarborCap(builder.Configuration, cap =>
    {
        // 可覆盖配置文件中的 DefaultGroup
        cap.DefaultGroupName = "harbor.admin.host";
    })
    .AddHarborCapSubscribers(typeof(InternationalTranslationSubscriber).Assembly);
```

`Harbor:Cap:DefaultGroup` 会映射到 CAP 的 `DefaultGroupName`；`configureCap` 回调中的赋值优先级更高。当前 Host 在 `Program.cs` 中将组名固定为 `harbor.admin.host`。

注册后可注入：

- `IEventPublisher`（始终注册）
- `IEventRequestClient`（仅 `Harbor:Cap:RequestReply:Enabled = true` 时注册）
- `ICapPublisher`（由 CAP `AddCap` 注册）

`AddHarborCap` 还固定启用 `EnablePublishParallelSend`、`UseStorageLock`，并对 JSON 序列化使用 `UnsafeRelaxedJsonEscaping`。

## CAP 配置

配置节名称：`Harbor:Cap`。

```json
{
  "Harbor": {
    "Cap": {
      "Transport": "RabbitMq",
      "DefaultGroup": "harbor-admin",
      "Version": "v1",
      "FailedRetryCount": 5,
      "FailedRetryInterval": 15,
      "UseDashboard": true,
      "Storage": {
        "Type": "Sqlite",
        "ConnectionString": "Data Source=../data/cap.db"
      },
      "RabbitMq": {
        "HostName": "127.0.0.1",
        "Port": 5672,
        "UserName": "guest",
        "Password": "guest",
        "ExchangeName": "harbor.cap.default"
      }
    }
  }
}
```

### Transport 与 Storage

| `Transport` | 行为 |
|-------------|------|
| `RabbitMq`（默认） | 使用 `RabbitMq` 节点配置连接 RabbitMQ；消息持久化由 `Storage` 决定 |
| `InMemory` | 调用 `UseInMemoryStorage()`，**会覆盖** `Storage` 配置，消息仅在进程内流转 |

| `Storage.Type` | 说明 |
|----------------|------|
| `Sqlite`（默认） | 使用 `Storage.ConnectionString` |
| `PostgreSQL` / `Postgres` | 使用 PostgreSQL 作为 CAP 消息表存储 |
| `InMemory` | 进程内存储，适合本地开发 |

仅在 `Transport = RabbitMq` 时，`Storage` 配置才会按上表生效。

## 事件发布

### String topic 发布（当前业务主要用法）

发布方显式传入 topic 字符串，payload 可以是任意 DTO，不要求实现 `IIntegrationEvent`。

示例（参考 `HarborAdmin.Modules.AI`）：

```csharp
await eventPublisher.PublishAsync(
    AiEventTopics.ConfigPublished,
    new AiConfigPublishedEvent(release.Id, release.Version, release.Checksum),
    cancellationToken);
```

订阅方使用相同 topic 常量：

```csharp
public sealed class AiConfigPublishedSubscriber(AiRuntimeConfigCache configCache) : ICapSubscribe
{
    [CapSubscribe(AiEventTopics.ConfigPublished)]
    public Task HandleAsync(AiConfigPublishedEvent @event, CancellationToken cancellationToken = default) =>
        configCache.LoadReleaseAsync(@event.ReleaseId, cancellationToken);
}
```

推荐在模块 `Contracts` 中集中定义 topic 常量（如 `AiEventTopics`、`InternationalConstants`），避免散落魔法字符串。

### 强类型集成事件（BuildingBlock 提供）

适用于需要统一事件元数据（`Id`、`OccurredAt`、`CorrelationId` 等）的场景。事件类型必须实现 `IIntegrationEvent` 并声明 `[EventName]`，不使用类型名 fallback。

```csharp
[EventName("harbor.config.application.published.v1")]
public sealed class ConfigApplicationPublishedEvent : IntegrationEventBase
{
    public required string AppId { get; init; }
}
```

发布：

```csharp
await eventPublisher.PublishAsync(
    new ConfigApplicationPublishedEvent { AppId = appId },
    cancellationToken);
```

如果事件类型未声明 `[EventName]`，`IntegrationEventNameResolver` 会抛出明确异常。

两种发布方式可并存：模块可继续用 string topic + 普通 DTO；新集成事件可逐步迁移到 `IIntegrationEvent` + `[EventName]`。

## 订阅程序集

```csharp
builder.Services
    .AddHarborCap(builder.Configuration)
    .AddHarborCapSubscribers(typeof(InternationalTranslationSubscriber).Assembly);
```

业务订阅处理器使用 CAP 原生 `ICapSubscribe` + `[CapSubscribe("topic")]`。Host 当前注册 `InternationalTranslationSubscriber`；AIWorker 注册 `AiConfigPublishedSubscriber`。

示例（参考 `HarborAdmin.Modules.International`）：

```csharp
public sealed class InternationalTranslationSubscriber(InternationalTranslationService translationService) : ICapSubscribe
{
    [CapSubscribe(InternationalConstants.TranslationCompletedTopic)]
    public Task HandleAsync(AiBusinessResponse response, CancellationToken cancellationToken = default) =>
        translationService.ApplyAiTranslationAsync(response, cancellationToken);
}
```

## FreeSql 事务桥接

需要让数据库写入和 CAP 发布共享 outbox 事务时，在 FreeSql `IUnitOfWork` 上开启 CAP 事务。组合根需同时引用 Data 与 EventBus；`EventBus` 只直接依赖 FreeSql NuGet 类型，不引用 Data 项目。

```csharp
using var uow = unitOfWorkManagerCloud.Begin("AdminDb");
using var capTran = uow.BeginCapTran(capPublisher);

// 数据库写入
await eventPublisher.PublishAsync(AiEventTopics.ConfigPublished, payload, cancellationToken);

uow.Commit();
```

注意：

- `FreeSqlCapTransaction` 仅支持同步 `Commit()` / `Rollback()`；`CommitAsync` / `RollbackAsync` 会抛出 `NotSupportedException`。
- `BeginCapTran` 扩展方法定义在 `UnitOfWorkCapExtensions`，作用于 `IUnitOfWork`。

## Request/Reply

Request/Reply 默认关闭。`Harbor:Cap:RequestReply:Enabled = true` 时，`AddHarborCap` 会调用 `AddCapRequestReply(...)` 并注册 `IEventRequestClient`。

### Redis 响应通道（Harbor 跨服务推荐）

```json
{
  "Harbor": {
    "Cap": {
      "RequestReply": {
        "Enabled": true,
        "ServiceName": "harbor-admin-host",
        "InstanceId": "local-host",
        "DefaultTimeoutSeconds": 120,
        "Transport": "Redis",
        "Store": "InMemory",
        "EnableOpenTelemetryDiagnostics": false,
        "Redis": {
          "EndpointName": "harbor-admin-host",
          "ConnectionString": "localhost:6379",
          "StreamPrefix": "harbor:cap:reply"
        }
      }
    }
  }
}
```

Garnet 可作为 Redis 协议兼容服务接入，配置节点名仍使用 `Redis`。

### PostgreSQL 持久化 pending request

```json
{
  "Harbor": {
    "Cap": {
      "RequestReply": {
        "Enabled": true,
        "ServiceName": "harbor-admin",
        "Transport": "PostgreSql",
        "Store": "PostgreSql",
        "PostgreSql": {
          "ConnectionString": "Host=localhost;Database=harbor;Username=postgres;Password=postgres",
          "Schema": "cap",
          "ReplyTableName": "request_reply_inbox",
          "StoreTableName": "request_reply",
          "AutoCreateTable": true
        }
      }
    }
  }
}
```

### MySQL 持久化 pending request

```json
{
  "Harbor": {
    "Cap": {
      "RequestReply": {
        "Enabled": true,
        "ServiceName": "harbor-admin",
        "Transport": "MySql",
        "Store": "MySql",
        "MySql": {
          "ConnectionString": "Server=localhost;Database=harbor;User=root;Password=root",
          "TableNamePrefix": "cap",
          "ReplyTableName": "request_reply_inbox",
          "StoreTableName": "request_reply",
          "AutoCreateTable": true
        }
      }
    }
  }
}
```

### 请求方与处理方

请求方：

```csharp
var result = await eventRequestClient.RequestAsync<MyRequest, MyResponse>(
    "harbor.module.action.request.v1",
    request,
    TimeSpan.FromSeconds(10),
    cancellationToken);
```

处理方使用 `DotNetCore.Cap.RequestReply` 原生协议：

```csharp
public sealed class MyRequestSubscriber : ICapSubscribe
{
    [CapSubscribe("harbor.module.action.request.v1")]
    [CapRequestReply]
    public Task<MyResponse> HandleAsync(RequestEnvelope<MyRequest> request)
    {
        return Task.FromResult(new MyResponse());
    }
}
```

处理方可以返回普通响应对象，也可以返回 `ReplyEnvelope<T>` 表达业务失败。

推荐选型：

- 本地单进程测试：CAP `Transport = InMemory`（同时强制 InMemory 存储）；Request/Reply 使用 `Transport = InMemory`，`Store = InMemory`。
- Harbor 跨服务默认：RabbitMQ 承载 CAP 发布/订阅，Redis Stream 承载 Request/Reply 响应，InMemory store 保存短生命周期请求状态。
- 需要持久化 pending request 状态时：将 Request/Reply 的 `Store`（及可选 `Transport`）切到 PostgreSQL 或 MySQL。
