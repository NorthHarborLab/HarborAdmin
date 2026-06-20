# HarborAdmin — AI 编码规范

本文件是 **HarborAdmin 后台（.NET 10）** 的编码习惯与架构约定，供 AI 编码助手在修改本仓库代码前阅读。

- **基线模块**：`modules/HarborAdmin.Modules.Admin`（一切新代码应对齐该模块风格）
- **上级指引**：仓库根目录 [`AGENTS.md`](../AGENTS.md)（仓库结构、MCP、部署）；若与本文件冲突，**以本文件（更近路径）为准**
- **前端规范**：见 `HarborAdmin.Web/apps/harbor-admin` 与根 `AGENTS.md` 前端章节

---

## 1. 架构原则（必须遵守）

| 规则              | 说明                                                                                                                                        |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| Modular Monolith  | 业务按模块垂直切分，模块内自包含 Domain / Application / Contracts / Infrastructure / Controllers                                            |
| Host 只做组合根   | `services/HarborAdmin.Host` 负责 HTTP 管道、安全、DI 组合；**业务逻辑与 Controller 放在模块内**                                             |
| 跨模块边界        | 仅通过 `Contracts` 暴露类型；**禁止**引用他模块的 `Domain` 或 `Infrastructure`                                                              |
| ConfigCenter 进程 | `services/HarborAdmin.ConfigCenter` 仅 TCP JSON，**不引入 Kestrel/HTTP**                                                                    |
| AIWorker 边界     | `services/HarborAdmin.AIWorker` 保留独立执行进程与 `InternalAiController`；**不要求**将 Worker 业务下沉到 `Modules.AI`                      |
| TaskWorker 边界   | `services/HarborAdmin.TaskWorker` 只做调度、订阅和节点执行宿主；任务定义、触发器、执行记录、callable 抽象仍放在 `Modules.TaskOrchestration` |

```text
Host / Worker（组合根）
    └── Module.Controllers（薄 HTTP）
            └── Application.Services（业务）
                    └── Infrastructure.Repositories（持久化）
                            └── Domain.Entities
```

---

## 2. 模块目录结构

每个业务模块路径：`modules/HarborAdmin.Modules.{ModuleName}/`

```text
HarborAdmin.Modules.{ModuleName}/
├── {ModuleName}StartUp.cs             # 模块启动入口，声明 DbKey 并注册模块服务
├── README.md                          # 模块职责与路由（建议维护）
├── Domain/
│   └── Entities/                      # 实体 only，无独立 Domain Service
├── Application/
│   ├── Abstractions/                  # I*Repository、Host 窄接口
│   ├── Mappings/                      # Mapster IRegister
│   └── Services/
│       ├── {Area}/                    # 按业务域拆分 Service
│       └── Shared/                    # *ServiceContext、内部 Helper
├── Contracts/
│   └── {Area}/
│       ├── Dto/                       # 输出契约（单数 Dto）
│       └── Request/                   # 输入契约（单数 Request）
├── Controllers/
│   └── {Area}/                        # 按域分子目录
└── Infrastructure/
    ├── Contexts/                      # I*DbContext
    ├── Repositories/                  # FreeSql*Repository（partial）
    ├── Options/ Stores/ Caching/ ...  # 按需要
```

### Contracts 目录（禁止扁平复数目录）

| 禁止                   | 正确                                         |
| ---------------------- | -------------------------------------------- |
| `Contracts/Dtos/`      | `Contracts/{Area}/Dto/`                      |
| `Contracts/Requests/`  | `Contracts/{Area}/Request/`                  |
| `Contracts/Snapshots/` | `Contracts/Shared/Snapshot/` 或 `{Area}/...` |
| `Contracts/Constants/` | `Contracts/Shared/Constant/`                 |

**示例（AI 模块）**：`Contracts/Provider/Dto`、`Contracts/Business/Request`、`Contracts/Shared/Snapshot`。

---

## 3. 命名约定

### 3.1 命名空间

与文件夹一一对应，例如：

- `HarborAdmin.Modules.Admin.Application.Services.User`
- `HarborAdmin.Modules.Admin.Contracts.System.Dto`
- `HarborAdmin.Modules.Admin.Controllers.System`

### 3.2 类型命名

