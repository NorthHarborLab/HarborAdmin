using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using HarborAdmin.Host.Infrastructure.Security;

namespace HarborAdmin.Host.Infrastructure;

/// <summary>
/// Admin 认证基础设施 DI 扩展（Host 组合根）。
/// </summary>
public static class AdminServiceExtensions
{
    /// <summary>
    /// 注册 Admin 认证安全服务。
    /// </summary>
    /// <remarks>
    /// 须在 <c>AddHarborFreeSql</c> 之前调用，确保 <see cref="ICurrentUser"/> 为 Singleton 且可被根容器解析。
    /// </remarks>
    public static IServiceCollection AddAdminSecurity(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<AdminRequestUser>();
        services.AddSingleton<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }
}
