using HarborAdmin.Modules.AI.Contracts.Dtos;

namespace HarborAdmin.Modules.AI.Application.Services;

public sealed partial class AiManagementService
{
    /// <summary>
    /// 列出密钥。
    /// </summary>
    public async Task<IReadOnlyList<AiSecretDto>> ListSecretsAsync(CancellationToken cancellationToken = default) =>
        (await secretStore.ListAsync(cancellationToken))
        .Select(secret => mapper.Map<AiSecretDto>(secret))
        .ToList();

    /// <summary>
    /// 保存或轮换密钥。
    /// </summary>
    public async Task<AiSecretDto> SaveSecretAsync(SaveAiSecretRequest request, CancellationToken cancellationToken = default)
    {
        var saved = await secretStore.SaveAsync(
            NormalizeKey(request.SecretRef, nameof(request.SecretRef)),
            NormalizeRequired(request.DisplayName, nameof(request.DisplayName)),
            NormalizeRequired(request.SecretValue, nameof(request.SecretValue)),
            cancellationToken);
        return mapper.Map<AiSecretDto>(saved);
    }

    private async Task<int> ResolveSecretVersionAsync(string? secretRef, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(secretRef))
        {
            return 0;
        }

        return (await secretStore.GetAsync(secretRef.Trim(), cancellationToken))?.Version ?? 0;
    }

}
