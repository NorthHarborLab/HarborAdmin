using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;

/// <summary>
/// 生成 CRUD API 请求。
/// </summary>
public sealed record GenerateCrudApisRequest(
    [property: Required(ErrorMessage = "基础 URL 不能为空。")]
    [property: MaxLength(300)]
    string BaseUrl,
    bool EnabledLog,
    bool EnabledParams,
    bool EnabledResult);