namespace HarborAdmin.Modules.Secrets.Contracts.Requests;

/// <summary>
/// 保存或轮换密钥请求。
/// </summary>
public sealed record SaveSecretRequest(string SecretRef, string DisplayName, string SecretValue);
