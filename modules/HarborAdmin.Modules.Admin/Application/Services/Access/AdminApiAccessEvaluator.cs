using HarborAdmin.Modules.Admin.Application.Abstractions;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// Admin API 权限评估适配器。
/// </summary>
public sealed class AdminApiAccessEvaluator(ApiAuthorizationService apiAuthorizationService) : IAdminApiAccessEvaluator
{
    /// <inheritdoc />
    public Task<bool> CanAccessAsync(long userId, string path, string method, CancellationToken cancellationToken = default) =>
        apiAuthorizationService.CanAccessApiAsync(userId, path, method, cancellationToken);
}
