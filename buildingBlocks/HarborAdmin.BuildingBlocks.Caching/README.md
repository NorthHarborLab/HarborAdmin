# HarborAdmin.BuildingBlocks.Caching

HarborAdmin 缓存基础包，提供对象缓存、强类型缓存模型、tag 失效、Redis/Garnet 结构化访问和底层 Redis 访问入口。

本包是独立 BuildingBlock，不依赖业务模块或 Host。数据库写入后的自动缓存失效由 Host 等组合根通过事件或 AOP 桥接接入，不能放回本包内部。

## 功能范围

- 支持 `Memory`、`Redis`、`Garnet` 三种 Provider。
- `Memory` 始终作为进程内一级缓存。
- `Redis` / `Garnet` 作为分布式二级缓存，并保存跨进程可用的 tag 索引。
- 支持 `IHarborCache` 的普通 key 读写、`GetOrCreateAsync`、按 key 删除、按 tag 删除。
- 支持 `cache.Get<TModel>().Where(...).GetOrCreateAsync(...)` 风格的强类型缓存模型。
- 支持 `[CacheKey]`、`[CacheKeyPart]`、`[CacheTag]`、`[CacheTagPart]` 通过 Attribute 生成 key 和 tag。
- 支持 `[CacheTag("...", typeof(Entity))]` 声明实体变更后需要失效的 tag 模板（同一类可声明多条）。
- 支持 Redis Hash、List、Counter 的强类型结构入口。
- 支持通过 `IHarborRedisClient` 直接获取 `IConnectionMultiplexer`、`IDatabase`、`ISubscriber` 做底层 Redis 操作（仅 Redis/Garnet Provider）。

## 边界约束

本包不负责：

- 连接数据库、订阅 FreeSql AOP、处理事务或读取实体仓储。
- 决定业务何时失效缓存。
- 做跨模块业务编排。
- 提供分布式锁、缓存击穿保护、后台预热、CAP 事件订阅。
- 保证 Memory Provider 的 tag 索引跨进程一致。

需要数据库侧自动失效时，由组合根同时引用 Data 和 Caching，在 Data 的写入完成事件中调用 `IHarborEntityCacheInvalidator`。当前 Host 的桥接示例位于：

- `services/HarborAdmin.Host/Infrastructure/CacheInvalidationAopBridge.cs`
- `services/HarborAdmin.Host/Program.cs`

## 项目结构

```text
HarborAdmin.BuildingBlocks.Caching/
  Abstractions/      对外抽象：IHarborCache、失效器、Redis 结构、Redis Client
  Attributes/        缓存模型、tag、Redis 结构 key 的 Attribute
  Infrastructure/    Memory/Redis/Garnet 运行时实现
  Internal/          反射元数据、表达式解析、模板格式化、KeyPrefix 归一化、失效规则发现
  Options/           Harbor:Cache 配置对象
  Serialization/     JSON 序列化工具
```

## 配置

配置节名称：`Harbor:Cache`。

```json
{
  "Harbor": {
    "Cache": {
      "Provider": "Memory",
      "KeyPrefix": "harbor",
      "DefaultExpirationSeconds": 600,
      "Redis": {
        "ConnectionString": "localhost:6379"
      },
      "Garnet": {
        "ConnectionString": "localhost:6379"
      }
    }
  }
}
```

### KeyPrefix

`CacheKeyNormalizer` 会在所有对象缓存与 tag 读写边界自动应用 `{KeyPrefix}:`：

- 对象缓存 key、tag、Redis 结构 key 均受此规则约束。
- 若调用方或 Attribute 已包含相同前缀（如 `harbor:admin:access:user`），则不会重复拼接。
- 业务侧可只写模块级路径（如 `admin:access:user`），由配置统一加上环境或实例前缀（如 `harbor`、`harbor-dev`）。
- tag 索引内部元数据使用 `{KeyPrefix}:cache:...` 命名空间，与业务 key 隔离。
- 修改 `KeyPrefix` 后，Redis 中已有数据不会自动迁移，需重新预热或手动清理。

