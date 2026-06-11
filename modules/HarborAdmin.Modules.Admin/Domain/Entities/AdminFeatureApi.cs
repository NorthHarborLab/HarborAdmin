using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.Admin.Domain.Entities;

/// <summary>
/// Admin 功能 API 端点
/// </summary>
[Index("ux_admin_feature_api_code", $"{nameof(FeatureCode)},{nameof(ApiCode)}", true)]
[Index("ux_admin_feature_api_endpoint", $"{nameof(Path)},{nameof(HttpMethod)}", true)]
[Index("idx_admin_feature_api_feature_id", nameof(AdminFeatureId), false)]
public sealed class AdminFeatureApi : AuditableEntity
{
    /// <summary>
    /// 功能 ID。
    /// </summary>
    public long AdminFeatureId { get; set; }

    /// <summary>
    /// 所属功能。
    /// </summary>
    [Navigate(nameof(AdminFeatureId))]
    public AdminFeature AdminFeature { get; set; } = null!;

    /// <summary>
    /// 功能编码
    /// </summary>
    public string FeatureCode { get; set; } = string.Empty;

    /// <summary>
    /// API 编码
    /// </summary>
    public string ApiCode { get; set; } = string.Empty;

    /// <summary>
    /// API 名称国际化 Key
    /// </summary>
    public string NameKey { get; set; } = string.Empty;

    /// <summary>
    /// API 名称兜底文本
    /// </summary>
    public string? NameFallback { get; set; }

    /// <summary>
    /// 接口路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// HTTP 方法
    /// </summary>
    public string HttpMethod { get; set; } = string.Empty;

    /// <summary>
    /// 是否记录接口日志
    /// </summary>
    public bool EnabledLog { get; set; }

    /// <summary>
    /// 是否记录请求参数
    /// </summary>
    public bool EnabledParams { get; set; }

    /// <summary>
    /// 是否记录响应结果
    /// </summary>
    public bool EnabledResult { get; set; }

    /// <summary>
    /// 展示顺序
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 绑定了此 API 的动作关系。
    /// </summary>
    [Navigate(nameof(AdminFeatureActionApi.AdminFeatureApiId))]
    public List<AdminFeatureActionApi> ActionApis { get; set; } = [];
}