| 种类             | 模式                                                             | 示例                                                    |
| ---------------- | ---------------------------------------------------------------- | ------------------------------------------------------- |
| 实体             | `{Prefix}{Entity}`，`sealed`                                     | `AdminUser`、`AiProvider`、`HarborSecret`               |
| 主业务实体       | 继承 `AuditableEntity`                                           | `AdminUser`、`AiProvider`、`ConfigItem`                 |
| 关联/日志/快照表 | 继承 `EntityBase`                                                | `AdminUserRole`、`AiInvocationLog`                      |
| 输出 DTO         | `{Name}Dto`，优先 `sealed record`                                | `SystemUserDto`、`AiProviderDto`                        |
| 输入 Request     | `Save{Name}Request`，`sealed class`                              | `SaveSystemUserRequest`、`SaveAiProviderRequest`        |
| 应用服务         | `{Area}Service`，`sealed`，主构造函数                            | `UserService`、`ProviderService`                        |
| 服务上下文       | `{Module}ServiceContext` 或 `{Area}ServiceContext`               | `AdminServiceContext`、`SystemServiceContext`           |
| 实体仓储接口     | `I{Entity}Repository` 或 `I{Module}{Domain}Repository`           | `IAiProviderRepository`、`IAdminUserRepository`         |
| 复杂领域仓储实现 | `{Domain}Repository`，继承 `HarborRepository<TDbContext>` | `AdminAccessRepository`、`AdminFeatureDesignRepository` |
| DbContext        | `I{Module}DbContext` / `{Module}DbContext`                       | `IAdminDbContext`                                       |
| 模块启动入口     | `{Module}StartUp`                                                | `AdminStartUp`                                          |
| Controller       | `{Resource}Controller`，无模块前缀                               | `UserController`、`ProviderController`                  |
| Options          | `{Module}{Area}Options` + `SectionName` 常量                     | `AdminAuthOptions`                                      |

### 3.3 Controller 与路由

- **文件位置**：`Controllers/{Area}/{Resource}Controller.cs`
- **类名**：去掉模块前缀（`AiProvidersController` → `ProviderController`）
- **路由**：`[Route("...")]` **保持稳定**，重构时不得破坏前端已对接路径
- **命名空间**：`HarborAdmin.Modules.{Module}.Controllers.{Area}`

---

## 4. 实体（Domain/Entities）

### 4.1 基类选择

```csharp
// 主业务聚合 / 可维护配置表
public sealed class AdminUser : AuditableEntity { }

// 关联表、日志、配额桶、发布快照行等
public sealed class AdminUserRole : EntityBase { }
```

`AuditableEntity` 已含：`Id`、`CreatedAt`、`UpdatedAt`、`CreatedBy`、`UpdatedBy`。  
**禁止**在主业务实体上重复声明 `CreatedAt`/`UpdatedAt`。

### 4.2 FreeSql 注解

```csharp
[Index("ux_admin_user_name", nameof(UserName), true)]
public sealed class AdminUser : AuditableEntity
{
    [Column(StringLength = -1)]
    public string? Remark { get; set; }

    [Navigate(nameof(DeptId))]
    public AdminDepartment? Dept { get; set; }

    [Navigate(nameof(AdminUserRole.UserId))]
    public List<AdminUserRole> UserRoles { get; set; } = [];
}
```

| 规则         | 说明                                                                   |
| ------------ | ---------------------------------------------------------------------- |
| `sealed`     | 所有实体类必须 `sealed`                                                |
| `[Index]`    | 使用 `nameof(Field)` 或 `$"{nameof(A)},{nameof(B)}"`，禁止裸字符串列名 |
| `[Navigate]` | 有关联就必须建模导航属性                                               |
| **禁止**     | `[Column(IsIgnore = true)]` 标在导航属性上（会破坏 FreeSql 关系）      |
| 布尔启用     | 实体用 `Enabled`；对外 DTO 可映射为 `Status`（1/0）                    |
| 主键         | `long Id`；对外 DTO 的 Id 用 `string`（`Id.ToString()`）               |

普通实体不声明数据库 Key。模块默认数据库归属由模块根目录的 `{Module}StartUp` 声明，例如 `AdminStartUp.GetDbKey()` 返回 `AdminDb`；少数需要落到其他库的实体可声明 `[OverrideDbKey("...")]` 覆盖模块默认值。`AddHarborFreeSql` 会扫描模块启动入口与实体覆盖并建立最终映射。

### 4.3 查询加载

