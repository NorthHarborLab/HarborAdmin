# HarborAdmin.Modules.ConfigCenter

配置中心管理模块：负责应用登记、草稿配置项维护、配置发布、发布快照读取，以及发布后通知独立的 ConfigCenter TCP 进程刷新缓存。

本模块运行在 `HarborAdmin.Host` 的 HTTP 管理后台内；`services/HarborAdmin.ConfigCenter` 是独立 TCP 读进程，不在本模块内承载
HTTP API。

## 职责边界

| 层次                  | 路径                                         | 职责                            |
|---------------------|--------------------------------------------|-------------------------------|
| Host                | `services/HarborAdmin.Host`                | HTTP 管道、鉴权、模块组合、TCP 通知实现覆盖    |
| ConfigCenter Module | `modules/HarborAdmin.Modules.ConfigCenter` | 管理端配置 CRUD、发布、发布快照查询、数据库访问    |
| ConfigCenter TCP    | `services/HarborAdmin.ConfigCenter`        | TCP JSON 拉取、订阅、发布通知接收、缓存刷新与广播 |
| Client SDK          | `client/HarborAdmin.ConfigCenter.Client`   | 业务服务侧 `IConfiguration` 接入与热更新 |

模块内只处理管理后台能力和发布数据写入；运行时客户端拉取、订阅与连接管理由 TCP 进程和 Client SDK 负责。

## HTTP API 路由

| 路由                                                     | 方法       | 说明                 |
|--------------------------------------------------------|----------|--------------------|
| `/api/admin/config-center/apps`                        | `GET`    | 列出已注册应用            |
| `/api/admin/config-center/apps`                        | `POST`   | 注册应用               |
| `/api/admin/config-center/apps/{appId}`                | `PUT`    | 更新应用名称与描述          |
| `/api/admin/config-center/apps/{appId}`                | `DELETE` | 删除应用及其草稿、发布记录和发布项  |
| `/api/admin/config-center/{appId}/items`               | `GET`    | 列出应用草稿配置项          |
| `/api/admin/config-center/{appId}/items`               | `POST`   | 新增草稿配置项            |
| `/api/admin/config-center/{appId}/items/{id}`          | `PUT`    | 更新草稿配置项            |
| `/api/admin/config-center/{appId}/items/{id}`          | `DELETE` | 删除草稿配置项            |
| `/api/admin/config-center/{appId}/releases`            | `GET`    | 列出发布历史             |
| `/api/admin/config-center/{appId}/publish`             | `POST`   | 发布当前草稿并通知 TCP 进程   |
| `/api/admin/config-center/{appId}/published?version=0` | `GET`    | 读取已发布快照，`0` 表示最新版本 |

删除接口返回 `ApiResult.Ok(true)`，保持前端 `requestClient` 对 `{ code, data }` 响应包的约定。

发布时会复制当前草稿配置项到不可变发布快照；发布头和发布项通过同一个 `UnitOfWorkManagerCloud` 工作单元写入，避免只生成半个快照。事务提交后才发送
TCP 通知，避免 TCP 读进程提前读取未提交数据。

## 数据模型

| 实体                  | 基类                | 表达内容                     |
|---------------------|-------------------|--------------------------|
| `ConfigApplication` | `AuditableEntity` | 配置中心应用，使用 `AppId` 作为业务标识 |
| `ConfigItem`        | `AuditableEntity` | 应用当前草稿配置项                |
| `ConfigRelease`     | `EntityBase`      | 一次发布记录，应用内 `Version` 递增  |
| `ConfigReleaseItem` | `EntityBase`      | 发布快照明细，发布时从草稿项复制         |

所有实体均通过 `[DbKey("ConfigCenterDb")]` 显式声明数据库归属。跨模块不要直接引用这些实体；其他模块若需要消费配置，应通过配置中心客户端、Contracts
或应用服务边界完成。

## 配置键与值类型

