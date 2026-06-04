namespace HarborAdmin.Modules.Secrets.Contracts.Requests;

/// <summary>
/// 设置密钥启停请求。
/// </summary>
public sealed record SetSecretEnabledRequest(string SecretRef, bool Enabled);