### Provider

- `Memory`：仅进程内缓存；tag 索引也只在当前进程有效；`IHarborRedisClient` 未注册；`IHarborRedisStructures` 调用会抛出明确异常。
- `Redis`：使用 StackExchange.Redis + `Microsoft.Extensions.Caching.StackExchangeRedis`。
- `Garnet`：按 Redis 协议接入，底层仍使用 StackExchange.Redis。

## DI 接入

```csharp
builder.Services.AddHarborCaching(builder.Configuration.GetSection("Harbor:Cache"));
```

所有 Provider 均可注入：

- `IHarborCache`
- `IHarborCacheInvalidator`
- `IHarborEntityCacheInvalidator`
- `ICacheCatalogProvider`
- `IHarborCacheManager`

仅 `Redis` / `Garnet` Provider 额外注册：

- `IHarborRedisClient`
- `IHarborRedisStructures`

## Key 与 Tag 的业务范围

对象缓存同时维护 **key**（精确读写）与 **tag**（逻辑分组、批量失效）两套标识。二者职责不同，命名空间相关但不等价。

### 概念分工

| 维度 | Key | Tag |
|------|-----|-----|
| 业务含义 | 唯一标识**一份**缓存值 | 标识一类可一起失效的缓存**集合** |
| 主要用途 | `Get` / `Set` / 精确删除 | 按业务事件批量删除关联 key |
| 数量关系 | 一条 key 对应一个值 | 一条 key 可绑定多个 tag；一个 tag 可关联多条 key |
| 是否存储值 | 是，key 即缓存条目地址 | 否，tag 只维护「tag → keys」索引 |
| 典型失效方式 | `InvalidateKeyAsync` | `InvalidateTagAsync` / `InvalidateEntityAsync` |

可以把它理解为：**key 是仓库货位编号，tag 是品类/批次标签**。读取时找货位；整批下架时扫标签。

### 命名层次

HarborAdmin 推荐按层级组织（`KeyPrefix` 由配置统一追加，以下示例已含 `harbor`）：

```text
{KeyPrefix}:{模块}:{子域}:{资源}[:{实例标识...}]
```

常见模块示例：

| 模块 | Key 前缀示例 | Tag 前缀示例 | 业务子域 |
|------|-------------|-------------|---------|
| Admin 访问控制 | `harbor:admin:access:session-version:global` | `harbor:admin:access:users` | 用户快照、角色、运行时 schema |
| Admin 认证 | `harbor:admin:captcha-challenge:{CaptchaId}` | —（通常按 key 过期，无批量 tag） | 验证码、加密挑战 |
| International | `harbor:international:bundle` | `harbor:international:all` | 全量包、单页面包 |

**Key** 命名落到**具体条目**，通常由 `[CacheKey(Prefix)]` + `Key = "{Part1}:{Part2}"` 拼出，例如：

```text
harbor:admin:access:user:1001:3        ← 用户 1001、sessionVersion=3 的访问快照
harbor:international:page:login        ← pageKey=login 的页面资源包
harbor:admin:access:runtime:feature-apis  ← 全局 Feature API 列表
```

**Tag** 命名落到**失效范围**，不必与某一条 key 一一对应，例如：

```text
harbor:admin:access:users              ← 所有用户访问快照
harbor:admin:access:user:1001          ← 仅用户 1001 相关快照
harbor:international:page:login        ← 仅 login 页面相关条目
harbor:international:all               ← 国际化模块全量失效
```

### Key 的业务范围

Key 只回答一个问题：**「我要读/写哪一条缓存？」**

适用范围：

- 强类型模型：由 `[CacheKey]` + `[CacheKeyPart]` + `Where` 等值条件唯一确定。
- 普通对象缓存：调用方直接传入完整或模块级 key 字符串。
- 运维查看：按 key 精确读取 JSON 内容。

