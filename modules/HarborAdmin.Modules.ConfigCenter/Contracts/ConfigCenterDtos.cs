namespace HarborAdmin.Modules.ConfigCenter.Contracts;

/// <summary>应用信息 DTO</summary>
/// <param name="Id">主键</param>
/// <param name="AppId">应用唯一标识</param>
/// <param name="Name">显示名称</param>
/// <param name="Description">描述</param>
/// <param name="CreatedAt">创建时间（UTC）</param>
public sealed record ConfigApplicationDto(long Id, string AppId, string Name, string? Description, DateTimeOffset CreatedAt);

/// <summary>创建应用请求</summary>
/// <param name="AppId">应用唯一标识</param>
/// <param name="Name">显示名称</param>
/// <param name="Description">描述</param>
public sealed record CreateConfigApplicationRequest(string AppId, string Name, string? Description);

/// <summary>更新应用请求</summary>
/// <param name="Name">显示名称</param>
/// <param name="Description">描述</param>
public sealed record UpdateConfigApplicationRequest(string Name, string? Description);

/// <summary>草稿配置项 DTO</summary>
/// <param name="Id">主键</param>
/// <param name="AppId">应用标识</param>
/// <param name="Environment">环境</param>
/// <param name="Group">配置根路径，非空时参与最终配置键生成</param>
/// <param name="Key">配置键名</param>
/// <param name="Value">值</param>
/// <param name="ValueType">值类型</param>
/// <param name="Remark">备注</param>
/// <param name="UpdatedAt">更新时间（UTC）</param>
public sealed record ConfigItemDto(
    long Id,
    string AppId,
    string Environment,
    string Group,
    string Key,
    string Value,
    string ValueType,
    string? Remark,
    DateTimeOffset UpdatedAt);

/// <summary>创建配置项请求</summary>
/// <param name="Group">配置根路径，非空时参与最终配置键生成</param>
/// <param name="Key">配置键名</param>
/// <param name="Value">值</param>
/// <param name="ValueType">值类型</param>
/// <param name="Remark">备注</param>
public sealed record CreateConfigItemRequest(
    string Group,
    string Key,
    string Value,
    string ValueType,
    string? Remark);

/// <summary>更新配置项请求</summary>
/// <param name="Group">配置根路径，非空时参与最终配置键生成</param>
/// <param name="Key">配置键名</param>
/// <param name="Value">值</param>
/// <param name="ValueType">值类型</param>
/// <param name="Remark">备注</param>
public sealed record UpdateConfigItemRequest(
    string Group,
    string Key,
    string Value,
    string ValueType,
    string? Remark);

/// <summary>发布记录 DTO</summary>
/// <param name="Id">发布主键</param>
/// <param name="AppId">应用标识</param>
/// <param name="Environment">环境</param>
/// <param name="Version">版本号</param>
/// <param name="PublishedBy">发布人</param>
/// <param name="PublishedAt">发布时间（UTC）</param>
public sealed record ConfigReleaseDto(
    long Id,
    string AppId,
    string Environment,
    int Version,
    string? PublishedBy,
    DateTimeOffset PublishedAt);

/// <summary>发布操作请求</summary>
/// <param name="PublishedBy">发布人(可选)</param>
public sealed record PublishConfigRequest(string? PublishedBy);

/// <summary>发布操作结果</summary>
/// <param name="ReleaseId">新发布记录主键</param>
/// <param name="Version">新版本号</param>
public sealed record PublishConfigResult(long ReleaseId, int Version);

/// <summary>已发布配置快照,供 TCP 客户端拉取</summary>
/// <param name="Version">版本号</param>
/// <param name="Data">扁平化键值对；存在 Group 时从 <c>Group:Key</c> 展开，options/json 项会继续展开为 <c>Section:Property</c> 层级键。</param>
public sealed record PublishedConfigSnapshot(int Version, IReadOnlyDictionary<string, string> Data);
