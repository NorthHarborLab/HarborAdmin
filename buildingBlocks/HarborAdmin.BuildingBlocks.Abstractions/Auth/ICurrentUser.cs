namespace HarborAdmin.BuildingBlocks.Abstractions.Auth;

/// <summary>
/// 当前操作用户上下文。
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// 用户主键；未登录或系统进程为 0。
    /// </summary>
    long Id { get; }

    /// <summary>
    /// 登录名。
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// 显示名。
    /// </summary>
    string? DisplayName { get; }
}