Key **不负责**表达「哪些条目应一起失效」。若只靠 key，批量清理只能逐条枚举或扫描前缀，成本高且容易遗漏。

当前仓库中的 Key 业务域示例：

| 缓存模型 | 实际 Key 形态 | 缓存内容 |
|---------|--------------|---------|
| `SessionVersionCacheModel` | `harbor:admin:access:session-version:global` | 全局会话版本号 |
| `UserAccessSnapshotCacheModel` | `harbor:admin:access:user:{UserId}:{SessionVersion}` | 单用户菜单/权限/角色快照 |
| `InternationalBundleCacheModel` | `harbor:international:bundle` | 全量国际化资源包 |
| `InternationalPageBundleCacheModel` | `harbor:international:page:{PageKey}` | 单页面资源包 |
| `EnabledFeatureApisCacheModel` | `harbor:admin:access:runtime:feature-apis` | 全局 Feature API 列表 |

### Tag 的业务范围

Tag 回答另一个问题：**「哪一类业务变更应让哪些缓存一起失效？」**

适用范围：

- 写入时绑定：强类型模型 `[CacheTag]` / `[CacheTagPart]`，或 `GetOrCreateAsync(..., tags: [...])`。
- 手动批量失效：`InvalidateTagAsync`。
- 数据库写入后自动失效：`[CacheTag(..., typeof(Entity))]` + `InvalidateEntityAsync`。
- 运维分组清理：`IHarborCacheManager.InvalidateGroupAsync` 按 `GroupPrefix` 汇总 tag 后批量失效。

Tag **不存储业务值**，只维护索引。失效 tag 时，框架查出所有关联 key，再逐条删除一级/二级缓存与索引。

#### 按失效粒度划分

| 粒度 | 含义 | 示例 tag | 典型触发场景 |
|------|------|---------|-------------|
| 模块全量 | 整个子域所有相关缓存 | `harbor:international:all` | 国际化批量发布、全量刷新 |
| 资源池全量 | 同一类资源的全部实例 | `harbor:admin:access:users` | 菜单/权限体系大改，所有用户快照作废 |
| 资源池全量（另一维度） | 按另一业务轴批量失效 | `harbor:admin:access:roles` | 角色/权限模型调整 |
| 类型共享 | 同一前缀下多种模型共用 | `harbor:admin:access:runtime` | Feature API/Action/Schema 任一变更 |
| 单实例 | 只影响一个业务实体相关条目 | `harbor:admin:access:user:{UserId}` | 某用户资料或角色关系变更 |
| 单页面/单角色 | 更细粒度的实体维度 | `harbor:international:page:{PageKey}` | 单页面文案更新 |

#### 一条 key 为何绑定多个 tag

以 `UserAccessSnapshotCacheModel` 为例，用户 `1001` 的某次快照 key 为 `harbor:admin:access:user:1001:3`，写入时会同时绑定：

```text
harbor:admin:access:users              ← 支持「清空全部用户快照」
harbor:admin:access:user:1001          ← 支持「只清用户 1001」（实体 AdminUser/AdminUserRole 变更时自动命中）
harbor:admin:access:roles              ← 支持「角色体系变更时连带失效用户快照」
```

这样不同业务事件可以命中不同粒度的失效，而不必在业务代码里维护 key 列表。

#### 带实体类型的 Tag（自动失效）

`[CacheTag("模板", typeof(Entity))]` 中的 `typeof(...)` **只用于数据库侧自动失效规则发现**，不改变 tag 字符串本身。

| 声明 | 写入时 | 数据库写入后 |
|------|--------|-------------|
| `[CacheTag("harbor:international:all", typeof(InternationalPage), typeof(InternationalEntry))]` | 绑定 tag `harbor:international:all` | `InternationalPage` / `InternationalEntry` 变更时，格式化模板并失效该 tag |
| `[CacheTag("harbor:admin:access:user:{UserId}", typeof(AdminUser))]` | 绑定 tag `harbor:admin:access:user:1001` | `AdminUser` 变更时，用实体字段替换 `{UserId}` 后失效 |

