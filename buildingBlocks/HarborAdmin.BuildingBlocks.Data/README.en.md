# HarborAdmin.BuildingBlocks.Data

HarborAdmin FreeSql data access infrastructure: multi-database registration, module entity scanning, entity-to-database-key mapping, FreeSql global filters, audit field population, snowflake IDs, read/write splitting, and unit-of-work management.

## Scope

- Registers `HarborFreeSqlCloud` with support for single-database, multi-database, and read/write splitting.
- Uses `Harbor:DbConfig:Databases` as the single database configuration entry point.
- Scans entity types from `HarborAdmin.Modules.*` assemblies.
- In multi-database mode, maps entities to database keys via `[DbKey("...")]`.
- Registers `DbEntityRegistry` so repositories or business code can resolve the database key by entity type.
- Registers the default `IFreeSql` pointing to the first database in the `Databases` array.
- Registers `UnitOfWorkManagerCloud` to start FreeSql unit-of-work by database key.
- Registers `HarborFreeSqlInitializerHostedService` to trigger `HarborFreeSqlCloud` resolution and structure sync on Host startup.
- Registers a global soft-delete filter.
- Registers audit field population and snowflake ID population.
- Registers PostgreSQL `DateTimeOffset` mapping.
- Provides `IHarborFreeSqlPreSyncHook` to run migrations or preprocessing before CodeFirst structure sync.
- Provides a FreeSql `CurdAfter` side-channel extension point so the composition root can plug in cache invalidation, audit logging, domain events, and similar capabilities.

## Non-goals / Boundaries

This package does not handle:

- Cache read/write or cache invalidation.
- CAP event publishing, subscriptions, or message transactions.
- HTTP controllers, Host startup flow, or business module registration.
- Concrete query logic inside business repository implementations.
- Database migration script orchestration.
- Runtime tenant switching or user permission filtering.

When cache invalidation must be triggered after database writes, the composition root references both Data and Caching and wires the bridge through `HarborFreeSqlOptions.AddCurdAfterHandler(...)`.

## Project layout

```text
HarborAdmin.BuildingBlocks.Data/
  Auth/                                    Null current-user implementation
  Configs/                                 DbConfig, DbConnectionConfig, SlaveDb
  DbRegistration.cs                        FreeSqlBuilder, AOP, filters, structure sync
  FilterNames.cs                           Global filter names
  HarborFreeSqlCloud.cs                    Multi-database FreeSqlCloud<string>
  HarborFreeSqlInitializerHostedService.cs Triggers FreeSqlCloud initialization at startup
  HarborFreeSqlOptions.cs                  Registration options, DbEntityRegistry
  IHarborFreeSqlPreSyncHook.cs             Pre-CodeFirst sync hook
  ServiceCollectionExtensions.cs
  UnitOfWorkManagerCloud.cs                Multi-database unit-of-work manager
```

## DI integration

Register in Host or another composition root:

```csharp
builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName), options =>
{
    options.SnowflakeWorkerId = configuration.GetValue<ushort?>("Harbor:YitterWorkId") ?? 1;
    options.AddEntityAssembly(typeof(SomeEntity).Assembly);
    options.AddCurdAfterHandler(CacheInvalidationAopBridge.Dispatch);
});
```

After registration you can inject:

- `HarborFreeSqlCloud`
- `IFreeSql`
- `DbEntityRegistry`
- `UnitOfWorkManagerCloud`
- `ICurrentUser`

`ICurrentUser` is defined in `HarborAdmin.BuildingBlocks.Abstractions.Auth`. Data only provides a default null implementation via `TryAddSingleton<ICurrentUser, NullCurrentUser>()`. For real user auditing, Host should register its own `ICurrentUser` implementation before `HarborFreeSqlCloud` is resolved.

### Default IFreeSql

`IFreeSql` always points to the **first element** in `Harbor:DbConfig:Databases`. It is not necessarily the business primary database.

In the current Host configuration, the first entry is `ConfigCenterDb`, so the default `IFreeSql` points to the config center database. To access the Admin business database, use `cloud.Use("AdminDb")` or `registry.GetDbKey<TEntity>()`. Do not assume `IFreeSql` is the Admin database.

In multi-database scenarios, prefer resolving databases explicitly by `DbKey` instead of relying on array order.

## Database configuration

