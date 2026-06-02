namespace HarborAdmin.BuildingBlocks.Data.Auth;

/// <summary>
/// 当前操作用户（审计字段来源）
/// </summary>
public interface ICurrentUser
{
    /// <summary>
    /// 用户主键；未登录为 0
    /// </summary>
    long Id { get; }

    /// <summary>
    /// 登录名
    /// </summary>
    string? UserName { get; }

    /// <summary>
    /// 显示名
    /// </summary>
    string? DisplayName { get; }
}
