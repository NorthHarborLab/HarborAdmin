using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Dto;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Services.FeatureDesign;

public sealed class FeatureDesignApiService
{
    private readonly FeatureDesignServiceContext _context;

    /// <summary>
    /// 初始化功能 API 服务。
    /// </summary>
    public FeatureDesignApiService(FeatureDesignServiceContext context)
    {
        _context = context;
    }

    /// <summary>
    /// 查询功能 API。
    /// </summary>
    public async Task<IReadOnlyList<AdminFeatureApiDto>> ListApisAsync(string featureCode, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var apis = feature.Apis
            .OrderBy(item => item.SortOrder)
            .ToArray();
        return _context.Mapper.Map<AdminFeatureApiDto[]>(apis);
    }

    /// <summary>
    /// 查询全部功能 API 树。
    /// </summary>
    public async Task<IReadOnlyList<AdminFeatureApiTreeDto>> ListApiTreeAsync(CancellationToken cancellationToken)
    {
        var features = await _context.Repository.ListFeatureApiTreeAsync(cancellationToken);

        return features
            .Select(feature => new AdminFeatureApiTreeDto(
                feature.FeatureCode,
                string.IsNullOrWhiteSpace(feature.Name) ? feature.FeatureCode : feature.Name,
                feature.Apis
                    .OrderBy(api => api.SortOrder)
                    .ThenBy(api => api.ApiCode)
                    .Select(api => new AdminFeatureApiTreeItemDto(
                        api.Id,
                        api.FeatureCode,
                        api.ApiCode,
                        string.IsNullOrWhiteSpace(api.NameFallback) ? $"{api.HttpMethod} {api.Path}" : $"{api.NameFallback}（{api.HttpMethod} {api.Path}）",
                        api.Path,
                        api.HttpMethod))
                    .ToArray()))
            .Where(group => group.Apis.Count > 0)
            .ToArray();
    }

    /// <summary>
    /// 新建 API。
    /// </summary>
    public async Task<AdminFeatureApiDto> CreateApiAsync(string featureCode, SaveAdminFeatureApiRequest request, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var apiCode = request.ApiCode.Trim();
        if (feature.Apis.Any(item => string.Equals(item.ApiCode, apiCode, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ConflictDomainException($"Feature API '{normalized}.{apiCode}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var api = new AdminFeatureApi
        {
            AdminFeatureId = feature.Id,
            FeatureCode = normalized,
            ApiCode = apiCode,
            CreatedAt = now,
        };
        ApplyApi(api, request, now);
        feature.Apis.Add(api);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Apis));
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        return _context.Mapper.Map<AdminFeatureApiDto>(api);
    }

    /// <summary>
    /// 更新 API。
    /// </summary>
    public async Task<AdminFeatureApiDto> UpdateApiAsync(string featureCode, string apiCode, SaveAdminFeatureApiRequest request, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var normalizedApi = apiCode.Trim();
        var api = feature.Apis.FirstOrDefault(item => string.Equals(item.ApiCode, normalizedApi, StringComparison.OrdinalIgnoreCase))
                  ?? throw new NotFoundDomainException($"Feature API '{normalized}.{normalizedApi}' was not found.");
        api.AdminFeatureId = feature.Id;
        ApplyApi(api, request, DateTimeOffset.UtcNow);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Apis));
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        return _context.Mapper.Map<AdminFeatureApiDto>(api);
    }

    /// <summary>
    /// 排序 API。
    /// </summary>
    public async Task ReorderApisAsync(string featureCode, ReorderAdminFeatureApiRequest request, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var apis = feature.Apis
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ApiCode)
            .ToArray();
        var apiIds = apis.Select(item => item.Id).ToHashSet();
        if (apis.Length != request.OrderedIds!.Count || request.OrderedIds.Any(id => !apiIds.Contains(id)))
        {
            throw new ValidationDomainException("只能在当前功能的 API 内排序。");
        }

        var orderedIndex = request.OrderedIds
            .Select((id, index) => new { id, index })
            .ToDictionary(item => item.id, item => item.index);
        var now = DateTimeOffset.UtcNow;
        foreach (var api in apis)
        {
            api.SortOrder = (orderedIndex[api.Id] + 1) * 10;
            api.UpdatedAt = now;
        }

