# HarborAdmin.Modules.Secrets

通用密钥管理模块：负责 Secret 元数据管理、密钥轮换、启用禁用、历史版本保存，以及为其他模块提供 `ISecretStore` /
`ISecretResolver` 解析能力。

本模块用于保存数据库连接密码、第三方 API Key、AI Provider Key 等敏感配置。HTTP 管理接口只接收明文写入，不返回明文；运行时解析明文通过
`ISecretResolver` 完成。

## 职责边界

| 层次          | 路径                                       | 职责                                            |
|-------------|------------------------------------------|-----------------------------------------------|
| Controller  | `Controllers/Secret/SecretController.cs` | 管理端 Secret 列表、保存/轮换、启用禁用                      |
| Application | `Application/Services/SecretService.cs`  | SecretRef 校验、明文保护、业务编排                        |
| Store       | `Infrastructure/Stores/SecretStore.cs`   | 面向其他模块的 `ISecretStore` / `ISecretResolver` 实现 |
| Repository  | `Infrastructure/Repositories/`           | FreeSql 持久化、轮换事务、历史版本查询                       |
| Domain      | `Domain/Entities/`                       | 当前 Secret 元数据与历史版本密文                          |

其他模块不要直接引用 Secrets 模块的 Domain 或 Infrastructure。需要解析 Secret 时依赖 `ISecretResolver`；需要保存或轮换
Secret 时依赖 `ISecretStore` 或通过管理端 API。

## HTTP API 路由

| 路由                           | 方法     | 说明                 |
|------------------------------|--------|--------------------|
| `/api/admin/secrets`         | `GET`  | 列出 Secret 描述，不返回明文 |
| `/api/admin/secrets`         | `POST` | 保存或轮换 Secret       |
| `/api/admin/secrets/enabled` | `PUT`  | 启用或禁用 Secret       |

所有接口返回 `ApiResult.Ok(...)`。DTO 只包含 `SecretRef`、显示名称、版本、启用状态、是否已配置密文和时间信息。

## 数据模型

| 实体                    | 基类                | 表达内容                |
|-----------------------|-------------------|---------------------|
| `HarborSecret`        | `AuditableEntity` | Secret 当前元数据与当前版本密文 |
| `HarborSecretVersion` | `EntityBase`      | Secret 历史版本密文       |

`HarborSecret.SecretRef` 全局唯一，`HarborSecretVersion` 通过 `SecretRef + Version` 唯一约束保存历史版本。实体当前归属
`AdminDb`，这是后台管理能力的一部分。

## SecretRef 规则

`SecretRef` 是跨模块引用 Secret 的稳定业务键，允许字符为：

- 英文字母
- 数字
- `.`
- `_`
- `:`
- `-`

示例：

```text
db:harbor:password
ai:openai:api-key
config:center:token
```

配置值中可以通过 Secret 引用标记间接引用这些 Secret；ConfigCenter 发布快照会固定引用版本，读取 resolved 快照时再解析明文。



每次保存都会生成新版本，并把 `HarborSecret` 更新为当前版本。`SaveIfChangedAsync` 用于基础设施复用场景：明文未变化时只更新显示名和启用状态，不产生新版本。

## 明文与密文

- `SaveSecretRequest.SecretValue` 是唯一从 HTTP 管理端进入的明文字段。
- 写库前必须通过 `ISecretProtector.Protect` 保护。
- `SecretDto`、`SecretDescriptor`、`SecretVersionDescriptor` 都不包含明文。
- `ResolveAsync` 只在运行时按需返回明文。
- 禁用的 Secret 解析结果为 `null`。

## 模块结构

```text
Application/
  Abstractions/     # ISecretsRepository
  Mappings/         # SecretDto 映射
  Services/         # SecretService
Contracts/
  Secret/
    Dto/            # SecretDto
    Request/        # SaveSecretRequest、SetSecretEnabledRequest
Controllers/
  Secret/           # SecretController
Domain/
  Entities/         # HarborSecret、HarborSecretVersion
Infrastructure/
  Contexts/         # SecretsDbContext
  Repositories/     # FreeSqlSecretsRepository
  Stores/           # SecretStore
```

## 依赖注册

Host 或独立服务通过 `AddSecretsModule(configuration)` 注册模块：

| 生命周期            | 服务                                       |
|-----------------|------------------------------------------|
| Singleton       | `ISecretsDbContext`、`ISecretsRepository` |
| Scoped          | `SecretService`、`SecretStore`            |
| Scoped Contract | `ISecretStore`、`ISecretResolver`         |

`ISecretStore` 与 `ISecretResolver` 默认指向同一个 `SecretStore`，便于 ConfigCenter、AI、数据库配置等模块按统一方式保存和解析
Secret。

## 开发注意事项

- 不要在任何 DTO、日志或异常中输出 Secret 明文。
- 保存或轮换必须同时写入当前表和版本表，保持版本可追溯。
- 禁用 Secret 不删除密文，只阻止运行时解析。
- `SecretRef` 是跨模块契约，修改命名规则会影响已有 ConfigCenter 快照和业务配置。
- 仓储轮换逻辑必须使用同一数据库工作单元，避免历史版本已写入但当前版本未更新。
