using HarborAdmin.BuildingBlocks.Secrets.Protection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HarborAdmin.BuildingBlocks.Secrets.DependencyInjection;

/// <summary>
/// 通用 Secret 基础设施依赖注入扩展。
/// </summary>
public static class SecretsServiceCollectionExtensions
{
    /// <summary>
    /// 注册通用 Secret 保护器。
    /// </summary>
    public static IServiceCollection AddHarborSecrets(this IServiceCollection services)
    {
        services.TryAddSingleton<ISecretProtector, AesGcmSecretProtector>();
        return services;
    }
}
