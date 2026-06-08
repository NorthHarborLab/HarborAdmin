# HarborAdmin — AI 编码规范

本文件是 **HarborAdmin 后台（.NET 10）** 的编码习惯与架构约定，供 AI 编码助手在修改本仓库代码前阅读。

- **基线模块**：`modules/HarborAdmin.Modules.Admin`（一切新代码应对齐该模块风格）
- **上级指引**：仓库根目录 [`AGENTS.md`](../AGENTS.md)（仓库结构、MCP、部署）；若与本文件冲突，**以本文件（更近路径）为准**
- **前端规范**：见 `HarborAdmin.Web/apps/harbor-admin` 与根 `AGENTS.md` 前端章节

---

## 1. 架构原则（必须遵守）

| 规则 | 说明 |
|------|------|
| Modular Monolith | 业务按模块垂直切分，模块内自包含 Domain / Application / Contracts / Infrastructure / Controllers |
| Host 只做组合根 | `services/HarborAdmin.Host` 负责 HTTP 管道、安全、DI 组合；**业务逻辑与 Controller 放在模块内** |
| 跨模块边界 | 仅通过 `Contracts` 暴露类型；**禁止**引用他模块的 `Domain` 或 `Infrastructure` |
| ConfigCenter 进程 | `services/HarborAdmin.ConfigCenter` 仅 TCP JSON，**不引入 Kestrel/HTTP** |
| AIWorker 边界 | `services/HarborAdmin.AIWorker` 保留独立执行进程与 `InternalAiController`；**不要求**将 Worker 业务下沉到 `Modules.AI` |

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
├── {ModuleName}ModuleExtensions.cs    # DI 入口，命名空间在模块根
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

| 禁止 | 正确 |
|------|------|
| `Contracts/Dtos/` | `Contracts/{Area}/Dto/` |
| `Contracts/Requests/` | `Contracts/{Area}/Request/` |
| `Contracts/Snapshots/` | `Contracts/Shared/Snapshot/` 或 `{Area}/...` |
| `Contracts/Constants/` | `Contracts/Shared/Constant/` |

**示例（AI 模块）**：`Contracts/Provider/Dto`、`Contracts/Business/Request`、`Contracts/Shared/Snapshot`。

---

## 3. 命名约定

### 3.1 命名空间

与文件夹一一对应，例如：

- `HarborAdmin.Modules.Admin.Application.Services.User`
- `HarborAdmin.Modules.Admin.Contracts.System.Dto`
- `HarborAdmin.Modules.Admin.Controllers.System`

### 3.2 类型命名

| 种类 | 模式 | 示例 |
|------|------|------|
| 实体 | `{Prefix}{Entity}`，`sealed` | `AdminUser`、`AiProvider`、`HarborSecret` |
| 主业务实体 | 继承 `AuditableEntity` | `AdminUser`、`AiProvider`、`ConfigItem` |
| 关联/日志/快照表 | 继承 `EntityBase` | `AdminUserRole`、`AiInvocationLog` |
| 输出 DTO | `{Name}Dto`，优先 `sealed record` | `SystemUserDto`、`AiProviderDto` |
| 输入 Request | `Save{Name}Request`，`sealed class` | `SaveSystemUserRequest`、`SaveAiProviderRequest` |
| 应用服务 | `{Area}Service`，`sealed`，主构造函数 | `UserService`、`ProviderService` |
| 服务上下文 | `{Module}ServiceContext` | `AdminServiceContext`、`AiServiceContext` |
| 仓储接口 | `I{Module}Repository`，`partial` | `IAdminRepository`、`IAiRepository` |
| 仓储实现 | `FreeSql{Module}Repository`，`partial` | `FreeSqlAdminRepository.System.cs` |
| DbContext | `I{Module}DbContext` / `{Module}DbContext` | `IAdminDbContext` |
| Controller | `{Resource}Controller`，无模块前缀 | `UserController`、`ProviderController` |
| DI 扩展 | `{Module}ModuleExtensions.Add{Module}Module()` | `AdminModuleExtensions` |
| Options | `{Module}{Area}Options` + `SectionName` 常量 | `AdminAuthOptions` |

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
[DbKey("AdminDb")]  // 或 ConfigCenterDb 等，与 DbEntityRegistry 一致
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

