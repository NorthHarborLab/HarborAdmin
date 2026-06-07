using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin Feature 聚合读写。
/// </summary>
public partial interface IAdminRepository
{
    /// <summary>
    /// 加载 Feature 设计态聚合（含字段、接口、动作）。
    /// </summary>
    Task<AdminFeature?> GetFeatureAggregateAsync(string featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载已启用的 Feature 运行时聚合。
    /// </summary>
    Task<AdminFeature?> GetEnabledFeatureRuntimeAsync(string featureCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 加载指定 Feature 的动作及其 API 绑定。
    /// </summary>
    Task<AdminFeatureAction?> GetFeatureActionAsync(string featureCode, string actionCode, CancellationToken cancellationToken = default);
}