无 `typeof(...)` 的 tag 仍会在写入时绑定，但**不会**注册实体自动失效规则，需由业务显式调用 `InvalidateTagAsync`。

### 何时用 Key 失效，何时用 Tag 失效

| 场景 | 推荐方式 | 仓库示例 |
|------|---------|---------|
| 只作废一条已知缓存 | 按 key | `SessionVersionCacheModel` 在 `InvalidateUserAccessAsync` 中 `RemoveAsync` |
| 一类资源全部作废 | 按 tag | `InvalidateTagAsync(AdminAccessCacheKeys.AllUsersTag)` |
| 单实体相关条目作废 | 按带占位符的 tag 或实体自动失效 | `AdminUser` 更新 → `harbor:admin:access:user:{UserId}` |
| 管理端运维清理 | 按 tag / 分组 / key | `IHarborCacheManager` |
| 模块协调器统一入口 | 组合多个 tag | `InternationalCacheCoordinator.InvalidatePageAsync` 依次失效 `all`、`page-id`、`page` |

**选择原则：**

- 能明确到**单条目**且不会波及其他实例 → 用 **key**。
- 变更影响**不确定有多少条**、或需要**跨模型批量**清理 → 用 **tag**。
- 数据库实体写入后需自动清理 → 在模型上声明 **带 `typeof(Entity)` 的 tag**，交给 `InvalidateEntityAsync`。

### 不纳入 Tag 体系的范围

以下能力**只使用 key**，不参与 tag 绑定与按 tag 失效：

- Redis 结构化 API（`[RedisHash]` / `[RedisList]` / `[RedisCounter]`）。
- `IHarborRedisClient` 直接写入的 Redis key。
- 未传 `tags` 且模型未声明 `[CacheTag]` 的普通 `GetOrCreateAsync` 条目（只能按 key 删除，或等过期）。

### 业务侧维护建议

1. 每个模块建立 `*CacheKeys.cs`，集中定义 tag 常量与 key 段常量（参考 `AdminAccessCacheKeys`、`InternationalCacheKeys`）。
2. 稳定缓存条目沉淀为带 `[CacheKey]` 的模型；**失效维度**用 `[CacheTag]` 表达，避免在 Service 中硬编码 key 列表。
3. 同一业务域的模型共用 `GroupPrefix`（`[CacheCatalog]`），便于运维按组查看与清理。
4. 优先设计**粒度由粗到细**的 tag：模块全量 → 资源池 → 单实例，便于业务协调器组合失效。

## 普通对象缓存

以下示例在 `KeyPrefix = harbor` 时，传入 `international:bundle` 与传入 `harbor:international:bundle` 等价，实际存储 key 均为 `harbor:international:bundle`。

```csharp
var value = await cache.GetOrCreateAsync(
    "international:bundle",
    async ct => await LoadBundleAsync(ct),
    expiration: TimeSpan.FromMinutes(10),
    tags: ["international:all"],
    cancellationToken);
```

按 key 失效：

```csharp
await invalidator.InvalidateKeyAsync("international:bundle", cancellationToken);
```

按 tag 失效：

```csharp
await invalidator.InvalidateTagAsync("international:all", cancellationToken);
```

## 强类型缓存模型

强类型缓存模型用于把 key 和 tag 的拼接规则集中到模型上，调用方不再手写字符串。

示例参考 `HarborAdmin.Modules.International` 中的 `InternationalBundleCacheModel`：

```csharp
[CacheKey("harbor:international", Key = "{Id}", ExpirationSeconds = 600)]
[CacheTag("harbor:international:all", typeof(InternationalPage), typeof(InternationalEntry), typeof(InternationalEntryTranslation))]
public sealed class InternationalBundleCacheModel
{
    [CacheKeyPart]
    public string Id { get; init; } = "bundle";

    public InternationalBundleDto Value { get; init; } = new(0, new Dictionary<string, object>());
}
```