| 场景                   | 用法                                                        |
| ---------------------- | ----------------------------------------------------------- |
| ManyToOne / OneToOne   | `.Include(x => x.Dept)`                                     |
| OneToMany / ManyToMany | `.IncludeMany(x => x.UserRoles, then => then.Include(...))` |
| 关联过滤               | `Where(x => x.Children.Any(...))`，避免手写二次查询         |

---

## 5. Contracts（DTO / Request）

### 5.1 Dto（输出）

```csharp
/// <summary>
/// 系统用户。
/// </summary>
public sealed record SystemUserDto(
    string Id,
    string Name,
    string UserName,
    int Status,
    string CreateTime);
```

- 放在 `Contracts/{Area}/Dto/`
- 简单只读输出优先 `sealed record` + 位置参数
- 树形结构可用 `record` + `Children` 属性，服务层 `with` 填充

### 5.2 Request（输入）

```csharp
/// <summary>
/// 保存用户请求。
/// </summary>
public sealed class SaveSystemUserRequest
{
    /// <summary>
    /// 显示名称。
    /// </summary>
    [Required(ErrorMessage = "用户名称不能为空。")]
    [MaxLength(64)]
    public string Name { get; set; } = string.Empty;
}
```

| 规则     | 说明                                                                        |
| -------- | --------------------------------------------------------------------------- |
| 形态     | `sealed class` + DataAnnotations，**禁止** `sealed record` 作为 Request     |
| 命名     | 统一 `Save{Resource}Request`，Create/Update 合并为一个 Save 类型            |
| 校验     | `[Required]`、`[MaxLength]`、`[Range]` 等，`ErrorMessage` 使用**中文**      |
| 服务方法 | `Save{Resource}Async(long? id, SaveXxxRequest request, ...)` 或等价主键参数 |

### 5.3 映射

- 使用 Mapster `IRegister`（`Application/Mappings/*MappingRegister.cs`）
- Id、Status、时间格式在映射层集中转换，Controller 不手写

---

## 6. Application / Services

### 6.1 服务拆分

- **按业务域**一个 Service 一类职责（参考 `UserService`、`MenuService`、`ProviderService`）
- **禁止**巨型 `XxxManagementService` + 大量 partial 文件（历史代码应逐步拆分）
- 模块内业务 Service **一般不定义** `I*Service` 接口；仅 Host 边界与仓储使用接口

### 6.2 ServiceContext

共享 ORM、UoW、聚合加载、级联保存放在 `{Module}ServiceContext`：

```csharp
public sealed class SystemServiceContext(AdminServiceContext context, IAdminDbContext db)
{
    public IBaseRepository<AdminUser> GetUserRepository()
    {
        var repo = db.Orm.GetRepository<AdminUser>();
        repo.DbContextOptions.EnableCascadeSave = true;
        return repo;
    }

    public void SaveUserChildren(AdminUser user, string propertyName) =>
        GetUserRepository().SaveMany(user, propertyName);
}
```

### 6.3 典型保存流程

```csharp
public async Task<SystemUserDto> SaveUserAsync(long currentUserId, long? id, SaveSystemUserRequest request, CancellationToken cancellationToken)
{
    var now = DateTimeOffset.UtcNow;
    var user = id.HasValue
        ? await systemContext.LoadUserAggregateAsync(id.Value, cancellationToken)
          ?? throw new NotFoundDomainException("用户不存在。")
        : new AdminUser { CreatedAt = now };

    // 赋值、校验、关联子集合 ...
    user.UpdatedAt = now;

    // Insert / Update + SaveMany + 必要时 BumpSessionVersionAsync
    return mapper.Map<SystemUserDto>(user);
}
```

| 规则     | 说明                                                   |
| -------- | ------------------------------------------------------ |
| 时间戳   | `CreatedAt` / `UpdatedAt` 使用 `DateTimeOffset.UtcNow` |
| 取消     | 公开异步方法带 `CancellationToken cancellationToken`   |
| 校验失败 | 抛领域异常，**不**返回 `ApiResult.Fail`                |

形态稳定的普通 CRUD Service 优先继承 `HarborApplicationRepositoryService<TEntity, TDto, TSaveRequest, TRepository>`，其中 `TRepository` 实现 `IHarborCrudRepository<TEntity>`。基类直接调用实体仓储完成 `List/Get/Save/Delete`；子类只覆盖 `MapToDto`、`CreateEntity`、`ApplySaveAsync`、`ValidateCreateAsync`、`ValidateUpdateAsync`、`CanDeleteAsync`、`AfterSaveAsync`、`AfterDeleteAsync` 等业务钩子。

