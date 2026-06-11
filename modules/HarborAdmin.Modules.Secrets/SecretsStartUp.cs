using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Abstractions.Modules;
using HarborAdmin.BuildingBlocks.Data;
using HarborAdmin.Modules.Secrets.Application.Abstractions;
using HarborAdmin.Modules.Secrets.Application.Services;
using HarborAdmin.Modules.Secrets.Infrastructure.Contexts;
using HarborAdmin.Modules.Secrets.Infrastructure.Repositories;
using HarborAdmin.Modules.Secrets.Infrastructure.Stores;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HarborAdmin.Modules.Secrets;

/// <summary>
/// Secrets 模块启动入口。
/// </summary>
public sealed class SecretsStartUp : HarborModuleMetadataBase, IHarborModuleStartup
{
    /// <inheritdoc />
    public override string ModuleName => "Secrets";

    /// <inheritdoc />
    public override string GetDbKey() => "AdminDb";

    /// <summary>
    /// 注册 Secrets 管理模块。
    /// </summary>
    /// <param name="services">服务集合。</param>
    /// <param name="context">模块注册上下文。</param>
    public void AddModule(IServiceCollection services, HarborModuleRegistrationContext context)
    {
        services.AddHarborModuleData<ISecretsDbContext, SecretsDbContext, ISecretsRepository, FreeSqlSecretsRepository>(
            repositoryLifetime: ServiceLifetime.Scoped);
        services.TryAddScoped<SecretStore>();
        services.TryAddScoped<ISecretStore>(sp => sp.GetRequiredService<SecretStore>());
        services.TryAddScoped<ISecretResolver>(sp => sp.GetRequiredService<SecretStore>());
        services.AddScoped<SecretService>();
    }
}
