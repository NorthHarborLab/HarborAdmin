# HarborAdmin.BuildingBlocks.Abstractions

HarborAdmin 的抽象基础设施项目，放置跨 Host、Worker、业务模块和基础设施项目共享的契约、基类、统一响应和领域异常。该项目保持轻量、低依赖，避免把 FreeSql、CAP、缓存、ASP.NET 管道等具体基础设施带入业务模块边界。

## 职责范围

- 定义所有模块都可引用的领域基类与标记接口。
- 定义统一 API 响应、分页请求和分页结果。
- 定义应用服务通用基类、基础 CRUD 服务契约和 Controller 响应包装。
- 定义模块元数据、模块启动契约与模块程序集发现器，供 Host、Data 等组合入口复用。
- 定义当前用户、字段权限、认证要求等跨模块安全抽象。
- 定义统一领域异常和业务错误码。
- 定义 Secret 读取相关抽象，避免业务模块直接依赖具体密钥存储实现。

本项目不负责：

- 数据库连接、FreeSql 注册、事务或结构同步。
- 缓存、CAP、消息总线、HTTP 管道和中间件。
- 模块内业务仓储实现或业务服务编排。
- 运行时配置加载、配置中心 TCP 协议或 Worker 执行逻辑。

## 目录说明

```text
HarborAdmin.BuildingBlocks.Abstractions/
  Application/    应用服务基类、基础 CRUD / 分页 CRUD 服务契约
  Attributes/     跨模块使用的 Attribute，例如认证、字段权限忽略、实体 DbKey 覆盖
  Auth/           当前用户抽象
  Controllers/    Harbor / CRUD / 分页 CRUD Controller 响应包装基类
  Domain/         EntityBase、AuditableEntity、软删/审计接口
  Enums/          跨模块通用枚举，例如 CRUD 删除决策
  Exception/      统一领域异常
  ModelResults/   ApiResult、分页请求和分页结果
  Modules/        模块元数据、模块启动契约、注册上下文与模块程序集发现器
  Repositories/   应用层可依赖的通用仓储契约
  Secrets/        Secret 解析和存储抽象
```

目录与命名空间保持一一对应。新增公共类型时，优先放入已有职责目录；只有当概念稳定且跨模块复用时才新增目录。

## 模块启动入口

每个业务模块必须在模块根命名空间定义一个 `{Module}StartUp`，继承 `HarborModuleMetadataBase` 并实现 `IHarborModuleStartup`。该类同时承担模块属性声明和模块 DI 注册入口。

```csharp
public sealed class AdminStartUp : HarborModuleMetadataBase, IHarborModuleStartup
{
    public override string ModuleName => "Admin";

    public override string GetDbKey() => "AdminDb";

    public void AddModule(IServiceCollection services, HarborModuleRegistrationContext context)
    {
        // 注册模块内 DbContext、Repository、Service、Options 等。
    }
}
```

`GetDbKey()` 表示模块默认数据库 Key。`HarborAdmin.BuildingBlocks.Data` 会扫描模块启动入口，并按模块默认 DbKey 建立实体到数据库的默认映射。

Host、ConfigCenter、AIWorker 等组合根通过 `AddHarborModules(...)` 扫描 `IHarborModuleStartup` 并调用 `AddModule(...)`。模块可以通过 `HarborModuleRegistrationContext.Configuration` 读取配置，通过 `HostKind` 区分 `Host`、`ConfigCenter`、`AIWorker` 等宿主差异。

`HarborModuleAssemblyDiscovery` 统一发现 `HarborAdmin.Modules.*` 程序集，发现顺序为显式追加程序集、当前 AppDomain 已加载程序集、入口程序集引用。Host 的 MVC `ApplicationPart` 注册、Data 的实体扫描和模块启动注册都应复用它，避免多套模块发现规则漂移。

### 实体级 DbKey 覆盖

普通实体不声明数据库 Key，使用模块默认 DbKey。少数实体需要落到其他库时，使用 `OverrideDbKeyAttribute` 覆盖模块默认值：

```csharp
[OverrideDbKey("AuditDb")]
public sealed class AdminAuditLog : EntityBase
{
}
```

`OverrideDbKeyAttribute` 只用于例外覆盖，不是普通实体的标准写法。使用覆盖实体开启事务时，不要使用模块 DbContext 的默认 `DbKey`，应通过 `DbEntityRegistry.GetDbKey<TEntity>()` 获取该实体最终 DbKey。

## 领域基类

实体基类位于 `Domain/`：

- `IEntity`：统一实体标记。
- `EntityBase`：统一 `long Id` 主键。
- `IAuditable`：审计字段契约。
- `AuditableEntity`：包含主键、创建/更新时间、创建/更新人。
- `ISoftDelete`：软删除过滤契约。

`EntityBase` 保持零 FreeSql 依赖。具体数据库映射、主键生成、审计字段填充和软删过滤由 Data 项目接入。

`OverrideDbKeyAttribute` 位于 `Attributes/`。它不是领域基类的一部分，只是实体级数据库归属的例外覆盖标记，用来避免恢复旧的“每个实体都写 DbKey”的语义。

## API 与 Controller 基础能力