读取或创建：

```csharp
var model = await cache
    .Get<InternationalBundleCacheModel>()
    .Where(x => x.Id == "bundle")
    .GetOrCreateAsync(async ct => await LoadBundleAsync(ct), cancellationToken);
```

上述表达式在 `KeyPrefix = harbor` 时生成：

```text
harbor:international:bundle
```

强类型 Where 的限制：

- 只支持等值表达式。
- 多个条件只能用 `&&` 连接。
- 参与 key 的属性必须标记 `[CacheKeyPart]`。
- 不支持范围查询、`Contains`、`OR`、排序、分页或通用 LINQ 查询。

`GetOrCreateAsync` 仅在缓存未命中、执行 factory 并写入时绑定 tag；命中缓存不会重绑 tag。

## Tag 绑定与失效机制

写入强类型缓存时，`CacheModelMetadata` 会从模型实例生成 tag 并绑定到 key（详见上文「Key 与 Tag 的业务范围」）。

Tag 来源：

- 类上的 `[CacheTag("...")]`（支持 `AllowMultiple`，同一类可声明多条）
- 属性上的 `[CacheTagPart("...")]`
- 普通 `GetOrCreateAsync(..., tags: [...])` 手动传入

tag 模板使用 `{PropertyName}` 占位符，属性名大小写不敏感，格式化使用 `InvariantCulture`，避免区域设置影响 key/tag。

类级 `[CacheTag]` 追加 `typeof(Entity)` 时，同一声明兼作写入绑定与数据库自动失效规则；无实体类型时仅写入绑定，需业务显式 `InvalidateTagAsync`。

## 数据库侧自动失效

本包只提供失效规则和实体失效入口，不直接监听数据库：

```csharp
[CacheTag("harbor:international:page:{PageKey}", typeof(InternationalPage))]
[CacheTag("harbor:international:page-id:{PageId}", typeof(InternationalEntry))]
public sealed class InternationalPageBundleCacheModel
{
    // ...
}
```

当组合根捕获到数据库实体写入事件后，调用：

```csharp
await entityInvalidator.InvalidateEntityAsync(entity, operation, cancellationToken);
```

内部流程：

1. `CacheInvalidationRuleProvider` 扫描 `HarborAdmin.*` 程序集里带 `InvalidatesOn` 的 `[CacheTag]`。
2. 根据实体类型匹配规则（支持基类/接口声明）。
3. 使用实体属性格式化 tag 模板（同样经过 `KeyPrefix` 归一化）。
4. 调用 `IHarborCache.RemoveByTagAsync` 删除关联 key。

注意：

- `operation` 当前是扩展点，现有规则不按操作类型过滤。
- 失效规则扫描是进程内 Lazy 缓存。
- 组合根应隔离缓存失效异常，避免缓存故障影响已经完成的数据库写入。

## Redis 结构化 API

当 Provider 是 `Redis` 或 `Garnet` 时，可使用 Redis 原生结构。结构 key 同样会经过 `KeyPrefix` 归一化。

Hash key 模型：

```csharp
[RedisHash("user:profile:{UserId}")]
public sealed class UserProfileHashKey
{
    [RedisKeyPart]
    public required long UserId { get; init; }
}
```

Hash 使用：

```csharp
var hash = redisStructures.Hash(new UserProfileHashKey { UserId = userId });
await hash.HSetAsync("name", "admin", cancellationToken);
var name = await hash.HGetAsync<string>("name", cancellationToken);
```

List 和 Counter 分别使用 `[RedisList]`、`[RedisCounter]`。

当前 Redis 结构 API 的边界：

