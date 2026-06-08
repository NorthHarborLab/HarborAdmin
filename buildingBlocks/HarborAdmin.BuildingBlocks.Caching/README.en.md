# HarborAdmin.BuildingBlocks.Caching

HarborAdmin cache foundation package provides object caching, strongly-typed cache models, tag-based invalidation, Redis/Garnet structured access, and a low-level Redis access entry point.

This package is an independent BuildingBlock and does not depend on business modules or Host. Cache invalidation automatically triggered after database writes is bridged in by the Host (or other composition roots) via events or AOP; it cannot be put back inside this package.

## Scope

- Supports `Memory`, `Redis`, and `Garnet` providers.
- `Memory` is always used as the in-process first-level cache.
- `Redis` / `Garnet` are used as distributed second-level caches, and they store tag indices usable across processes.
- Supports standard key read/write on `IHarborCache`, `GetOrCreateAsync`, invalidation by key, and invalidation by tag.
- Supports strongly-typed cache models with the style `cache.Get<TModel>().Where(...).GetOrCreateAsync(...)`.
- Supports generating keys and tags via attributes: `[CacheKey]`, `[CacheKeyPart]`, `[CacheTag]`, `[CacheTagPart]`.
- Supports `[CacheTag("...", typeof(Entity))]` to declare tag templates that should be invalidated when entities change (multiple declarations are allowed on the same class).
- Supports strongly-typed structured Redis entry points for Hash, List, and Counter.
- Supports low-level Redis operations through `IHarborRedisClient` to get `IConnectionMultiplexer`, `IDatabase`, and `ISubscriber` (only for Redis/Garnet providers).

## Non-goals / Boundaries

This package does not handle:

- Connecting to databases, subscribing to FreeSql AOP, handling transactions, or reading entity repositories.
- Deciding when business logic should invalidate caches.
- Cross-module orchestration.
- Distributed locking, cache stampede protection, background warming, and CAP event subscriptions.
- Ensuring Memory Provider tag-index consistency across processes.

When database-side automatic invalidation is needed, the composition root references both Data and Caching, and calls `IHarborEntityCacheInvalidator` in the “database write completed” event. The current Host bridge example is here:

- `services/HarborAdmin.Host/Infrastructure/CacheInvalidationAopBridge.cs`
- `services/HarborAdmin.Host/Program.cs`

## Project layout

```text
HarborAdmin.BuildingBlocks.Caching/
  Abstractions/      Public abstractions: IHarborCache, invalidators, Redis structures, Redis client
  Attributes/        Attributes for cache models, tags, and Redis structure keys
  Infrastructure/    Runtime implementations for Memory/Redis/Garnet
  Internal/          Reflection metadata, expression parsing, template formatting, KeyPrefix normalization, invalidation rule discovery
  Options/           Harbor:Cache configuration object
  Serialization/     JSON serialization helpers
```

## Configuration

Configuration section name: `Harbor:Cache`.

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

`CacheKeyNormalizer` applies `{KeyPrefix}:` automatically at the object cache and tag read/write boundaries:

- Object cache keys, tags, and Redis structured keys all follow this rule.
- If the caller or the attribute already contains the same prefix (e.g. `harbor:admin:access:user`), it will not be duplicated.
- Business code can write only module-level paths (e.g. `admin:access:user`); the configuration adds the environment/instance prefix (e.g. `harbor`, `harbor-dev`).
- Tag-index internal metadata uses `{KeyPrefix}:cache:...` namespace to isolate it from business keys.
- After changing `KeyPrefix`, existing Redis data will not be migrated automatically; you must warm it up again or manually clean it.

### Provider

- `Memory`: in-process only. Tag index is also only valid within the current process. `IHarborRedisClient` is not registered. Calls to Redis structured APIs will throw a clear exception.
- `Redis`: uses StackExchange.Redis + `Microsoft.Extensions.Caching.StackExchangeRedis`.
- `Garnet`: connects via the Redis protocol; under the hood it still uses StackExchange.Redis.

## DI integration

```csharp
builder.Services.AddHarborCaching(builder.Configuration.GetSection("Harbor:Cache"));
```