`ModelResults/` 中的 `ApiResult` / `ApiResult<T>` 是 HTTP API 的统一响应模型：

- 成功响应使用 `ApiResult.Ok(...)`。
- Controller 不直接返回 `ApiResult.Fail(...)` 表示业务失败。
- 业务失败由应用服务抛出领域异常，Host 过滤器转换为统一响应。

`Controllers/` 提供三类 Controller 基础能力：

- `HarborControllerBase`：所有非标准 Controller 的最小基类，继承 ASP.NET Core `ControllerBase`，提供 `OkResult(...)`、`OkResultAsync(...)`、`ListResultAsync(...)`、`PageResultAsync(...)`、`CreateResultAsync(...)`、`UpdateResultAsync(...)`、`DeleteResultAsync(...)`。
- `CrudControllerBase<TDto, TSaveRequest>`：标准非分页 CRUD 基类，服务实现 `ICrudApplicationService<TDto, TSaveRequest>`。
- `PagedCrudControllerBase<TDto, TQuery, TSaveRequest>`：标准分页 CRUD 基类，服务实现 `IPagedCrudApplicationService<TDto, TQuery, TSaveRequest>`，其中 `TQuery` 继承 `PageRequest`，服务层负责转换为仓储使用的 `HarborQueryOptions`。

标准可分页 CRUD Controller 优先使用分页基类：

```csharp
public sealed class ProviderController(ProviderService service)
    : PagedCrudControllerBase<AiProviderDto, PageRequest, SaveAiProviderRequest>
{
    [HttpGet]
    public async Task<ApiResult<PagedResult<AiProviderDto>>> List(
        [FromQuery] PageRequest query,
        CancellationToken cancellationToken) =>
        await PageResultAsync(query, service, cancellationToken);
}
```

标准非分页 CRUD Controller 可复用 List / Get / Create / Update / Delete 的响应包装：

```csharp
public sealed class ProviderController(ProviderService service)
    : CrudControllerBase<AiProviderDto, SaveAiProviderRequest>
{
    [HttpPost]
    public async Task<ApiResult<AiProviderDto>> Create(
        [FromBody] SaveAiProviderRequest request,
        CancellationToken cancellationToken) =>
        await CreateResultAsync(request, service, cancellationToken);
}
```

具体 Controller 仍必须显式声明路由、HTTP Verb、XML 注释和业务服务；`CrudControllerBase` / `PagedCrudControllerBase` 不生成隐式端点。

## 应用服务基础能力

`HarborApplicationService` 提供轻量公共能力：

- `UtcNow`：统一当前 UTC 时间入口。
- `RequireText(...)`：规范化必填文本并抛出校验异常。
- `RequireFound(...)`：要求对象存在并抛出未找到异常。

`HarborApplicationService` 只提供 Guard、时间等轻量公共能力，不承载 CRUD 方法模板。模块内形态稳定的普通 CRUD 不应重复手写这些方法，应优先使用 Repository 驱动基类。

`Repositories/` 中的 `IHarborRepository` / `IHarborRepository<TEntity>` 是仓储根契约，`IHarborQueryRepository<TEntity>` 与 `IHarborCrudRepository<TEntity>` 分别承载标准查询和 CRUD 能力。普通 CRUD 应用服务优先继承 `HarborApplicationRepositoryService<TEntity, TDto, TSaveRequest, TRepository>`，让基类直接调用仓储完成 `List/Get/Save/Delete`，子类只实现 DTO 映射、请求落实体、保存/删除校验等钩子。

`HarborApplicationPagedRepositoryService<TEntity, TDto, TQuery, TSaveRequest, TRepository>` 继承 Repository CRUD 基类并实现 `IPagedCrudApplicationService<TDto, TQuery, TSaveRequest>`。形态稳定的分页资源服务可继承它；树形、动态查询、发布或带副作用的接口不要为了套分页基类而改变业务形态。

Controller 应直接通过 `CrudControllerBase` 调用这些标准 CRUD 方法，不再为普通 CRUD 保留 `ListProvidersAsync`、`SaveProviderAsync` 这类转发壳。复杂查询、树形资源、权限副作用、发布或流式接口不强行套用该契约。

应用服务不直接引用 FreeSql、FreeSqlCloud 或 DbKey。分库由仓储实现根据模块 metadata 与 `[OverrideDbKey]` 自动完成。

## 领域异常

领域异常统一放在 `Exception/`：

- `ValidationDomainException`
- `NotFoundDomainException`
- `ConflictDomainException`
- `UnauthorizedDomainException`
- `ForbiddenDomainException`
- `BusinessDomainException`

服务层通过抛异常表达业务失败，Host 统一转换 HTTP 状态码和 `ApiResult`。

## 设计约束

- Abstractions 只能放稳定、跨模块共享、低依赖的契约和基类。
- 不允许在本项目中引入 FreeSql、CAP、Redis、HTTP 中间件或具体数据访问实现。
- 跨模块只能通过 Contracts、应用服务、事件或只读模型协作，禁止直接引用其他模块的 Domain / Infrastructure。
- 新增通用能力前先确认至少两个模块有真实重复需求；不要把单一模块的业务规则上提到 Abstractions。