- value 统一使用 JSON 序列化。
- Hash/List/Counter 不自动绑定缓存 tag。
- Counter 使用 Redis 原子自增/自减。
- Memory Provider 下调用结构 API 会抛出异常，提示切换到 Redis 或 Garnet。

## 直接访问 Redis

需要执行本包未封装的 Redis 命令时注入 `IHarborRedisClient`（仅 Redis/Garnet Provider）：

```csharp
var db = redisClient.GetDatabase();
await db.StringSetAsync("harbor:custom:key", "value");
```

使用原则：

- 优先使用 `IHarborCache` 和 `IHarborRedisStructures`。
- `IHarborRedisClient` **不会**自动应用 `KeyPrefix`，直接访问 Redis 时由调用方自行维护 key 前缀、过期时间和一致性。
- 不要在业务模块里散落大量临时 key 拼接；稳定结构应沉淀为缓存模型或 Redis key 模型。

## 内部实现说明

关键类：

- `HarborCache`：对象缓存主实现，Memory 一级缓存，Redis/Garnet 二级缓存。
- `CacheKeyNormalizer`：统一应用 `Harbor:Cache:KeyPrefix`。
- `HarborCacheSet`：强类型缓存入口，负责把 `Where` 表达式转成 key。
- `ExpressionKeyParser`：只解析 `[CacheKeyPart]` 标记属性的等值条件。
- `CacheModelMetadata`：缓存模型 Attribute 元数据缓存。
- `TemplateFormatter`：统一处理 `{PropertyName}` 模板替换。
- `MemoryTagIndexStore`：进程内 tag 双向索引。
- `RedisTagIndexStore`：Redis Set tag 双向索引（内部 key 为 `{KeyPrefix}:cache:...`）。
- `CacheInvalidationRuleProvider`：扫描 `[CacheTag]` 声明的实体失效规则。
- `HarborRedisStructures`：Redis Hash/List/Counter 强类型门面。
- `UnavailableRedisStructures`：Memory Provider 下的显式失败实现。

## 缓存运维 API

注册 `AddHarborCaching` 后自动注入 `ICacheCatalogProvider` 与 `IHarborCacheManager`：

- `ICacheCatalogProvider`：扫描所有带 `[CacheKey]` 的类型；`[CacheCatalog]` 为可选元数据，用于展示名、分组、排序与额外脱敏字段。
- `IHarborCacheManager`：运行时 tag/key 查询、JSON 内容查看（脱敏 + 256KB 截断）、按 tag/key/分组失效。目录中的 prefix、groupPrefix、tag 模板均已包含 `KeyPrefix`。

运维查看内容时，`CacheEntryMasker` 默认脱敏 `PrivateKeyBase64`、`Password`、`Token`；`[CacheCatalog(SensitiveFields = [...])]` 可追加字段名。

示例参考 `HarborAdmin.Modules.Admin` 中的 `UserAccessSnapshotCacheModel`：

```csharp
[CacheCatalog("用户访问快照", GroupPrefix = "harbor:admin:access", GroupName = "Admin 访问控制", Module = "Admin", Order = 20, Description = "用户菜单/权限/角色快照")]
[CacheKey("harbor:admin:access:user", Key = "{UserId}:{SessionVersion}", ExpirationSeconds = 1800)]
[CacheTag("harbor:admin:access:users")]
[CacheTag("harbor:admin:access:user:{UserId}", typeof(AdminUser), typeof(AdminUserRole))]
[CacheTag("harbor:admin:access:roles", typeof(AdminRole), typeof(AdminRolePermission), typeof(AdminRoleMenu), typeof(AdminRoleDataScope), typeof(AdminRoleFieldPermission))]
public sealed class UserAccessSnapshotCacheModel
{
    [CacheKeyPart]
    public long UserId { get; init; }

    [CacheKeyPart]
    public long SessionVersion { get; init; }

    // ...
}
```

`IHarborCache.TryGetRawEntryAsync` 仅供运维读取原始 JSON，不改变业务 `GetAsync<T>` 语义。
