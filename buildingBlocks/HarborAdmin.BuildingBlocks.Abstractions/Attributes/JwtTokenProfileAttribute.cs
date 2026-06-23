using HarborAdmin.BuildingBlocks.Abstractions.Auth;
using Microsoft.AspNetCore.Authorization;

namespace HarborAdmin.BuildingBlocks.Abstractions.Attributes;

/// <summary>
/// 指定接口使用的 JWT Profile。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class JwtTokenProfileAttribute(string profileKey) : Attribute, IAuthorizeData
{
    /// <summary>
    /// JWT Profile Key。
    /// </summary>
    public string ProfileKey { get; } = profileKey;

    /// <inheritdoc />
    public string? Policy { get; set; }

    /// <inheritdoc />
    public string? Roles { get; set; }

    /// <inheritdoc />
    public string? AuthenticationSchemes { get; set; } =
        JwtProfileAuthenticationDefaults.AuthenticationScheme;
}
