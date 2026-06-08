namespace HarborAdmin.Modules.Admin.Contracts.System.Dto;

/// <summary>
/// 系统角色字段策略。
/// </summary>
public sealed record SystemRoleFieldPolicyDto(
    string FeatureCode,
    string FieldName,
    bool Visible,
    bool Editable,
    bool Exportable,
    bool Masked);