All providers register:

- `IHarborCache`
- `IHarborCacheInvalidator`
- `IHarborEntityCacheInvalidator`
- `ICacheCatalogProvider`
- `IHarborCacheManager`

Only `Redis` / `Garnet` additionally register:

- `IHarborRedisClient`
- `IHarborRedisStructures`

## Key and Tag business scope

Object caching maintains two different identifiers at the same time:

- **Key**: precise identifier for read/write.
- **Tag**: logical grouping identifier for batch invalidation.

They serve different responsibilities. They are related in naming, but they are not equivalent.

### Conceptual division

| Dimension | Key | Tag |
|---|---|---|
| Business meaning | A unique identifier for one cached value | A set of cached items that can be invalidated together |
| Primary use | `Get` / `Set` / precise deletion | Batch deletion of keys associated with a business event |
| Cardinality | One key maps to one cached value | One key can bind multiple tags; one tag can map to many keys |
| Stores value? | Yes: the key is the cache entry address | No: tag only maintains a “tag -> keys” index |
| Typical invalidation | `InvalidateKeyAsync` | `InvalidateTagAsync` / `InvalidateEntityAsync` |

You can think of it as: **key is a storage bin number**, and **tag is a category/batch label**. Reads locate the bin; batch removals scan by label.

### Naming hierarchy

HarborAdmin recommends hierarchical naming. `KeyPrefix` is appended by configuration (the examples below already include `harbor`):

```text
{KeyPrefix}:{module}:{domain}:{resource}[:{instance-identifier...}]
```

Typical module examples:

| Module | Key prefix example | Tag prefix example | Business sub-domain |
|---|---|---|---|
| Admin access control | `harbor:admin:access:session-version:global` | `harbor:admin:access:users` | user snapshots, roles, runtime schema |
| Admin authentication | `harbor:admin:captcha-challenge:{CaptchaId}` | — (usually invalidated by expiration, not batch tags) | captchas, encryption challenges |
| International | `harbor:international:bundle` | `harbor:international:all` | full bundles, page bundles |

**Key** maps to **specific cache entries**. It’s typically created by `[CacheKey(Prefix)]` + `Key = "{Part1}:{Part2}"`, e.g.:

```text
harbor:admin:access:user:1001:3        # user 1001, sessionVersion=3 access snapshot
harbor:international:page:login        # pageKey=login page bundle
harbor:admin:access:runtime:feature-apis  # global feature API list
```

**Tag** maps to an **invalidation scope**, and does not need a 1:1 relation with a key, e.g.:

```text
harbor:admin:access:users              # all user access snapshots
harbor:admin:access:user:1001          # only user 1001 related snapshots
harbor:international:page:login        # only login page related entries
harbor:international:all               # invalidate all international module caches
```

### Key business scope

Key only answers one question: **“Which single cached entry should I read/write?”**

Applies to:

- Strongly-typed models: uniquely determined by `[CacheKey]` + `[CacheKeyPart]` + `Where` equality conditions.
- Plain object caching: caller passes the full key string (or a module-level key string).
- Operations/observability: read JSON content precisely by key.

Key **does not** express “which entries should be invalidated together”. If you rely only on keys, batch cleanup needs enumeration or prefix scanning, which is expensive and easy to miss.

Key business-domain examples in this repository:

| Cache model | Actual key shape | Cached content |
|---|---|---|
| `SessionVersionCacheModel` | `harbor:admin:access:session-version:global` | global session version |
| `UserAccessSnapshotCacheModel` | `harbor:admin:access:user:{UserId}:{SessionVersion}` | per-user menu/permission/role snapshot |
| `InternationalBundleCacheModel` | `harbor:international:bundle` | full internationalized resource bundle |
| `InternationalPageBundleCacheModel` | `harbor:international:page:{PageKey}` | single page resource bundle |
| `EnabledFeatureApisCacheModel` | `harbor:admin:access:runtime:feature-apis` | global feature API list |

### Tag business scope

Tag answers another question: **“Which kind of business change should invalidate which caches together?”**

Applies to:

- Bind on write: `[CacheTag]` / `[CacheTagPart]` on strongly-typed models, or `GetOrCreateAsync(..., tags: [...])`.
- Manual batch invalidation: `InvalidateTagAsync`.
- Automatic invalidation after database writes: `[CacheTag(..., typeof(Entity))]` + `InvalidateEntityAsync`.
- Ops grouping cleanup: `IHarborCacheManager.InvalidateGroupAsync` consolidates tags by `GroupPrefix`, then invalidates them in batch.

Tag **does not store business values**; it only maintains an index. When an invalidation tag is triggered, the framework finds all associated keys and then deletes both first-/second-level caches and the index entries per key.

#### Invalidation granularity

| Granularity | Meaning | Example tag | Typical trigger |
|---|---|---|---|
| Module-wide | all caches within the whole sub-domain | `harbor:international:all` | international batch publish, full refresh |
| Resource pool-wide | all instances of the same resource type | `harbor:admin:access:users` | big change to menu/permission system; all user snapshots become stale |
| Resource pool-wide (other dimension) | invalidate by another business axis | `harbor:admin:access:roles` | role/permission model changes |
| Type-shared | different models share the same prefix | `harbor:admin:access:runtime` | any change among feature API/action/schema |
| Single instance | affects only one business entity | `harbor:admin:access:user:{UserId}` | a user profile or relation change |
| Single page / single role | finer-grained entity dimensions | `harbor:international:page:{PageKey}` | a single page content update |

#### Why one key binds multiple tags

Take `UserAccessSnapshotCacheModel` as an example. A user `1001` snapshot key is `harbor:admin:access:user:1001:3`. When writing, it binds:

```text
harbor:admin:access:users              # supports “clear all user access snapshots”
harbor:admin:access:user:1001          # supports “clear only user 1001” (auto-hit on AdminUser/AdminUserRole changes)
harbor:admin:access:roles              # supports “roles system change” -> invalidate user snapshots
```

This allows different business events to hit different invalidation scopes without maintaining a key list in business services.

#### Entity-type tags (automatic invalidation)

In `[CacheTag("template", typeof(Entity))]`, `typeof(...)` is **only used for discovering automatic invalidation rules on the database side**; it does not change the tag string itself.

| Declaration | On write | After DB write |
|---|---|---|
| `[CacheTag("harbor:international:all", typeof(Page), typeof(Entry))]` | bind tag `harbor:international:all` | when `InternationalPage` / `InternationalEntry` change, format template and invalidate this tag |
| `[CacheTag("harbor:admin:access:user:{UserId}", typeof(AdminUser))]` | bind tag `harbor:admin:access:user:1001` | when `AdminUser` changes, replace `{UserId}` with entity fields and invalidate |

Tags without `typeof(...)` are still bound on write, but they **do not** register entity auto-invalidation rules. In that case, the business must explicitly call `InvalidateTagAsync`.

### When to invalidate by key vs by tag

| Scenario | Recommended method | Repository example |
|---|---|---|
| Invalidate only one known cache entry | by key | `SessionVersionCacheModel` calls `RemoveAsync` in `InvalidateUserAccessAsync` |
| Invalidate one resource pool completely | by tag | `InvalidateTagAsync(AdminAccessCacheKeys.AllUsersTag)` |
| Invalidate entries related to one entity | by tag with placeholders or entity auto-invalidation | `AdminUser` update -> `harbor:admin:access:user:{UserId}` |
| Ops cleanup in admin | by tag / by group / by key | `IHarborCacheManager` |
| Unified entry in a module coordinator | combine multiple tags | `InternationalCacheCoordinator.InvalidatePageAsync` invalidates `all`, `page-id`, `page` sequentially |

**Selection principles:**

- If you can clearly target a **single entry** without impacting other instances -> use **key**.
- If you cannot know exactly how many items are affected, or need **cross-model batch cleanup** -> use **tag**.
- If DB entity writes must automatically clean caches -> declare a **tag with `typeof(Entity)`** on the model and let `InvalidateEntityAsync` handle it.

### Not included in the Tag system

The following capabilities only use keys and are not part of tag binding / tag invalidation:

