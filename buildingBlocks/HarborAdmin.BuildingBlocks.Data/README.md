# HarborAdmin.BuildingBlocks.Data

HarborAdmin 的 FreeSql 数据访问基础设施 ，负责多库注册、模块实体扫描、实体到数据库 Key 的映射、FreeSql 全局过滤器、审计字段填充、雪花 ID、读写分离和工作单元管理。

## 功能范围

- 注册 `HarborFreeSqlCloud`，支持单库、多库和读写分离。
- 使用 `Harbor:DbConfig:Databases` 作为唯一数据库配置入口。
- 扫描 `HarborAdmin.Modules.*` 程序集中的实体类型。
- 在多库模式下通过 `[DbKey("...")]` 建立实体到数据库 Key 的映射。
- 注册 `DbEntityRegistry`，供仓储或业务代码按实体类型查找数据库 Key。
- 注册默认 `IFreeSql`，指向 `Databases` 数组中的第一个数据库。
- 注册 `UnitOfWorkManagerCloud`，支持按数据库 Key 开启 FreeSql 工作单元。
- 注册 `HarborFreeSqlInitializerHostedService`，在 Host 启动时触发 `HarborFreeSqlCloud` 解析与结构同步。
- 注册软删除全局过滤器。
- 注册审计字段填充和雪花 ID 填充。
- 注册 PostgreSQL `DateTimeOffset` 映射。
- 提供 `IHarborFreeSqlPreSyncHook`，在 CodeFirst 结构同步前执行迁移或预处理。
- 提供 FreeSql `CurdAfter` 旁路扩展点，让组合根接入缓存失效、审计日志、领域事件等能力。

## 边界约束

本包不负责：

- 缓存读写或缓存失效。
- CAP 事件发布、订阅或消息事务。
- HTTP Controller、Host 启动流程或业务模块注册。
- 业务仓储实现中的具体查询逻辑。
- 数据库迁移脚本编排。
- 运行时切换租户或用户权限过滤。

需要数据库写入后触发缓存失效时，由 Host 等组合根同时引用 Data 和 Caching，并通过 `HarborFreeSqlOptions.AddCurdAfterHandler(...)` 接入桥接逻辑。

## 项目结构

```text
HarborAdmin.BuildingBlocks.Data/
  Auth/                                    当前用户空实现
  Configs/                                 DbConfig、DbConnectionConfig、SlaveDb
  DbRegistration.cs                        FreeSqlBuilder、AOP、过滤器、结构同步
  FilterNames.cs                           全局过滤器名称
  HarborFreeSqlCloud.cs                    多库 FreeSqlCloud<string>
  HarborFreeSqlInitializerHostedService.cs 启动时触发 FreeSqlCloud 初始化
  HarborFreeSqlOptions.cs                  注册选项、DbEntityRegistry
  IHarborFreeSqlPreSyncHook.cs             CodeFirst 同步前钩子
  ServiceCollectionExtensions.cs
  UnitOfWorkManagerCloud.cs                多库工作单元管理器
```

## DI 接入

Host 或服务组合根中注册：

```csharp
builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName), options =>
{
    options.SnowflakeWorkerId = configuration.GetValue<ushort?>("Harbor:YitterWorkId") ?? 1;
    options.AddEntityAssembly(typeof(SomeEntity).Assembly);
    options.AddCurdAfterHandler(CacheInvalidationAopBridge.Dispatch);
});
```

注册后可注入：

- `HarborFreeSqlCloud`
- `IFreeSql`
- `DbEntityRegistry`
- `UnitOfWorkManagerCloud`
- `ICurrentUser`

`ICurrentUser` 定义在 `HarborAdmin.BuildingBlocks.Abstractions.Auth`，Data 只通过 `TryAddSingleton<ICurrentUser, NullCurrentUser>()` 提供默认空实现。需要真实用户审计时，Host 应在解析 `HarborFreeSqlCloud` 前注册自己的 `ICurrentUser` 实现。

### 默认 IFreeSql