Service 不传 DbKey 或底层路由对象；分库由仓储根据模块 metadata / `[OverrideDbKey]` 决定。Controller 直接通过 `CrudControllerBase` 调用这些标准方法，不再保留 `SaveProviderAsync` 这类普通 CRUD 转发入口；确有业务语义差异的方法再单独命名。

---

## 7. Repository

```csharp
// Application/Abstractions/IAdminUserRepository.cs
public interface IAdminUserRepository
{
    Task<AdminUser?> GetUserAggregateAsync(long userId, CancellationToken cancellationToken = default);
}

// Infrastructure/Repositories/AdminUserRepository.cs
public sealed class AdminUserRepository(IAdminDbContext db, UnitOfWorkManagerCloud unitOfWorkManager)
    : HarborRepository<IAdminDbContext>(db, unitOfWorkManager), IAdminUserRepository
{
}
```

| 规则          | 说明                                                                                               |
| ------------- | -------------------------------------------------------------------------------------------------- |
| 标准实体 CRUD | 接口继承 `IHarborCrudRepository<TEntity>`，实现继承 `FreeSqlCrudRepository<TEntity, TDbContext>` |
| 复杂领域仓储  | 使用窄接口，例如 `IAdminAccessRepository`、`IAdminFeatureDesignRepository`                         |
| 模块总仓储    | 不再默认创建 `I{Module}Repository`；只有确有跨领域聚合且无法拆小时才允许                           |
| 生命周期      | DbContext 通常 `Singleton`；实体/领域仓储通常 `Scoped`                                             |

标准实体 CRUD 可新增实体级仓储并继承 `FreeSqlCrudRepository<TEntity, TDbContext>`；复杂聚合查询、发布快照、多表业务操作放入按领域命名的窄仓储。不要用一个巨型 `IAdminRepository` / `FreeSqlAdminRepository` 收纳所有方法，不要把 DbKey 或底层路由传到 Service / Controller。

---

## 8. Controllers

```csharp
/// <summary>
/// 系统用户管理。
/// </summary>
[ApiController]
[Route("api/admin/system/user")]
public sealed class UserController(UserService userService, ICurrentUser currentUser) : HarborControllerBase
{
    /// <summary>
    /// 创建用户。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SystemUserDto>> Create([FromBody] SaveSystemUserRequest request, CancellationToken cancellationToken) =>
        await OkResultAsync(userService.SaveUserAsync(currentUser.Id, null, request, cancellationToken));

    /// <summary>
    /// 删除用户。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await userService.DeleteUserAsync(id, cancellationToken);
        return OkResult(true);
    }
}
```

| 规则     | 说明                                                                                                                                                                          |
| -------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 职责     | 薄适配：编排 Service，**不含业务逻辑**                                                                                                                                        |
| 基类     | 所有模块 Controller 继承 `HarborControllerBase` / `CrudControllerBase` / `PagedCrudControllerBase`，不直接继承 `ControllerBase`                                               |
| 成功响应 | 通过 `OkResult(...)`、`OkResultAsync(...)`、`ListResultAsync(...)`、`PageResultAsync(...)`、`CreateResultAsync(...)`、`UpdateResultAsync(...)`、`DeleteResultAsync(...)` 包装 |
| 失败响应 | **禁止**在 Controller 调 `ApiResult.Fail`；Service 抛领域异常，由 Host 过滤器转换                                                                                             |
| 删除     | 无 body 时返回 `ApiResult<bool>` 且值为 `true`，优先调用 `DeleteResultAsync(...)`                                                                                             |
| 路由参数 | 使用约束，如 `{id:long}`、`{featureCode}`                                                                                                                                     |
| 流式例外 | `AiChatController` 等 SSE 端点可不返回 `ApiResult<T>`                                                                                                                         |