| 规则 | 说明 |
|------|------|
| `sealed` | 所有实体类必须 `sealed` |
| `[Index]` | 使用 `nameof(Field)` 或 `$"{nameof(A)},{nameof(B)}"`，禁止裸字符串列名 |
| `[Navigate]` | 有关联就必须建模导航属性 |
| **禁止** | `[Column(IsIgnore = true)]` 标在导航属性上（会破坏 FreeSql 关系） |
| 布尔启用 | 实体用 `Enabled`；对外 DTO 可映射为 `Status`（1/0） |
| 主键 | `long Id`；对外 DTO 的 Id 用 `string`（`Id.ToString()`） |

### 4.3 查询加载

| 场景 | 用法 |
|------|------|
| ManyToOne / OneToOne | `.Include(x => x.Dept)` |
| OneToMany / ManyToMany | `.IncludeMany(x => x.UserRoles, then => then.Include(...))` |
| 关联过滤 | `Where(x => x.Children.Any(...))`，避免手写二次查询 |

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

| 规则 | 说明 |
|------|------|
| 形态 | `sealed class` + DataAnnotations，**禁止** `sealed record` 作为 Request |
| 命名 | 统一 `Save{Resource}Request`，Create/Update 合并为一个 Save 类型 |
| 校验 | `[Required]`、`[MaxLength]`、`[Range]` 等，`ErrorMessage` 使用**中文** |
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

| 规则 | 说明 |
|------|------|
| 时间戳 | `CreatedAt` / `UpdatedAt` 使用 `DateTimeOffset.UtcNow` |
| 取消 | 公开异步方法带 `CancellationToken cancellationToken` |
| 校验失败 | 抛领域异常，**不**返回 `ApiResult.Fail` |

---

## 7. Repository

```csharp
// Application/Abstractions/IAdminRepository.cs
public partial interface IAdminRepository { }

// Application/Abstractions/IAdminRepository.System.cs
public partial interface IAdminRepository
{
    Task<AdminUser?> GetUserAggregateAsync(long userId, CancellationToken cancellationToken = default);
}

// Infrastructure/Repositories/FreeSqlAdminRepository.cs
public sealed partial class FreeSqlAdminRepository(IAdminDbContext db) : IAdminRepository
{
    private IFreeSql FreeSql => db.Orm;
}
```

| 规则 | 说明 |
|------|------|
| 接口 | 单一 `I{Module}Repository` + `partial` 按域分文件 |
| 实现 | `FreeSql{Module}Repository` + `partial`，主文件只持有 `IFreeSql` |
| 生命周期 | 通常 `Singleton`（与 `I*DbContext` 一致） |
| partial 文件头 | 必须有模块级 `/// <summary>` 多行注释 |

---

## 8. Controllers

```csharp
/// <summary>
/// 系统用户管理。
/// </summary>
[ApiController]
[Route("api/admin/system/user")]
public sealed class UserController(UserService userService, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>
    /// 创建用户。
    /// </summary>
    [HttpPost]
    public async Task<ApiResult<SystemUserDto>> Create([FromBody] SaveSystemUserRequest request, CancellationToken cancellationToken) =>
        ApiResult.Ok(await userService.SaveUserAsync(currentUser.Id, null, request, cancellationToken));

    /// <summary>
    /// 删除用户。
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<ApiResult<bool>> Delete(long id, CancellationToken cancellationToken)
    {
        await userService.DeleteUserAsync(id, cancellationToken);
        return ApiResult.Ok(true);
    }
}
```

| 规则 | 说明 |
|------|------|
| 职责 | 薄适配：编排 Service，**不含业务逻辑** |
| 成功响应 | 仅 `ApiResult.Ok(...)` |
| 失败响应 | **禁止**在 Controller 调 `ApiResult.Fail`；Service 抛领域异常，由 Host 过滤器转换 |
| 删除 | 无 body 时返回 `ApiResult.Ok(true)` |
| 路由参数 | 使用约束，如 `{id:long}`、`{featureCode}` |
| 流式例外 | `AiChatController` 等 SSE 端点可不返回 `ApiResult<T>` |

---

## 9. 错误处理与 ApiResult

| 异常 | 典型场景 | HTTP |
|------|----------|------|
| `ValidationDomainException` | 参数/业务校验 | 400 |
| `NotFoundDomainException` | 资源不存在 | 404 |
| `ConflictDomainException` | 重复、有关联不可删 | 409 |
| `UnauthorizedDomainException` | 登录/Token 失败 | 401 |
| `ForbiddenDomainException` | 无权限 | 403 |
| `BusinessDomainException` | 自定义业务码 | 可配置 |

