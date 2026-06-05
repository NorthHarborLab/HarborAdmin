using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 后台动态视图接口定义。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_view_endpoint_view", nameof(ViewCode), true)]
public sealed class AdminViewEndpoint : EntityBase
{
    /// <summary>
    /// 视图编码。
    /// </summary>
    public string ViewCode { get; set; } = string.Empty;

    /// <summary>
    /// 查询接口路径。
    /// </summary>
    public string? QueryPath { get; set; }

    /// <summary>
    /// 详情接口路径。
    /// </summary>
    public string? DetailPath { get; set; }

    /// <summary>
    /// 创建接口路径。
    /// </summary>
    public string? CreatePath { get; set; }

    /// <summary>
    /// 更新接口路径。
    /// </summary>
    public string? UpdatePath { get; set; }

    /// <summary>
    /// 删除接口路径。
    /// </summary>
    public string? DeletePath { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 创建时间（UTC）。
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// 更新时间（UTC）。
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
