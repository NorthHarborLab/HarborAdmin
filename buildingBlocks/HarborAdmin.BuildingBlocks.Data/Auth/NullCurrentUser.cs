using HarborAdmin.BuildingBlocks.Abstractions.Auth;

namespace HarborAdmin.BuildingBlocks.Data.Auth;

/// <summary>
/// 无登录上下文时的占位用户
/// </summary>
public sealed class NullCurrentUser : ICurrentUser
{
    /// <inheritdoc />
    public long Id => 0;

    /// <inheritdoc />
    public string? UserName => null;

    /// <inheritdoc />
    public string? DisplayName => null;
}
