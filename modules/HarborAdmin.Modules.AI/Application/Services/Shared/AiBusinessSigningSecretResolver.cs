using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Modules.AI.Application.Abstractions;

namespace HarborAdmin.Modules.AI.Application.Services.Shared;

/// <summary>
/// 基于 AI 业务配置与 Secret 模块的签名密钥解析器。
/// </summary>
public sealed class AiBusinessSigningSecretResolver(IAiBusinessRepository repository, IEnumerable<ISecretResolver> secretResolvers)
    : IAiBusinessSigningSecretResolver
{
    /// <inheritdoc />
    public async Task<AiBusinessSigningSecretInfo?> ResolveAsync(string businessKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(businessKey))
        {
            return null;
        }

        var business = await repository.GetBusinessByKeyAsync(businessKey.Trim(), cancellationToken);
        if (business is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(business.SigningSecretRef))
        {
            return new AiBusinessSigningSecretInfo(false, null);
        }

        foreach (var resolver in secretResolvers)
        {
            var secret = await resolver.ResolveAsync(business.SigningSecretRef, cancellationToken);
            if (!string.IsNullOrWhiteSpace(secret))
            {
                return new AiBusinessSigningSecretInfo(true, secret);
            }
        }

        var environmentSecret = Environment.GetEnvironmentVariable(business.SigningSecretRef);
        return new AiBusinessSigningSecretInfo(true, environmentSecret);
    }
}