`IFreeSql` 始终指向 `Harbor:DbConfig:Databases` 数组的**第一个元素**，与业务主库无必然对应关系。

当前 Host 配置中第一个是 `ConfigCenterDb`，因此默认 `IFreeSql` 指向配置中心库；访问 Admin 业务库应使用 `cloud.Use("AdminDb")` 或 `registry.GetDbKey<TEntity>()`，不要假设 `IFreeSql` 就是 Admin 库。

多库场景下建议显式按 `DbKey` 取库，避免依赖数组顺序。

## 数据库配置

配置节名称：`Harbor:DbConfig`。

单数据库配置：

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

多库配置：

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

读写分离：

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

### 配置字段说明

| 字段 | 说明 |
|------|------|
| `Key` | FreeSqlCloud 注册键，多库模式下与 `[DbKey]` 对应 |
| `DataType` | 数据库类型，见下方支持列表 |
| `ConnectionString` | 主库连接字符串 |
| `SyncStructure` | 是否在启动时对该库关联实体执行 CodeFirst 同步 |
| `ReadOnly` | 只读库；为 `true` 时不执行结构同步 |
| `SlaveList` | 读写分离从库列表 |

### 雪花 ID 配置

`Harbor:YitterWorkId` 配置 Yitter 雪花 ID 的 WorkerId（`ushort`，默认 `1`）。不同 Host 实例应使用不同 WorkerId，避免分布式环境下 ID 冲突。

### 支持的 DataType

| 配置值 | FreeSql Provider 包 |
|--------|---------------------|
| `Sqlite` | `FreeSql.Provider.Sqlite` |
| `PostgreSQL` / `Postgres` | `FreeSql.Provider.PostgreSQL` |
| `SqlServer` / `MSSQL` | `FreeSql.Provider.SqlServer` |
| `MySql` | `FreeSql.Provider.MySql` |

以上 Provider 包已在 `HarborAdmin.BuildingBlocks.Data.csproj` 中引用。新增数据库类型时需同步更新 `ParseDataType`、包引用和本文档。

## 实体扫描与 DbKey

实体扫描规则：

- 默认扫描当前 AppDomain 已加载的 `HarborAdmin.Modules.*` 程序集。
- 同时扫描入口程序集引用的 `HarborAdmin.Modules.*` 程序集，必要时主动 `Assembly.Load`。
- 可通过 `HarborFreeSqlOptions.AddEntityAssembly(...)` 追加扫描程序集。
- 只扫描继承 `EntityBase` 的非抽象类。

单库模式：

- 所有实体映射到唯一数据库。
- 实体可不声明 `[DbKey]`。

多库模式：

- 每个实体必须显式声明 `[DbKey("...")]`。
- `[DbKey]` 的值必须存在于 `Harbor:DbConfig:Databases`。
- 注册时保留配置文件里的数据库 Key 原始大小写，避免 `cloud.Use(dbKey)` 表示不一致。

`[DbKey]` 定义在 `HarborAdmin.BuildingBlocks.Abstractions.Domain`。

示例（参考 `HarborAdmin.Modules.International`）：

```csharp
[DbKey("AdminDb")]
public sealed class InternationalEntry : AuditableEntity
{
    public long PageId { get; set; }
}
```

## DbEntityRegistry

`DbEntityRegistry` 保存实体类型到数据库 Key 的映射，注册为 Singleton。

```csharp
var dbKey = registry.GetDbKey<InternationalEntry>();
var fsql = cloud.Use(dbKey);
```

- `GetDbKey` / `GetDbKey<T>`：未映射时抛出 `KeyNotFoundException`。
- `TryGetDbKey(Type, out string)`：需要容错时使用。

## FreeSql 注册行为

`DbRegistration.RegisterDb(...)` 对每个数据库创建 `FreeSqlBuilder`，并注册以下行为：

- `UseConnectionString(...)`
- `UseAutoSyncStructure(false)`
- `UseSlave(...)` 和 `UseSlaveWeight(...)`
- 全局软删除过滤器
- `AuditValue` 审计字段填充
- `ConfigEntityProperty` 实体属性映射调整
- 可选 `CurdAfter` 旁路处理器

