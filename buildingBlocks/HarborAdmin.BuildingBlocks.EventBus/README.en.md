# HarborAdmin.BuildingBlocks.EventBus

HarborAdmin event bus foundation package built on DotNetCore.CAP: reliable message publishing, subscriber assembly registration, FreeSql unit-of-work transaction bridging, and CAP Request/Reply request-response wrapping.

## Scope

- Registers CAP infrastructure.
- Supports RabbitMQ and InMemory transport.
- Supports Sqlite, PostgreSQL, and InMemory CAP storage.
- Provides `IEventPublisher` publishing abstraction.
- Supports string topic + arbitrary payload publishing (current primary usage in business modules).
- Supports strongly-typed integration events via `IIntegrationEvent` + `[EventName]`.
- Supports CAP subscriber assembly registration.
- Supports FreeSql `IUnitOfWork` and CAP outbox transaction bridging.
- Integrates `DotNetCore.Cap.RequestReply` and provides `IEventRequestClient` request-response abstraction.

## Non-goals / Boundaries

This package does not handle:

- Defining business event types.
- Implementing business subscriber handlers.
- Accessing database repositories.
- Cache invalidation.
- Registering Host, controllers, or business modules.
- Idempotent consumption persistence tables.
- Replacing CAP Dashboard failure-message operations.

## Project layout

```text
HarborAdmin.BuildingBlocks.EventBus/
  Configs/                          HarborCapOptions and CAP / RequestReply configuration objects
  CapServiceCollectionExtensions.cs AddHarborCap, AddHarborCapSubscribers
  CapEventPublisher.cs              IEventPublisher implementation
  CapEventRequestClient.cs          IEventRequestClient implementation
  EventNameAttribute.cs             Strongly-typed integration event topic declaration
  IntegrationEventBase.cs           Integration event base class
  IIntegrationEvent.cs              Integration event contract
  IntegrationEventNameResolver.cs   [EventName] resolution
  IEventPublisher.cs
  IEventRequestClient.cs
  UnitOfWorkCapExtensions.cs        FreeSql IUnitOfWork and CAP transaction bridge
```

## DI integration

```csharp
builder.Services
    .AddHarborCap(builder.Configuration, cap =>
    {
        // Can override DefaultGroup from configuration
        cap.DefaultGroupName = "harbor.admin.host";
    })
    .AddHarborCapSubscribers(typeof(InternationalTranslationSubscriber).Assembly);
```

`Harbor:Cap:DefaultGroup` maps to CAP's `DefaultGroupName`. Values set in the `configureCap` callback take precedence. The current Host pins the group name to `harbor.admin.host` in `Program.cs`.

After registration you can inject:

- `IEventPublisher` (always registered)
- `IEventRequestClient` (only when `Harbor:Cap:RequestReply:Enabled = true`)
- `ICapPublisher` (registered by CAP `AddCap`)

`AddHarborCap` also enables `EnablePublishParallelSend` and `UseStorageLock`, and uses `UnsafeRelaxedJsonEscaping` for JSON serialization.

## CAP configuration

Configuration section name: `Harbor:Cap`.

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

### Transport and Storage

| `Transport` | Behavior |
|-------------|----------|
| `RabbitMq` (default) | Connects to RabbitMQ using the `RabbitMq` section; message persistence is determined by `Storage` |
| `InMemory` | Calls `UseInMemoryStorage()`, which **overrides** the `Storage` configuration; messages stay in-process only |

| `Storage.Type` | Description |
|----------------|-------------|
| `Sqlite` (default) | Uses `Storage.ConnectionString` |
| `PostgreSQL` / `Postgres` | Uses PostgreSQL as CAP message table storage |
| `InMemory` | In-process storage, suitable for local development |

The `Storage` table above applies only when `Transport = RabbitMq`.

## Event publishing

### String topic publishing (current primary business usage)

The publisher passes an explicit topic string. The payload can be any DTO and does not need to implement `IIntegrationEvent`.

Example (from `HarborAdmin.Modules.AI`):

```csharp
await eventPublisher.PublishAsync(
    AiEventTopics.ConfigPublished,
    new AiConfigPublishedEvent(release.Id, release.Version, release.Checksum),
    cancellationToken);
```

The subscriber uses the same topic constant:

