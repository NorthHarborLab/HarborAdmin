namespace HarborAdmin.Modules.Admin.Infrastructure.Caching;

/// <summary>
/// Admin 访问控制缓存 tag 与 key 常量。
/// </summary>
public static class AdminAccessCacheKeys
{
    /// <summary>
    /// 全部用户访问快照 tag。
    /// </summary>
    public const string AllUsersTag = "harbor:admin:access:users";

    /// <summary>
    /// 全部角色维度缓存 tag。
    /// </summary>
    public const string AllRolesTag = "harbor:admin:access:roles";

    /// <summary>
    /// 单角色 tag 模板（子表失效用 RoleId，写入缓存时用模型 RoleId 绑定）。
    /// </summary>
    public const string RoleTagTemplate = "harbor:admin:access:role:{RoleId}";

    /// <summary>
    /// AdminRole 本体失效 tag 模板（AdminRole 仅有 Id）。
    /// </summary>
    public const string RoleIdTagTemplate = "harbor:admin:access:role:{Id}";

    /// <summary>
    /// 运行时 API/schema 元数据 tag。
    /// </summary>
    public const string RuntimeTag = "harbor:admin:access:runtime";

    /// <summary>
    /// 全局 sessionVersion 缓存 key 段。
    /// </summary>
    public const string SessionVersionId = "global";

    /// <summary>
    /// 全局 Feature API 列表缓存 key 段。
    /// </summary>
    public const string FeatureApisKey = "feature-apis";

    /// <summary>
    /// 全局 Feature Action 列表缓存 key 段。
    /// </summary>
    public const string FeatureActionsKey = "feature-actions";
}
