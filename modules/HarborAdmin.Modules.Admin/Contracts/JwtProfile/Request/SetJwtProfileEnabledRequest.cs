namespace HarborAdmin.Modules.Admin.Contracts.JwtProfile.Request;

/// <summary>
/// 设置 JWT Profile 启停请求。
/// </summary>
public sealed class SetJwtProfileEnabledRequest
{
    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}
