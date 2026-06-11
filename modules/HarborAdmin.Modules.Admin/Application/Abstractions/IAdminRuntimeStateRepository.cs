namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 运行时状态仓储。
/// </summary>
public interface IAdminRuntimeStateRepository
{
    /// <summary>
    /// 获取全局会话版本号。
    /// </summary>
    Task<long> GetSessionVersionValueAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 递增全局会话版本号并失效权限缓存。
    /// </summary>
    Task BumpSessionVersionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 失效字典相关运行时缓存。
    /// </summary>
    Task InvalidateDictionaryRuntimeAsync(CancellationToken cancellationToken = default);
}
