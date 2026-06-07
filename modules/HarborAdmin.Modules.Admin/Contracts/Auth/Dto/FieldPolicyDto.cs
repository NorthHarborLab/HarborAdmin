namespace HarborAdmin.Modules.Admin.Contracts.Auth.Dto;

/// <summary>
/// 字段策略
/// </summary>
public sealed record FieldPolicyDto(string FeatureCode, string FieldName, bool Visible, bool Editable, bool Exportable, bool Masked);