标准可分页 CRUD Controller 优先继承 `PagedCrudControllerBase<TDto, TQuery, TSaveRequest>`，Service 继承 `HarborApplicationPagedRepositoryService<...>` 并实现 `IPagedCrudApplicationService<TDto, TQuery, TSaveRequest>`，其中 `TQuery` 继承 `PageRequest`；Service 内部再转换为仓储使用的 `HarborQueryOptions`。标准非分页 CRUD 继承 `CrudControllerBase<TDto, TSaveRequest>` 并让 Service 实现 `ICrudApplicationService<TDto, TSaveRequest>`。父资源、业务键、当前用户、树、发布、动态元数据、副作用或流式接口不强行套 CRUD 模板，但必须继承 `HarborControllerBase` 并复用统一包装方法。具体 Controller 仍必须显式声明路由、HTTP Verb、XML 注释和需要的业务服务。

---

## 9. 错误处理与 ApiResult

| 异常                          | 典型场景           | HTTP   |
| ----------------------------- | ------------------ | ------ |
| `ValidationDomainException`   | 参数/业务校验      | 400    |
| `NotFoundDomainException`     | 资源不存在         | 404    |
| `ConflictDomainException`     | 重复、有关联不可删 | 409    |
| `UnauthorizedDomainException` | 登录/Token 失败    | 401    |
| `ForbiddenDomainException`    | 无权限             | 403    |
| `BusinessDomainException`     | 自定义业务码       | 可配置 |

```csharp
// 推荐
var entity = await repository.GetAsync(id, cancellationToken)
    ?? throw new NotFoundDomainException("用户不存在。");
```

消息语言：系统管理类偏中文；部分技术域（如 AI 配置）可用英文，同一模块内保持一致。

---

## 10. 依赖注入

```csharp
public sealed class AdminStartUp : HarborModuleMetadataBase, IHarborModuleStartup
{
    public override string ModuleName => "Admin";

    public override string GetDbKey() => "AdminDb";

    public void AddModule(IServiceCollection services, HarborModuleRegistrationContext context)
    {
        AddAdminInfrastructure(services);   // Singleton: DbContext, Repository
        AddAdminSystemManagement(services); // Scoped: Services, ServiceContext
    }
}
```

| 生命周期  | 典型注册                                                                  |
| --------- | ------------------------------------------------------------------------- |
| Singleton | `I*DbContext`、`I*Repository`、无状态基础设施                             |
| Scoped    | 业务 Service、ServiceContext、Store（请求级）                             |
| Options   | `services.AddOptions<TOptions>().BindConfiguration(TOptions.SectionName)` |

Host / Worker / ConfigCenter 的组合根通过 `AddHarborModules(moduleAssemblies, configuration, hostKind)` 扫描 `IHarborModuleStartup` 并注册模块，不在入口文件中手写 `Add{Module}Module()`。模块内如需区分宿主差异，通过 `HarborModuleRegistrationContext.HostKind` 判断。

Worker 进程只加载自身需要的模块程序集：

- `HarborAdmin.AIWorker`：加载 `AiStartUp`、`SecretsStartUp`，注册 AI 执行、供应商适配、配额与配置热加载。
- `HarborAdmin.TaskWorker`：加载 `TaskOrchestrationStartUp`，注册 Quartz、任务运行订阅者、节点执行器与 `ITaskCallableService`。

新增 Worker 时必须同步：

- 在 `HarborHostKinds` 增加宿主常量，并传给 `AddHarborModules(...)`。
- 为 Worker 设置独立 `Harbor:YitterWorkId` 默认值，避免同库雪花 ID 冲突。
- 只引用需要的模块项目，不把 Host 的全部模块默认搬进 Worker。
- 若配置来自 ConfigCenter，先确保对应 `AppId` 的配置项和发布快照存在；可用脚本放在 `scripts/`，但执行写库脚本前必须确认目标数据源和影响范围。

---

## 11. XML 注释

**必须**使用多行 `<summary>`，中文说明：

```csharp
/// <summary>
/// Admin 登录用户
/// </summary>
```

**禁止**单行写法：`/// <summary>xxx</summary>`

- 公开成员补充 `<param>`、`<returns>`
- C# 公开类、公开方法、公开属性、公开树形节点类型都必须添加 XML 注释。
- 实现接口可用 `/// <inheritdoc />`
- 仓储/服务文件需有文件级 `/// <summary>`
- 私有方法也必须添加注释说明。
- 对于复杂业务需增加行内注释。
- 注释中不要出现中文符号，最后不要有句号。

---

## 12. 数据库与迁移

- 开发库：`harbor@dev.harborlab.net`（PostgreSQL）
- 表名默认与实体类名一致（FreeSql）
- **执行写库/破坏性 SQL 前**必须说明数据源与影响范围，并获得确认