```csharp
// 推荐
var entity = await repository.GetAsync(id, cancellationToken)
    ?? throw new NotFoundDomainException("用户不存在。");
```

消息语言：系统管理类偏中文；部分技术域（如 AI 配置）可用英文，同一模块内保持一致。

---

## 10. 依赖注入

```csharp
public static IServiceCollection AddAdminModule(this IServiceCollection services)
{
    AddAdminInfrastructure(services);   // Singleton: DbContext, Repository
    AddAdminSystemManagement(services); // Scoped: Services, ServiceContext
    return services;
}
```

| 生命周期 | 典型注册 |
|----------|----------|
| Singleton | `I*DbContext`、`I*Repository`、无状态基础设施 |
| Scoped | 业务 Service、ServiceContext、Store（请求级） |
| Options | `services.AddOptions<TOptions>().BindConfiguration(TOptions.SectionName)` |

Host `Program.cs` 只调用 `Add{Module}Module()`，不在 Host 内注册模块内部类型。

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
- 实现接口可用 `/// <inheritdoc />`
- partial 仓储/服务分文件需有文件级 `/// <summary>`
- 私有方法也必须添加注释说明。
- 对于复杂业务需增加行内注释。
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
- [ ] Controller 仅 `ApiResult.Ok`，失败走领域异常
- [ ] Contracts 在 `{Area}/Dto` 与 `{Area}/Request`（单数目录名）
- [ ] Request 为 `sealed class` + DataAnnotations，Save 命名
- [ ] 实体 `sealed`；主表 `AuditableEntity`；`[Index]` 用 `nameof`
- [ ] 导航属性已 `[Navigate]`，查询用 `Include`/`IncludeMany`
- [ ] Service 按域拆分，无巨型 partial Service
- [ ] Repository 扩展 `I*Repository.{Domain}.cs` + `FreeSql*Repository.{Domain}.cs`
- [ ] XML 多行 summary，中文句末 `。`
- [ ] `dotnet build` 通过；涉及 API 契约变更时同步 `HarborAdmin.Web` 类型

---

## 15. 反模式（禁止）

| 反模式 | 正确做法 |
|--------|----------|
| 在 Host 写业务 Controller | 放入对应 Module |
| `Contracts/Dtos`、`Contracts/Requests` 复数目录 | `{Area}/Dto`、`{Area}/Request` |
| Controller 内 `ApiResult.Fail` | Service 抛 `NotFoundDomainException` 等 |
| Request 用 `record` 且无校验 | `sealed class` + DataAnnotations |
| 导航属性 `[Column(IsIgnore=true)]` | 仅用 `[Navigate]` |
| 手写 JOIN 替代导航查询 | `Include` / `IncludeMany` |
| 巨型 `XxxManagementService` | 按域拆 `ProviderService` 等 |
| 单行 XML summary | 多行 `<summary>` |
| 跨模块引用 `Infrastructure` | 只引用 `Contracts` |

---

## 16. 参考文件（复制风格时优先打开）

| 用途 | 路径 |
|------|------|
| 模块总览 | `modules/HarborAdmin.Modules.Admin/README.md` |
| 实体 | `modules/HarborAdmin.Modules.Admin/Domain/Entities/AdminUser.cs` |
| Request | `modules/HarborAdmin.Modules.Admin/Contracts/System/Request/SaveSystemUserRequest.cs` |
| DTO | `modules/HarborAdmin.Modules.Admin/Contracts/System/Dto/SystemUserDto.cs` |
| Service | `modules/HarborAdmin.Modules.Admin/Application/Services/User/UserService.cs` |
| ServiceContext | `modules/HarborAdmin.Modules.Admin/Application/Services/Shared/SystemServiceContext.cs` |
| Controller | `modules/HarborAdmin.Modules.Admin/Controllers/System/UserController.cs` |
| Repository | `modules/HarborAdmin.Modules.Admin/Infrastructure/Repositories/FreeSqlAdminRepository.System.cs` |
| DI | `modules/HarborAdmin.Modules.Admin/AdminModuleExtensions.cs` |
| 已对齐模块示例 | `Modules.AI`、`Modules.ConfigCenter`、`Modules.International`、`Modules.Secrets` |

---

*本规范随 `Modules.Admin` 基线演进；大规模重构前先阅读模块 `README.md` 与现有同域代码。*
