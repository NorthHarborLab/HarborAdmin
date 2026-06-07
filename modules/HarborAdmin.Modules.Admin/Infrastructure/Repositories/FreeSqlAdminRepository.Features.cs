using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Infrastructure.Repositories;

/// <summary>
/// Admin Feature 聚合 FreeSql 实现。
/// </summary>
public sealed partial class FreeSqlAdminRepository
{
    /// <inheritdoc />
    public async Task<AdminFeature?> GetFeatureAggregateAsync(string featureCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(item => item.FeatureCode == featureCode)
            .IncludeMany(item => item.Fields)
            .IncludeMany(item => item.Apis)
            .IncludeMany(item => item.Actions, then => then.IncludeMany(action => action.ActionApis))
            .ToOneAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminFeature?> GetEnabledFeatureRuntimeAsync(string featureCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeature>()
            .Where(item => item.FeatureCode == featureCode && item.Enabled)
            .IncludeMany(item => item.Fields)
            .IncludeMany(item => item.Apis)
            .IncludeMany(item => item.Actions, then => then.IncludeMany(action => action.ActionApis))
            .FirstAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<AdminFeatureAction?> GetFeatureActionAsync(string featureCode, string actionCode, CancellationToken cancellationToken = default) =>
        await FreeSql.Select<AdminFeatureAction>()
            .Where(item => item.FeatureCode == featureCode && item.ActionCode == actionCode)
            .IncludeMany(item => item.ActionApis)
            .ToOneAsync(cancellationToken);
}
