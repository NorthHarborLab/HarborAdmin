using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.JwtProfile.Request;

/// <summary>
/// 轮换 JWT Profile 密钥请求。
/// </summary>
public sealed class RotateJwtProfileSecretRequest
{
    /// <summary>
    /// 新密钥明文；为空时由服务端生成。
    /// </summary>
    [MaxLength(4096)]
    public string? SecretValue { get; set; }
}
