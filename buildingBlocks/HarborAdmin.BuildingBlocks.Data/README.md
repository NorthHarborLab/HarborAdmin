# HarborAdmin.BuildingBlocks.Data

HarborAdmin 的 FreeSql 数据访问基础设施 ，负责多库注册、模块实体扫描、实体到数据库 Key 的映射、FreeSql 全局过滤器、审计字段填充、雪花 ID、读写分离和工作单元管理。

## 功能范围

- 注册 `HarborFreeSqlCloud`，支持单库、多库和读写分离。
- 使用 `Harbor:DbConfig:Databases` 作为唯一数据库配置入口。
- 扫描 `HarborAdmin.Modules.*` 程序集中的模块元数据与实体类型。
- 在多库模式下通过模块 `{Module}ModuleMetadata.GetDbKey()` 建立默认数据库映射，特殊实体可用 `[OverrideDbKey("...")]` 覆盖。
- 注册 `DbModuleRegistry` 与 `DbEntityRegistry`，供模块 DbContext 和少量基础设施查找数据库 Key。
- 注册默认 `IFreeSql`，指向 `Databases` 数组中的第一个数据库。
- 注册 `UnitOfWorkManagerCloud`，支持按数据库 Key 开启 FreeSql 工作单元。
- 注册 `HarborFreeSqlInitializerHostedService`，在 Host 启动时触发 `HarborFreeSqlCloud` 解析与结构同步。
- 注册软删除全局过滤器。
- 注册审计字段填充和雪花 ID 填充。
- 注册 PostgreSQL `DateTimeOffset` 映射。
- 提供 `IHarborFreeSqlPreSyncHook`，在 CodeFirst 结构同步前执行迁移或预处理。
- 提供 FreeSql `CurdAfter` 旁路扩展点，让组合根接入缓存失效、审计日志、领域事件等能力。
- 提供 `AddHarborModuleData(...)`，统一注册模块 DbContext、仓储和可选 ServiceContext。

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
  HarborFreeSqlOptions.cs                  注册选项、DbModuleRegistry、DbEntityRegistry
  IHarborModuleDbContext.cs                模块 DbContext 公共契约
  HarborModuleDbContext.cs                 模块 DbContext 基类
  HarborModuleServiceCollectionExtensions.cs 模块通用 DI 注册扩展
  FreeSqlEntityRepository.cs               实体 CRUD 仓储基类，隐藏分库分表路由
  FreeSqlModuleRepository.cs               模块仓储基类
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
    options.AddModuleAssembly(typeof(SomeModuleMetadata).Assembly);
    options.AddCurdAfterHandler(CacheInvalidationAopBridge.Dispatch);
});
```

注册后可注入：

- `HarborFreeSqlCloud`
- `IFreeSql`
- `DbModuleRegistry`
- `DbEntityRegistry`
- `UnitOfWorkManagerCloud`
- `ICurrentUser`

`ICurrentUser` 定义在 `HarborAdmin.BuildingBlocks.Abstractions.Auth`，Data 只通过 `TryAddSingleton<ICurrentUser, NullCurrentUser>()` 提供默认空实现。需要真实用户审计时，Host 应在解析 `HarborFreeSqlCloud` 前注册自己的 `ICurrentUser` 实现。

### 默认 IFreeSql

`IFreeSql` 始终指向 `Harbor:DbConfig:Databases` 数组的**第一个元素**，与业务主库无必然对应关系。

当前 Host 配置中第一个是 `ConfigCenterDb`，因此默认 `IFreeSql` 指向配置中心库；访问模块业务库应优先使用 `IHarborModuleDbContext.DbKey` / `Orm`，不要假设默认 `IFreeSql` 就是业务库。

多库场景下建议显式按 `DbKey` 取库，避免依赖数组顺序。

### 模块 DbContext 与 ORM 选择

模块 DbContext 继承 `HarborModuleDbContext<TMetadata>`，并通过模块 metadata 从 `DbModuleRegistry` 解析模块默认数据库 Key。

| 成员 | 含义 |
|------|------|
| `DbKey` | 当前模块 metadata 声明的默认数据库 Key |
| `Orm` | 当前模块默认数据库对应的 `IFreeSql` |
| `GetOrm(string dbKey)` | 获取指定数据库 Key 对应的 `IFreeSql` |
| `Bind(IFreeSql orm)` | 在当前异步作用域绑定模块默认库的事务 ORM |
| `Bind(string dbKey, IFreeSql orm)` | 在当前异步作用域绑定指定数据库 Key 的事务 ORM |

`Orm` 只代表模块默认库。如果仓储操作的实体声明了 `[OverrideDbKey("...")]`，不能直接假设 `Orm` 就是该实体所在库，应通过 `DbEntityRegistry.GetDbKey<TEntity>()` 获取实体最终 DbKey，再调用 `GetOrm(dbKey)`。

标准实体 CRUD 仓储应优先继承 `FreeSqlEntityRepository<TEntity, TDbContext>`，由基类自动完成实体最终 DbKey 选择。只有模块级复杂聚合仓储才需要自己处理 `DbEntityRegistry`。

### 模块数据注册

模块扩展中优先使用 `AddHarborModuleData(...)` 注册模块 DbContext 与仓储，避免每个模块重复写同一组生命周期代码：

```csharp
services.AddHarborModuleData<IAdminDbContext, AdminDbContext, IAdminRepository, FreeSqlAdminRepository, AdminServiceContext>();
```

默认 DbContext 与仓储生命周期为 Singleton，ServiceContext 为 Scoped。少数模块如果仓储需要请求级生命周期，可显式指定：

```csharp
services.AddHarborModuleData<ISecretsDbContext, SecretsDbContext, ISecretsRepository, FreeSqlSecretsRepository>(
    repositoryLifetime: ServiceLifetime.Scoped);
