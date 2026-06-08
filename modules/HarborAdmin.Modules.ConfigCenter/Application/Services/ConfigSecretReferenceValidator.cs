using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Abstractions.Secrets;
using HarborAdmin.BuildingBlocks.Secrets.References;

namespace HarborAdmin.Modules.ConfigCenter.Application.Services;

/// <summary>
/// 配置中心 Secret 引用校验器。
/// </summary>
public sealed class ConfigSecretReferenceValidator(ISecretStore secretStore)
{
    /// <summary>
    /// 规范化 Secret 类型配置值。
    /// </summary>
    public async Task<string> NormalizeSecretMarkerAsync(string value, CancellationToken cancellationToken)
    {
        var normalized = value.Trim();
        if (SecretReferenceParser.TryParseSingle(normalized, out var reference))
        {
            await RequireSecretReferenceAsync(reference, cancellationToken);
            return SecretReferenceParser.Format(reference.SecretRef, reference.Version);
        }

        if (!SecretReferenceParser.IsValidRef(normalized))
        {
            throw new ValidationDomainException("Secret 类型配置值必须是 SecretRef 或 ${secret:ref} 标记。");
        }

        var descriptor = await secretStore.GetAsync(normalized, cancellationToken);
        if (descriptor is not { Enabled: true })
        {
            throw new ValidationDomainException($"SecretRef '{normalized}' 不存在或已禁用。");
        }

        return SecretReferenceParser.Format(descriptor.SecretRef);
    }

    /// <summary>
    /// 校验配置值中的 Secret 引用。
    /// </summary>
    public async Task ValidateSecretReferencesAsync(string value, CancellationToken cancellationToken)
    {
        foreach (var reference in SecretReferenceParser.Find(value))
        {
            await RequireSecretReferenceAsync(reference, cancellationToken);
        }
    }

    /// <summary>
    /// 发布时固定 Secret 引用版本。
    /// </summary>
    public async Task<string> PinSecretReferencesAsync(string value, string valueType, CancellationToken cancellationToken)
    {
        var normalized = valueType.Trim().Equals("secret", StringComparison.OrdinalIgnoreCase)
                         && !SecretReferenceParser.TryParseSingle(value.Trim(), out _)
            ? await NormalizeSecretMarkerAsync(value, cancellationToken)
            : value;
        if (!SecretReferenceParser.Contains(normalized))
        {
            return normalized;
        }

        return await SecretReferenceParser.ReplaceAsync(normalized, async (reference, token) =>
        {
            await RequireSecretReferenceAsync(reference, token);
            if (reference.Version is { } version)
            {
                return SecretReferenceParser.Format(reference.SecretRef, version);
            }

            var descriptor = await secretStore.GetAsync(reference.SecretRef, token)
                             ?? throw new ValidationDomainException($"SecretRef '{reference.SecretRef}' 不存在。");
            return SecretReferenceParser.Format(reference.SecretRef, descriptor.Version);
        }, cancellationToken);
    }

    private async Task RequireSecretReferenceAsync(SecretReferenceToken reference, CancellationToken cancellationToken)
    {
        var descriptor = await secretStore.GetAsync(reference.SecretRef, cancellationToken);
        if (descriptor is not { Enabled: true })
        {
            throw new ValidationDomainException($"SecretRef '{reference.SecretRef}' 不存在或已禁用。");
        }

        if (reference.Version is { } version &&
            await secretStore.GetVersionAsync(reference.SecretRef, version, cancellationToken) is null)
        {
            throw new ValidationDomainException($"SecretRef '{reference.SecretRef}' 版本 {version} 不存在。");
        }
    }
}
