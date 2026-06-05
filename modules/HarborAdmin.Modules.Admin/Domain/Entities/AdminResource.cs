using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 后台动态资源定义。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_admin_resource_code", nameof(ResourceCode), true)]
public sealed class AdminResource : EntityBase
{
    /// <summary>
    /// 资源编码。
    /// </summary>
    public string ResourceCode { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称国际化 Key。
    /// </summary>
    public string NameKey { get; set; } = string.Empty;

    /// <summary>
    /// 资源名称兜底文本。
    /// </summary>
    public string? NameFallback { get; set; }

    /// <summary>
    /// 所属模块名称。
    /// </summary>
    public string? ModuleName { get; set; }

    /// <summary>
    /// 资源处理器标识。
    /// </summary>
    public string? HandlerKey { get; set; }

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