`SyncStructure` 不在 `FreeSqlBuilder` 中自动启用，而是在实体扫描完成后按数据库 Key 分组调用 `CodeFirst.SyncStructure(entityTypes)`。

结构同步条件：`SyncStructure = true` 且 `ReadOnly = false`。只读库不会执行结构同步。

### 启动初始化

`HarborFreeSqlInitializerHostedService` 在 Host 启动时强制解析 `HarborFreeSqlCloud` Singleton，触发：

1. 各数据库 `FreeSqlBuilder` 注册
2. 按实体映射分组执行 CodeFirst 结构同步
3. 各 `IHarborFreeSqlPreSyncHook` 的 `BeforeSyncStructure` 回调

模块可通过注册 `IHarborFreeSqlPreSyncHook` 在结构同步前执行迁移逻辑，例如 ConfigCenter 的 `ConfigCenterLegacyEnvironmentMigration`。

## 软删除

所有实现 `ISoftDelete` 的实体都会应用全局过滤器：

```csharp
a => a.IsDeleted == false
```

过滤器名称为：

```csharp
FilterNames.Delete
```

如需在特殊查询中包含软删除数据，按 FreeSql 的过滤器机制在查询上下文中显式处理，不要删除全局过滤器。

## 审计字段与雪花 ID

本包通过 FreeSql `AuditValue` 处理：

- 插入时为 `EntityBase.Id` 生成雪花 ID。
- 如果属性标记为数据库自增列（`[Column(IsIdentity = true)]`），则不生成雪花 ID。
- 插入时填充 `IAuditable.CreatedAt`。
- 插入时填充 `IAuditable.CreatedBy`；如果业务层已设置非 0 用户主键，则保留业务层输入。
- 更新和插入更新时刷新 `IAuditable.UpdatedAt`。
- 更新和插入更新时刷新 `IAuditable.UpdatedBy`（始终覆盖为当前用户 ID，不保留业务层旧值）。

雪花 ID 使用 `Yitter.IdGenerator`，进程内只初始化一次；WorkerId 来自 `Harbor:YitterWorkId` 或 `HarborFreeSqlOptions.SnowflakeWorkerId`。

## PostgreSQL DateTimeOffset

当数据库类型是 PostgreSQL 时，`DateTimeOffset` 属性会显式映射为：

```text
timestamp with time zone
```

这是为了避免不同驱动版本或默认映射策略带来的列类型不一致。

## UnitOfWorkManagerCloud

多库工作单元入口：

```csharp
using var uow = unitOfWorkManagerCloud.Begin("AdminDb");

// 使用 cloud.Use("AdminDb") 获取 IFreeSql 后执行写入

uow.Commit();
```

也可以先获取指定库的 `UnitOfWorkManager`：

```csharp
var manager = unitOfWorkManagerCloud.GetUnitOfWorkManager("AdminDb");
```

注意：

- `UnitOfWorkManagerCloud` 是 scoped 服务。
- 内部按 dbKey 缓存 `UnitOfWorkManager`。
- Dispose 时会释放已创建的所有 `UnitOfWorkManager`。
- Dispose 做了重复调用保护。

## CurdAfter 扩展点

Data 提供通用 FreeSql 写入完成事件扩展点：

```csharp
builder.Services.AddHarborFreeSql(builder.Configuration.GetSection(DbConfig.SectionName), options =>
{
    options.AddCurdAfterHandler((sp, eventArgs) =>
    {
        // 组合根在这里桥接缓存失效、审计日志或其他旁路能力。
    });
});
```

设计原则：

- Data 只发布 FreeSql 写入完成事件。
- 处理器由组合根提供。
- 处理器内部应自行隔离异常，避免旁路能力影响已经完成的数据库写入。
- 不要在 Data 项目里直接解析 Caching、EventBus 或业务模块服务。

当前缓存失效桥接位于 Host：

```text
services/HarborAdmin.Host/Infrastructure/CacheInvalidationAopBridge.cs
```