```

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
| `Key` | FreeSqlCloud 注册键，多库模式下与模块 metadata 的 `GetDbKey()` 对应 |
| `DataType` | 数据库类型，见下方支持列表 |
| `ConnectionString` | 主库连接字符串 |
| `SyncStructure` | 是否在启动时对该库关联实体执行 CodeFirst 同步 |
| `ReadOnly` | 只读库；为 `true` 时不执行结构同步 |
| `SlaveList` | 读写分离从库列表 |

### 读写分离与 ReadOnly

`SlaveList` 是某个 DbKey 内部的读写分离配置。Data 层会在对应 `FreeSqlBuilder` 上调用 `UseSlave(...)` 和 `UseSlaveWeight(...)`，读写选择由 FreeSql 处理；模块 Service / Controller 不需要传主库或从库路由。

`ReadOnly` 当前只作为结构同步保护：当 `ReadOnly = true` 时，即使 `SyncStructure = true` 也不会执行 CodeFirst 同步。它不是应用层写入拦截器，也不能替代数据库账号权限。真正只读约束应由连接字符串使用只读账号、数据库权限或数据库实例能力保证。

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

## 模块扫描与 DbKey

模块扫描规则：

- 默认通过 `HarborModuleAssemblyDiscovery` 扫描当前 AppDomain 已加载的 `HarborAdmin.Modules.*` 程序集。
- 同时扫描入口程序集引用的 `HarborAdmin.Modules.*` 程序集，必要时主动 `Assembly.Load`。
- 可通过 `HarborFreeSqlOptions.AddModuleAssembly(...)` 追加扫描程序集。
- 每个模块程序集必须声明且只能声明一个 `IHarborModuleMetadata` 实现。
- 只扫描继承 `EntityBase` 的非抽象类。

单库模式：

- 所有模块实体映射到唯一数据库。
- 模块 metadata 仍需存在，但声明的 DbKey 不参与单库配置匹配。

多库模式：

- 每个模块 metadata 必须返回非空 DbKey。
- metadata 的 DbKey 必须存在于 `Harbor:DbConfig:Databases`。
- 普通实体使用模块默认 DbKey；只有需要覆盖模块默认库的实体才声明 `[OverrideDbKey("...")]`。
- `[OverrideDbKey]` 的值必须存在于 `Harbor:DbConfig:Databases`。
- 注册时保留配置文件里的数据库 Key 原始大小写，避免 `cloud.Use(dbKey)` 表示不一致。

示例：

```csharp
public sealed class InternationalModuleMetadata : HarborModuleMetadataBase
{
    public override string ModuleName => "International";