Configuration section name: `Harbor:DbConfig`.

Single-database configuration:

```json
{
  "Harbor": {
    "YitterWorkId": 1,
    "DbConfig": {
      "Databases": [
        {
          "Key": "AdminDb",
          "DataType": "PostgreSQL",
          "ConnectionString": "Host=localhost;Port=5432;Database=harbor;Username=postgres;Password=postgres",
          "SyncStructure": false,
          "ReadOnly": false
        }
      ]
    }
  }
}
```

Multi-database configuration:

```json
{
  "Harbor": {
    "YitterWorkId": 1,
    "DbConfig": {
      "Databases": [
        {
          "Key": "AdminDb",
          "DataType": "PostgreSQL",
          "ConnectionString": "Host=localhost;Port=5432;Database=harbor_admin;Username=postgres;Password=postgres",
          "SyncStructure": false,
          "ReadOnly": false
        },
        {
          "Key": "ConfigCenterDb",
          "DataType": "Sqlite",
          "ConnectionString": "Data Source=data/configcenter.db",
          "SyncStructure": true,
          "ReadOnly": false
        }
      ]
    }
  }
}
```

Read/write splitting:

```json
{
  "Harbor": {
    "DbConfig": {
      "Databases": [
        {
          "Key": "AdminDb",
          "DataType": "PostgreSQL",
          "ConnectionString": "Host=primary;Database=harbor;Username=postgres;Password=postgres",
          "SyncStructure": false,
          "ReadOnly": false,
          "SlaveList": [
            {
              "Weight": 1,
              "ConnectionString": "Host=readonly-1;Database=harbor;Username=postgres;Password=postgres"
            }
          ]
        }
      ]
    }
  }
}
```

### Configuration fields

| Field | Description |
|-------|-------------|
| `Key` | FreeSqlCloud registration key; in multi-database mode it matches `[DbKey]` |
| `DataType` | Database type; see supported list below |
| `ConnectionString` | Primary database connection string |
| `SyncStructure` | Whether to run CodeFirst sync for entities mapped to this database at startup |
| `ReadOnly` | Read-only database; when `true`, structure sync is skipped |
| `SlaveList` | Read/write splitting slave list |

### Snowflake ID configuration

`Harbor:YitterWorkId` configures the Yitter snowflake WorkerId (`ushort`, default `1`). Different Host instances should use different WorkerIds to avoid ID collisions in distributed environments.

### Supported DataType values

| Config value | FreeSql Provider package |
|--------------|--------------------------|
| `Sqlite` | `FreeSql.Provider.Sqlite` |
| `PostgreSQL` / `Postgres` | `FreeSql.Provider.PostgreSQL` |
| `SqlServer` / `MSSQL` | `FreeSql.Provider.SqlServer` |
| `MySql` | `FreeSql.Provider.MySql` |

These Provider packages are already referenced in `HarborAdmin.BuildingBlocks.Data.csproj`. When adding a new database type, update `ParseDataType`, package references, and this document together.

## Entity scanning and DbKey

Entity scanning rules:

- By default, scans `HarborAdmin.Modules.*` assemblies already loaded in the current AppDomain.
- Also scans `HarborAdmin.Modules.*` assemblies referenced by the entry assembly, calling `Assembly.Load` when needed.
- Additional assemblies can be added via `HarborFreeSqlOptions.AddEntityAssembly(...)`.
- Only non-abstract classes inheriting `EntityBase` are scanned.

Single-database mode:

- All entities map to the sole database.
- Entities do not need `[DbKey]`.

Multi-database mode:

- Every entity must explicitly declare `[DbKey("...")]`.
- The `[DbKey]` value must exist in `Harbor:DbConfig:Databases`.
- Registration preserves the original casing from configuration to keep `cloud.Use(dbKey)` consistent.

`[DbKey]` is defined in `HarborAdmin.BuildingBlocks.Abstractions.Domain`.

Example (from `HarborAdmin.Modules.International`):

```csharp
[DbKey("AdminDb")]
public sealed class InternationalEntry : AuditableEntity
{
    public long PageId { get; set; }
}
```

## DbEntityRegistry

`DbEntityRegistry` stores the entity-type-to-database-key mapping and is registered as a Singleton.

```csharp
var dbKey = registry.GetDbKey<InternationalEntry>();
var fsql = cloud.Use(dbKey);
```

