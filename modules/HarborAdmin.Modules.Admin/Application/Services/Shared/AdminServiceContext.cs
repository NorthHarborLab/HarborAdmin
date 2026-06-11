using HarborAdmin.Modules.Admin.Application.Abstractions;

namespace HarborAdmin.Modules.Admin.Application.Services.Shared;

/// <summary>
/// Admin 模块共享服务上下文。
/// </summary>
public sealed class AdminServiceContext(IAdminRuntimeStateRepository runtimeStateRepository)
{
    /// <summary>
    /// 读取全局会话版本号，不存在时初始化为 1。
    /// </summary>
    public Task<long> GetSessionVersionValueAsync(CancellationToken cancellationToken) =>
        runtimeStateRepository.GetSessionVersionValueAsync(cancellationToken);

    /// <summary>
    /// 递增全局会话版本号，通知前端刷新权限与菜单。
    /// </summary>
    public Task BumpSessionVersionAsync(CancellationToken cancellationToken) =>
        runtimeStateRepository.BumpSessionVersionAsync(cancellationToken);

    /// <summary>
    /// 失效字典相关运行时缓存并通知前端刷新会话资源。
    /// </summary>
    public Task InvalidateDictionaryRuntimeAsync(CancellationToken cancellationToken) =>
        runtimeStateRepository.InvalidateDictionaryRuntimeAsync(cancellationToken);
}