草稿项由 `Group`、`Key`、`Value`、`ValueType` 组成。最终发布快照会生成 `IConfiguration` 可消费的扁平键：

- `Group` 为空时，配置键为 `Key`。
- `Group` 非空时，配置键为 `Group:Key`。
- `ValueType` 为空时按 `string` 处理。
- `json`、`object`、`options`、`model` 会在快照中展开为冒号分隔的层级键。
- `secret` 表示 Secret 引用，发布时固定引用版本，读取 resolved 快照时才解析明文。

结构化值保存前必须是合法 JSON，避免发布后生成不可解析的层级配置。

## Secret 引用

`ConfigSecretReferenceValidator` 负责校验和规范化配置中的 Secret 标记：

- 普通配置值中允许引用 Secret，但保存时会校验引用是否存在。
- `ValueType=secret` 的值会被规范化为标准 Secret 标记。
- 发布快照中保存固定版本后的 Secret 标记。
- `GetResolvedPublishedSnapshotAsync` 只在内存中解析 Secret 明文，不把明文写回数据库。

## 模块结构

```text
Application/
  Abstractions/     # Repository 与发布通知客户端抽象
  Mappings/         # DTO 映射
  Services/         # 应用、草稿项、发布、快照、Secret 校验
Contracts/
  Application/      # 应用 DTO / Request
  Item/             # 配置项 DTO / Request
  Publish/          # 发布 DTO / Request / 快照契约
Controllers/
  Application/      # 应用管理 API
  Item/             # 草稿配置项 API
  Publish/          # 发布与发布快照 API
Domain/
  Entities/         # ConfigApplication、ConfigItem、ConfigRelease、ConfigReleaseItem
Infrastructure/
  Clients/          # TCP 发布通知与空实现
  Contexts/         # ConfigCenterDbContext
  Options/          # ConfigCenter 进程地址配置
  Repositories/     # FreeSql 仓储实现
```

## 依赖注册

Host 通过 `AddConfigCenterModule(configuration)` 注册模块：

| 生命周期      | 服务                                                                                                                                                     |
|-----------|--------------------------------------------------------------------------------------------------------------------------------------------------------|
| Singleton | `IConfigCenterDbContext`、`IConfigCenterRepository`、默认 `IConfigCenterNotifyClient`                                                                      |
| Scoped    | `ConfigCenterApplicationService`、`ConfigCenterItemService`、`ConfigCenterPublishService`、`ConfigCenterSnapshotService`、`ConfigSecretReferenceValidator` |
| Options   | `ConfigCenterServerOptions`，绑定 `ConfigCenter` 配置节                                                                                                      |

默认通知客户端是 `NoOpConfigCenterNotifyClient`。Host 需要在组合根中覆盖为 `TcpConfigCenterNotifyClient`，发布后才会通知独立
TCP 进程刷新缓存。

## 配置

模块读取 `ConfigCenter` 配置节用于定位 TCP 进程：

```json
{
  "ConfigCenter": {
    "Host": "127.0.0.1",
    "Port": 9500
  }
}
```

实体数据库归属由 `[DbKey("ConfigCenterDb")]` 和 `DbConfig:Databases` 共同决定；Host 与
`services/HarborAdmin.ConfigCenter` 必须指向同一个 `ConfigCenterDb`，否则发布后 TCP 读进程无法读取 Host 写入的快照。

## 开发注意事项

- Controller 保持薄适配，只返回 `ApiResult.Ok(...)`，失败由 Service 抛领域异常。
- 应用删除会级联清理草稿项、发布记录和发布项；调整删除逻辑时保持前端响应包契约。
- 发布快照是不可变历史，修改草稿不会影响已发布版本。
- 结构化配置展开规则需要同时兼容 `Microsoft.Extensions.Configuration` 的冒号分隔键。
- `services/HarborAdmin.ConfigCenter` 不引入 HTTP/Kestrel；协议变更需同步 TCP 进程与 Client SDK。