- `GetDbKey` / `GetDbKey<T>`: throws `KeyNotFoundException` when unmapped.
- `TryGetDbKey(Type, out string)`: use when you need a non-throwing lookup.

## FreeSql registration behavior

`DbRegistration.RegisterDb(...)` creates a `FreeSqlBuilder` per database and registers:

- `UseConnectionString(...)`
- `UseAutoSyncStructure(false)`
- `UseSlave(...)` and `UseSlaveWeight(...)`
- Global soft-delete filter
- `AuditValue` audit field population
- `ConfigEntityProperty` entity property mapping adjustments
- Optional `CurdAfter` side-channel handlers

`SyncStructure` is not enabled inside `FreeSqlBuilder`. After entity scanning completes, `CodeFirst.SyncStructure(entityTypes)` is called per database key group.

Structure sync runs only when `SyncStructure = true` and `ReadOnly = false`. Read-only databases never run structure sync.

### Startup initialization

`HarborFreeSqlInitializerHostedService` forces resolution of the `HarborFreeSqlCloud` Singleton at Host startup, which triggers:

1. `FreeSqlBuilder` registration for each database
2. CodeFirst structure sync grouped by entity mappings
3. `BeforeSyncStructure` callbacks on each `IHarborFreeSqlPreSyncHook`

Modules can register `IHarborFreeSqlPreSyncHook` to run migration logic before structure sync, for example ConfigCenter's `ConfigCenterLegacyEnvironmentMigration`.

## Soft delete

All entities implementing `ISoftDelete` get a global filter:

```csharp
a => a.IsDeleted == false
```

Filter name:

```csharp
FilterNames.Delete
```

To include soft-deleted rows in special queries, use FreeSql's filter mechanism explicitly in the query context. Do not remove the global filter.

## Audit fields and snowflake IDs

This package handles audit behavior through FreeSql `AuditValue`:

- On insert, generates a snowflake ID for `EntityBase.Id`.
- If the property is marked as a database identity column (`[Column(IsIdentity = true)]`), snowflake ID generation is skipped.
- On insert, fills `IAuditable.CreatedAt`.
- On insert, fills `IAuditable.CreatedBy`; if the business layer already set a non-zero user ID, that value is preserved.
- On update and insert-or-update, refreshes `IAuditable.UpdatedAt`.
- On update and insert-or-update, refreshes `IAuditable.UpdatedBy` (always overwritten with the current user ID; business-layer old values are not preserved).

Snowflake IDs use `Yitter.IdGenerator`, initialized only once per process. WorkerId comes from `Harbor:YitterWorkId` or `HarborFreeSqlOptions.SnowflakeWorkerId`.

## PostgreSQL DateTimeOffset

When the database type is PostgreSQL, `DateTimeOffset` properties are explicitly mapped to:

```text
timestamp with time zone
```

This avoids column type inconsistencies across driver versions or default mapping strategies.

## UnitOfWorkManagerCloud

Multi-database unit-of-work entry:

```csharp
using var uow = unitOfWorkManagerCloud.Begin("AdminDb");

// Use cloud.Use("AdminDb") to get IFreeSql, then perform writes

uow.Commit();
```

You can also get a database-specific `UnitOfWorkManager` first:

```csharp
var manager = unitOfWorkManagerCloud.GetUnitOfWorkManager("AdminDb");
```

Notes:

- `UnitOfWorkManagerCloud` is a scoped service.
- It caches `UnitOfWorkManager` instances internally by `dbKey`.
- Dispose releases all created `UnitOfWorkManager` instances.
- Dispose is protected against repeated calls.

## CurdAfter extension point

Data provides a generic FreeSql post-write event extension point:

```csharp
builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName), options =>
{
    options.AddCurdAfterHandler((sp, eventArgs) =>
    {
        // Composition root bridges cache invalidation, audit logging, or other side-channel capabilities here.
    });
});
```

Design principles:

- Data only publishes FreeSql post-write events.
- Handlers are provided by the composition root.
- Handlers should isolate their own exceptions so side-channel failures do not affect completed database writes.
- Do not resolve Caching, EventBus, or business module services directly inside the Data project.

The current cache invalidation bridge lives in Host:

```text
services/HarborAdmin.Host/Infrastructure/CacheInvalidationAopBridge.cs
```