```csharp
public sealed class AiConfigPublishedSubscriber(AiRuntimeConfigCache configCache) : ICapSubscribe
{
    [CapSubscribe(AiEventTopics.ConfigPublished)]
    public Task HandleAsync(AiConfigPublishedEvent @event, CancellationToken cancellationToken = default) =>
        configCache.LoadReleaseAsync(@event.ReleaseId, cancellationToken);
}
```

Prefer centralizing topic constants in module `Contracts` (e.g. `AiEventTopics`, `InternationalConstants`) instead of scattering magic strings.

### Strongly-typed integration events (provided by this BuildingBlock)

Use when you need unified event metadata (`Id`, `OccurredAt`, `CorrelationId`, etc.). The event type must implement `IIntegrationEvent` and declare `[EventName]`; there is no type-name fallback.

```csharp
[EventName("harbor.config.application.published.v1")]
public sealed class ConfigApplicationPublishedEvent : IntegrationEventBase
{
    public required string AppId { get; init; }
}
```

Publish:

```csharp
await eventPublisher.PublishAsync(
    new ConfigApplicationPublishedEvent { AppId = appId },
    cancellationToken);
```

If `[EventName]` is missing, `IntegrationEventNameResolver` throws a clear exception.

Both styles can coexist: modules may keep string topic + plain DTOs while new integration events migrate to `IIntegrationEvent` + `[EventName]`.

## Subscriber assemblies

```csharp
builder.Services
    .AddHarborCap(builder.Configuration)
    .AddHarborCapSubscribers(typeof(InternationalTranslationSubscriber).Assembly);
```

Business subscribers use CAP's native `ICapSubscribe` + `[CapSubscribe("topic")]`. Host currently registers `InternationalTranslationSubscriber`; AIWorker registers `AiConfigPublishedSubscriber`.

Example (from `HarborAdmin.Modules.International`):

```csharp
public sealed class InternationalTranslationSubscriber(InternationalTranslationService translationService) : ICapSubscribe
{
    [CapSubscribe(InternationalConstants.TranslationCompletedTopic)]
    public Task HandleAsync(AiBusinessResponse response, CancellationToken cancellationToken = default) =>
        translationService.ApplyAiTranslationAsync(response, cancellationToken);
}
```

## FreeSql transaction bridge

When database writes and CAP publishing must share an outbox transaction, start a CAP transaction on a FreeSql `IUnitOfWork`. The composition root must reference both Data and EventBus; EventBus depends only on FreeSql NuGet types and does not reference the Data project.

```csharp
using var uow = unitOfWorkManagerCloud.Begin("AdminDb");
using var capTran = uow.BeginCapTran(capPublisher);

// Database writes
await eventPublisher.PublishAsync(AiEventTopics.ConfigPublished, payload, cancellationToken);

uow.Commit();
```

Notes:

- `FreeSqlCapTransaction` supports only synchronous `Commit()` / `Rollback()`; `CommitAsync` / `RollbackAsync` throw `NotSupportedException`.
- `BeginCapTran` is defined in `UnitOfWorkCapExtensions` and applies to `IUnitOfWork`.

## Request/Reply

Request/Reply is disabled by default. When `Harbor:Cap:RequestReply:Enabled = true`, `AddHarborCap` calls `AddCapRequestReply(...)` and registers `IEventRequestClient`.

### Redis reply channel (recommended for cross-service Harbor)

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

Garnet can be used as a Redis-protocol-compatible backend; the configuration section name remains `Redis`.

### PostgreSQL pending-request persistence

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

### MySQL pending-request persistence

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

### Requester and handler

Requester:

```csharp
var result = await eventRequestClient.RequestAsync<MyRequest, MyResponse>(
    "harbor.module.action.request.v1",
    request,
    TimeSpan.FromSeconds(10),
    cancellationToken);
```

Handler uses the native `DotNetCore.Cap.RequestReply` protocol:

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

Handlers may return a plain response object or `ReplyEnvelope<T>` to express business failure.

Recommended choices:

- Local single-process testing: CAP `Transport = InMemory` (also forces InMemory storage); Request/Reply with `Transport = InMemory`, `Store = InMemory`.
- Harbor cross-service default: RabbitMQ for CAP pub/sub, Redis Stream for Request/Reply responses, InMemory store for short-lived request state.
- When pending request state must persist: switch Request/Reply `Store` (and optionally `Transport`) to PostgreSQL or MySQL.