---

## 13. Git 提交信息

- 使用**中文简体**
- 首行：`type: 一句话概述`（如 `feat: 新增 AI 供应商保存接口`）
- type：`feat` / `fix` / `refactor` / `perf` / `test` / `docs` / `chore`
- 需要时空一行补充原因、影响范围、风险（≤4 行）

---

## 14. 新功能检查清单

新增或修改代码时，按此清单自检：

- [ ] Controller 在 `modules/.../Controllers/{Area}/`，路由未破坏兼容
- [ ] Controller 成功响应通过通用包装方法，失败走领域异常
- [ ] Contracts 在 `{Area}/Dto` 与 `{Area}/Request`（单数目录名）
- [ ] Request 为 `sealed class` + DataAnnotations，Save 命名
- [ ] 实体 `sealed`；主表 `AuditableEntity`；`[Index]` 用 `nameof`
- [ ] 导航属性已 `[Navigate]`，查询用 `Include`/`IncludeMany`
- [ ] Service 按域拆分，无巨型 partial Service
- [ ] Repository 使用实体仓储或窄领域仓储，避免巨型模块总仓储
- [ ] XML 多行 summary，中文简体，句末无句号。
- [ ] `dotnet build` 通过；涉及 API 契约变更时同步 `HarborAdmin.Web` 类型

---

## 15. 反模式（禁止）

| 反模式                                          | 正确做法                                |
| ----------------------------------------------- | --------------------------------------- |
| 在 Host 写业务 Controller                       | 放入对应 Module                         |
| `Contracts/Dtos`、`Contracts/Requests` 复数目录 | `{Area}/Dto`、`{Area}/Request`          |
| Controller 内 `ApiResult.Fail`                  | Service 抛 `NotFoundDomainException` 等 |
| Request 用 `record` 且无校验                    | `sealed class` + DataAnnotations        |
| 导航属性 `[Column(IsIgnore=true)]`              | 仅用 `[Navigate]`                       |
| 手写 JOIN 替代导航查询                          | `Include` / `IncludeMany`               |
| 巨型 `XxxManagementService`                     | 按域拆 `ProviderService` 等             |
| 单行 XML summary                                | 多行 `<summary>`                        |
| 跨模块引用 `Infrastructure`                     | 只引用 `Contracts`                      |

---

## 16. 参考文件（复制风格时优先打开）

| 用途           | 路径                                                                                                                                                                       |
| -------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 模块总览       | `modules/HarborAdmin.Modules.Admin/README.md`                                                                                                                              |
| 实体           | `modules/HarborAdmin.Modules.Admin/Domain/Entities/AdminUser.cs`                                                                                                           |
| Request        | `modules/HarborAdmin.Modules.Admin/Contracts/System/Request/SaveSystemUserRequest.cs`                                                                                      |
| DTO            | `modules/HarborAdmin.Modules.Admin/Contracts/System/Dto/SystemUserDto.cs`                                                                                                  |
| Service        | `modules/HarborAdmin.Modules.Admin/Application/Services/User/UserService.cs`                                                                                               |
| ServiceContext | `modules/HarborAdmin.Modules.Admin/Application/Services/Shared/SystemServiceContext.cs`                                                                                    |
| Controller     | `modules/HarborAdmin.Modules.Admin/Controllers/System/UserController.cs`                                                                                                   |
| Repository     | `modules/HarborAdmin.Modules.Admin/Infrastructure/Repositories/AdminUserRepository.cs`                                                                                     |
| 模块启动入口   | `modules/HarborAdmin.Modules.Admin/AdminStartUp.cs`                                                                                                                        |
| Worker 组合根  | `services/HarborAdmin.AIWorker/Program.cs`、`services/HarborAdmin.TaskWorker/Program.cs`                                                                                   |
| 任务编排示例   | `modules/HarborAdmin.Modules.TaskOrchestration/TaskOrchestrationStartUp.cs`、`modules/HarborAdmin.Modules.TaskOrchestration/Controllers/Tasks/TaskManagementController.cs` |
| 已对齐模块示例 | `Modules.AI`、`Modules.ConfigCenter`、`Modules.International`、`Modules.Secrets`、`Modules.TaskOrchestration`                                                              |

---

_本规范随 `Modules.Admin` 基线演进；大规模重构前先阅读模块 `README.md` 与现有同域代码。_