    public override string GetDbKey() => "AdminDb";
}
```

普通实体不声明数据库 Key，数据库归属由实体所在模块程序集的 metadata 供给。少数特殊实体可覆盖模块默认库：

```csharp
[OverrideDbKey("AuditDb")]
public sealed class AdminAuditLog : EntityBase
{
}
```

## DbModuleRegistry

`DbModuleRegistry` 保存模块 metadata 类型到运行时数据库 Key 的映射，注册为 Singleton。模块 DbContext 基类通过它解析实际使用的 DbKey。

## DbEntityRegistry

`DbEntityRegistry` 保存实体类型到数据库 Key 的映射，注册为 Singleton。

```csharp
var dbKey = registry.GetDbKey<InternationalEntry>();
var fsql = cloud.Use(dbKey);
```

- `GetDbKey` / `GetDbKey<T>`：未映射时抛出 `KeyNotFoundException`。
- `TryGetDbKey(Type, out string)`：需要容错时使用。

模块 DbContext 的 `DbKey` 表示模块默认库；对声明了 `[OverrideDbKey]` 的实体开启事务时，应通过 `DbEntityRegistry.GetDbKey<TEntity>()` 获取实体最终 DbKey。

## 实体 CRUD 仓储

普通实体 CRUD 仓储可继承 `FreeSqlEntityRepository<TEntity, TDbContext>`，由基类根据 `DbEntityRegistry.GetDbKey<TEntity>()` 自动选择实体最终数据库：

```csharp
public sealed class AiProviderRepository(IAiDbContext db, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
    : FreeSqlEntityRepository<AiProvider, IAiDbContext>(db, entityRegistry), IAiProviderRepository
{
}
```

分库规则不进入 Service / Controller 方法签名：

- 普通实体使用模块 metadata 的默认 DbKey。
- 标注 `[OverrideDbKey("...")]` 的实体使用覆盖 DbKey。
- Repository 每次操作按实体最终 DbKey 获取 ORM，不长期缓存 `IBaseRepository<TEntity>`。

分表规则也隐藏在实体仓储内部。需要分表时覆盖 `ResolveListTableNameAsync(...)`、`ResolvePageTableNameAsync(...)`、`ResolveGetTableNameAsync(...)`、`ResolveInsertTableNameAsync(...)`、`ResolveUpdateTableNameAsync(...)`、`ResolveDeleteTableNameAsync(...)` 等 protected 钩子返回物理表名；默认返回空表示不分表。Service 不传 DbKey、TableName 或路由对象。

`ApplyTable(...)` 是基类内部把物理表名应用到 FreeSql `Select` / `Insert` / `Update` / `Delete` 的统一入口：

- 钩子返回空：继续使用实体默认表。
- 钩子返回表名：通过 FreeSql `AsTable(...)` 切到指定物理表。

如果某个操作无法从请求、实体或业务上下文推导出唯一物理表，实体仓储应抛出明确的领域异常，要求业务层提供足够业务条件；不要把 DbKey、TableName 或通用路由对象暴露给 Service / Controller。

复杂聚合保存可以覆盖 `InsertAsync` / `UpdateAsync`，在同一个 `UnitOfWorkManagerCloud.Begin(DbKey)` 中保存主实体和子集合。

事务内要让仓储基类使用同一个 ORM，应临时绑定当前 DbKey：

```csharp
using var uow = unitOfWorkManager.Begin(DbKey);
using (DbContext.Bind(DbKey, uow.Orm))
{
    await base.InsertAsync(entity, cancellationToken);
    // 保存子集合
}

uow.Commit();
```

## 模块仓储基类

`FreeSqlModuleRepository<TDbContext>` 面向模块级复杂查询和聚合操作，例如跨多张模块内表的读取、发布快照、权限聚合等。它提供：

- `DbContext`：模块数据库上下文。
- `FreeSql`：模块默认数据库的 ORM。
- `GetRepository<TEntity>(cascadeSave)`：从模块默认数据库创建 FreeSql 仓储。
- `InsertAndFillIdAsync(...)`：插入并回填雪花 ID。

使用边界：

- 适合操作模块默认库内的实体。
- 不自动处理 `[OverrideDbKey]` 实体。
- 如果模块级仓储必须操作覆盖库实体，应注入 `DbEntityRegistry`，通过 `DbEntityRegistry.GetDbKey<TEntity>()` 获取实体最终 DbKey，再调用 `DbContext.GetOrm(dbKey)`。
- 形态稳定的单实体 CRUD 优先使用 `FreeSqlEntityRepository<TEntity, TDbContext>`，不要在模块仓储里重复写基础 CRUD。

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

// 使用 cloud.Use("AdminDb") 或模块 DbContext.GetOrm("AdminDb") 获取 IFreeSql 后执行写入

uow.Commit();
```

也可以先获取指定库的 `UnitOfWorkManager`：

```csharp
var manager = unitOfWorkManagerCloud.GetUnitOfWorkManager("AdminDb");
```

注意：

- `UnitOfWorkManagerCloud` 是 scoped 服务。
- 内部按 dbKey 缓存 `UnitOfWorkManager`。
- dbKey 应使用实体最终 DbKey；普通实体可以使用模块 `DbContext.DbKey`，覆盖库实体应使用 `DbEntityRegistry.GetDbKey<TEntity>()`。
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
