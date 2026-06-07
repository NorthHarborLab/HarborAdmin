namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin API 访问权限评估器。
/// </summary>
public interface IAdminApiAccessEvaluator
{
    /// <summary>
    /// 判断用户是否允许访问指定 HTTP API。
    /// </summary>
    Task<bool> CanAccessAsync(long userId, string path, string method, CancellationToken cancellationToken = default);
}