        await _context.Repository.UpdateFeatureApisAsync(apis, cancellationToken);
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 删除 API。
    /// </summary>
    public async Task DeleteApiAsync(string featureCode, string apiCode, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var normalizedApi = apiCode.Trim();
        var api = feature.Apis.FirstOrDefault(item => string.Equals(item.ApiCode, normalizedApi, StringComparison.OrdinalIgnoreCase))
                  ?? throw new NotFoundDomainException($"Feature API '{normalized}.{apiCode}' was not found.");
        foreach (var action in feature.Actions)
        {
            if (action.ActionApis.RemoveAll(link => link.AdminFeatureApiId == api.Id) > 0)
            {
                _context.SaveActionChildren(action, nameof(AdminFeatureAction.ActionApis));
            }
        }

        feature.Apis.Remove(api);
        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Apis));
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
    }

    /// <summary>
    /// 生成默认 CRUD API。
    /// </summary>
    public async Task<IReadOnlyList<AdminFeatureApiDto>> GenerateCrudApisAsync(string featureCode, GenerateCrudApisRequest request, CancellationToken cancellationToken)
    {
        var feature = _context.EnsureFeatureNode(await _context.LoadFeatureAggregateAsync(featureCode, cancellationToken)
                          ?? throw new NotFoundDomainException($"Feature '{featureCode}' was not found."));
        var normalized = feature.FeatureCode;
        var baseUrl = request.BaseUrl.Trim().TrimEnd('/');
        var seeds = new[]
        {
            new SaveAdminFeatureApiRequest
            {
                ApiCode = "query",
                NameKey = "feature.api.query",
                NameFallback = "查询列表",
                Path = $"{baseUrl}/query",
                HttpMethod = "POST",
                EnabledLog = request.EnabledLog,
                EnabledParams = request.EnabledParams,
                EnabledResult = request.EnabledResult,
                SortOrder = 10,
                Enabled = true,
            },
            new SaveAdminFeatureApiRequest
            {
                ApiCode = "detail",
                NameKey = "feature.api.detail",
                NameFallback = "查询详情",
                Path = $"{baseUrl}/{{id}}",
                HttpMethod = "GET",
                EnabledLog = request.EnabledLog,
                EnabledParams = request.EnabledParams,
                EnabledResult = request.EnabledResult,
                SortOrder = 20,
                Enabled = true,
            },
            new SaveAdminFeatureApiRequest
            {
                ApiCode = "create",
                NameKey = "feature.api.create",
                NameFallback = "新增",
                Path = baseUrl,
                HttpMethod = "POST",
                EnabledLog = request.EnabledLog,
                EnabledParams = request.EnabledParams,
                EnabledResult = request.EnabledResult,
                SortOrder = 30,
                Enabled = true,
            },
            new SaveAdminFeatureApiRequest
            {
                ApiCode = "update",
                NameKey = "feature.api.update",
                NameFallback = "修改",
                Path = $"{baseUrl}/{{id}}",
                HttpMethod = "PUT",
                EnabledLog = request.EnabledLog,
                EnabledParams = request.EnabledParams,
                EnabledResult = request.EnabledResult,
                SortOrder = 40,
                Enabled = true,
            },
            new SaveAdminFeatureApiRequest
            {
                ApiCode = "delete",
                NameKey = "feature.api.delete",
                NameFallback = "删除",
                Path = $"{baseUrl}/{{id}}",
                HttpMethod = "DELETE",
                EnabledLog = request.EnabledLog,
                EnabledParams = request.EnabledParams,
                EnabledResult = request.EnabledResult,
                SortOrder = 50,
                Enabled = true,
            },
        };

        foreach (var seed in seeds)
        {
            var existing = feature.Apis.FirstOrDefault(item => string.Equals(item.ApiCode, seed.ApiCode, StringComparison.OrdinalIgnoreCase));
            if (existing is null)
            {
                var api = new AdminFeatureApi
                {
                    AdminFeatureId = feature.Id,
                    FeatureCode = normalized,
                    ApiCode = seed.ApiCode.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow,
                };
                ApplyApi(api, seed, DateTimeOffset.UtcNow);
                feature.Apis.Add(api);
            }
            else
            {
                existing.AdminFeatureId = feature.Id;
                ApplyApi(existing, seed, DateTimeOffset.UtcNow);
            }
        }

        _context.SaveFeatureChildren(feature, nameof(AdminFeature.Apis));
        await _context.AdminContext.BumpSessionVersionAsync(cancellationToken);
        return await ListApisAsync(normalized, cancellationToken);
    }

    /// <summary>
    /// 将 API 配置请求归一化后写回 API 实体。
    /// </summary>
    private static void ApplyApi(AdminFeatureApi api, SaveAdminFeatureApiRequest request, DateTimeOffset now)
    {
        api.ApiCode = request.ApiCode.Trim();
        api.NameKey = request.NameKey.Trim();
        api.NameFallback = string.IsNullOrWhiteSpace(request.NameFallback) ? null : request.NameFallback.Trim();
        api.Path = request.Path.Trim();
        // HTTP 方法统一大写，便于运行期权限匹配按 method + path 做稳定比较。
        api.HttpMethod = request.HttpMethod.Trim().ToUpperInvariant();
        api.EnabledLog = request.EnabledLog;
        api.EnabledParams = request.EnabledParams;
        api.EnabledResult = request.EnabledResult;
        api.SortOrder = request.SortOrder;
        api.Enabled = request.Enabled;
        api.UpdatedAt = now;
    }
}
