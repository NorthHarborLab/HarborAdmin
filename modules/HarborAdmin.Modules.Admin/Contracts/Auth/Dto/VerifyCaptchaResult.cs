namespace HarborAdmin.Modules.Admin.Contracts.Auth.Dto;

/// <summary>
/// 验证码校验结果
/// </summary>
public sealed record VerifyCaptchaResult(string CaptchaToken);
