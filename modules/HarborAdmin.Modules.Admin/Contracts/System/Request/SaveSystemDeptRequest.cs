using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.System.Request;

/// <summary>
/// 保存部门请求。
/// </summary>
public sealed class SaveSystemDeptRequest
{
    /// <summary>
    /// 父级部门 ID。
    /// </summary>
    [MaxLength(32)]
    public string? Pid { get; set; }

    /// <summary>
    /// 部门名称。
    /// </summary>
    [Required(ErrorMessage = "部门名称不能为空。")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 备注。
    /// </summary>
    [MaxLength(500)]
    public string? Remark { get; set; }

    /// <summary>
    /// 状态：1 启用，0 禁用。
    /// </summary>
    [Range(0, 1, ErrorMessage = "部门状态不合法。")]
    public int Status { get; set; } = 1;
}