- Structured Redis API (`[RedisHash]` / `[RedisList]` / `[RedisCounter]`).
- Redis keys written directly via `IHarborRedisClient`.
- Plain `GetOrCreateAsync` entries without `tags` and without `[CacheTag]` (only key deletion or expiration applies).

### Business-side maintenance recommendations

1. Each module maintains a `*CacheKeys.cs` to centralize tag constants and key segment constants (see `AdminAccessCacheKeys`, `InternationalCacheKeys`).
2. Stabilize cache entries into models with `[CacheKey]`. Use `[CacheTag]` to express the invalidation dimension, avoiding hard-coded key lists in services.
3. Share `GroupPrefix` within the same business domain (via `[CacheCatalog]`) to make ops inspection and cleanup by group easier.
4. Design tag granularity from coarse to fine: module-wide -> resource pool -> single instance, so coordinators can compose invalidations efficiently.

## Plain object caching

The following example is equivalent when `KeyPrefix = harbor`: passing `international:bundle` versus passing `harbor:international:bundle`. In both cases, the stored key is `harbor:international:bundle`.

```csharp
var value = await cache.GetOrCreateAsync(
    "international:bundle",
    async ct => await LoadBundleAsync(ct),
    expiration: TimeSpan.FromMinutes(10),
    tags: ["international:all"],
    cancellationToken);
```

Invalidate by key:

```csharp
await invalidator.InvalidateKeyAsync("international:bundle", cancellationToken);
```

Invalidate by tag:

```csharp
await invalidator.InvalidateTagAsync("international:all", cancellationToken);
```

## Strongly-typed cache models

Strongly-typed cache models centralize key/tag composition rules on the model; callers no longer need to write strings manually.

Example: `InternationalBundleCacheModel` in `HarborAdmin.Modules.International`:

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

Read or create:

```csharp
var model = await cache
    .Get<InternationalBundleCacheModel>()
    .Where(x => x.Id == "bundle")
    .GetOrCreateAsync(async ct => await LoadBundleAsync(ct), cancellationToken);
```

With `KeyPrefix = harbor`, the expression generates:

```text
harbor:international:bundle
```

Strongly-typed `Where` limitations:

- Only equality expressions are supported.
- Multiple conditions must be connected with `&&`.
- Properties participating in the key must be marked with `[CacheKeyPart]`.
- No range queries, `Contains`, `OR`, sorting, paging, or general LINQ queries.

`GetOrCreateAsync` binds tags only when the cache is missing: it executes the factory and writes the value; on cache hits, it does not re-bind tags.

## Tag binding and invalidation mechanism

When writing strongly-typed caches, `CacheModelMetadata` generates tags from the model instance and binds them to keys (see the section above “Key and Tag business scope”).

Tag sources:

- Class-level `[CacheTag("...")]` (supports `AllowMultiple`, multiple declarations per class are allowed).
- Property-level `[CacheTagPart("...")]`.
- Manually passed `GetOrCreateAsync(..., tags: [...])`.

Tag templates use `{PropertyName}` placeholders. Property name matching is case-insensitive, and formatting uses `InvariantCulture` to avoid locale affecting key/tag values.

When class-level `[CacheTag]` includes `typeof(Entity)`, the same declaration serves both:

- binding tags on write
- registering database-side automatic invalidation rules

Without entity type information, the tag is still bound on write but automatic invalidation is not registered; the business must explicitly call `InvalidateTagAsync`.

## Database-side automatic invalidation

This package provides invalidation rules and an entity invalidation entry; it does not directly subscribe to database changes:

```csharp
[CacheTag("harbor:international:page:{PageKey}", typeof(InternationalPage))]
[CacheTag("harbor:international:page-id:{PageId}", typeof(InternationalEntry))]
public sealed class InternationalPageBundleCacheModel
{
    // ...
}
```

When the composition root captures a database entity write event, it calls:

```csharp
await entityInvalidator.InvalidateEntityAsync(entity, operation, cancellationToken);
```

Internal flow:

1. `CacheInvalidationRuleProvider` scans the `HarborAdmin.*` assemblies for `[CacheTag]` declarations with `InvalidatesOn` (entity types).
2. Matches rules by entity type (supports base class/interface declarations).
3. Formats tag templates using entity fields (also normalized by `KeyPrefix`).
4. Calls `IHarborCache.RemoveByTagAsync` to delete all related keys.

Notes:

- `operation` is an extension point; current rules do not filter by operation type.
- Rule scanning discovery is a process-local Lazy cache.
- The composition root should isolate cache invalidation exceptions so cache infrastructure failures do not affect completed database writes.

## Structured Redis API

When Provider is `Redis` or `Garnet`, you can use native Redis structured APIs. Structured keys are also normalized by `KeyPrefix`.

Hash key model:

```csharp
[RedisHash("user:profile:{UserId}")]
public sealed class UserProfileHashKey
{
    [RedisKeyPart]
    public required long UserId { get; init; }
}
```

Hash usage:

```csharp
var hash = redisStructures.Hash(new UserProfileHashKey { UserId = userId });
await hash.HSetAsync("name", "admin", cancellationToken);
var name = await hash.HGetAsync<string>("name", cancellationToken);
```

List and Counter use `[RedisList]` and `[RedisCounter]` respectively.

Boundaries of the structured Redis API:

- Values are always serialized as JSON.
- Hash/List/Counter do not automatically bind cache tags.
- Counter uses Redis atomic increment/decrement.
- Under the Memory provider, calling structured APIs throws an exception instructing you to switch to Redis or Garnet.

## Direct access to Redis

When you need to run Redis commands not wrapped by this package, inject `IHarborRedisClient` (only for Redis/Garnet providers):

```csharp
var db = redisClient.GetDatabase();
await db.StringSetAsync("harbor:custom:key", "value");
```

Guidelines:

- Prefer `IHarborCache` and `IHarborRedisStructures`.
- `IHarborRedisClient` **does not** automatically apply `KeyPrefix`. When you access Redis directly, the caller is responsible for maintaining key prefix, expiration, and consistency.
- Avoid spreading many ad-hoc temporary Redis keys in business modules; stable structures should be modeled as cache models or Redis key models.

## Internal implementation notes

Key components:

- `HarborCache`: main object cache implementation (Memory first-level; Redis/Garnet optional second-level).
- `CacheKeyNormalizer`: normalizes `Harbor:Cache:KeyPrefix`.
- `HarborCacheSet`: strongly-typed cache entry; converts `Where` expressions into a final key.
- `ExpressionKeyParser`: parses only equality conditions on `[CacheKeyPart]` properties.
- `CacheModelMetadata`: caches attribute metadata for cache models.
- `TemplateFormatter`: formats `{PropertyName}` placeholders consistently.
- `MemoryTagIndexStore`: in-process bidirectional tag index.
- `RedisTagIndexStore`: Redis Set bidirectional tag index (internal keys use `{KeyPrefix}:cache:...`).
- `CacheInvalidationRuleProvider`: scans entity invalidation rules declared via `[CacheTag]`.
- `HarborRedisStructures`: structured Redis Hash/List/Counter typed facade.
- `UnavailableRedisStructures`: explicit failure implementation under the Memory provider.

## Cache management API (ops)

After calling `AddHarborCaching`, `ICacheCatalogProvider` and `IHarborCacheManager` are injected:

- `ICacheCatalogProvider`: scans all types with `[CacheKey]`. `[CacheCatalog]` is optional metadata for display name, group, ordering, and extra sensitive fields.
- `IHarborCacheManager`: runtime tag/key querying, raw JSON viewing (masking + 256KB truncation), invalidation by tag/key/group. The catalog’s `prefix`, `groupPrefix`, and tag templates already include `KeyPrefix`.

When viewing data, `CacheEntryMasker` masks these fields by default: `PrivateKeyBase64`, `Password`, `Token`. `[CacheCatalog(SensitiveFields = [...])]` can add more field names.

Example: `UserAccessSnapshotCacheModel` in `HarborAdmin.Modules.Admin`:

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

`IHarborCache.TryGetRawEntryAsync` is for ops to read raw JSON and does not change the semantics of business `GetAsync<T>`.

