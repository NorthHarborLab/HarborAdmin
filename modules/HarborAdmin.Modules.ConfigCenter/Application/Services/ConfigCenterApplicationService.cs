using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.ConfigCenter.Contracts.Application.Dto;
using HarborAdmin.Modules.ConfigCenter.Contracts.Application.Request;

namespace HarborAdmin.Modules.ConfigCenter.Application.Services;

/// <summary>
/// 配置中心应用管理服务。
/// </summary>
public sealed class ConfigCenterApplicationService(IConfigCenterRepository repository, IHarborMapper mapper)
{
    /// <summary>
    /// 列出所有应用。
    /// </summary>
    public async Task<IReadOnlyList<ConfigApplicationDto>> ListApplicationsAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListApplicationsAsync(cancellationToken))
        .Select(mapper.Map<ConfigApplicationDto>)
        .ToList();

    /// <summary>
    /// 保存应用（创建或更新）。
    /// </summary>
    public async Task<ConfigApplicationDto> SaveApplicationAsync(string? appId, SaveConfigApplicationRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(appId))
        {
            return await CreateApplicationAsync(request, cancellationToken);
        }

        return await UpdateApplicationAsync(appId, request, cancellationToken);
    }

    /// <summary>
    /// 删除应用及其全部配置数据。
    /// </summary>
    public async Task DeleteApplicationAsync(string appId, CancellationToken cancellationToken = default)
    {
        _ = await RequireApplicationAsync(appId, cancellationToken);
        await repository.DeleteApplicationAsync(appId.Trim(), cancellationToken);
    }

    internal async Task<ConfigApplication> RequireApplicationAsync(string appId, CancellationToken cancellationToken) =>
        await repository.GetApplicationByAppIdAsync(appId.Trim(), cancellationToken)
        ?? throw new NotFoundDomainException($"应用 '{appId}' 不存在。");

    /// <summary>
    /// 创建配置中心应用。
    /// </summary>
    private async Task<ConfigApplicationDto> CreateApplicationAsync(SaveConfigApplicationRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.AppId))
        {
            throw new ValidationDomainException("AppId 不能为空。");
        }

        var appId = request.AppId.Trim();
        var existing = await repository.GetApplicationByAppIdAsync(appId, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictDomainException($"应用 '{appId}' 已存在。");
        }

        var entity = new ConfigApplication
        {
            AppId = appId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim()
        };

        var created = await repository.InsertApplicationAsync(entity, cancellationToken);
        return mapper.Map<ConfigApplicationDto>(created);
    }

    /// <summary>
    /// 更新配置中心应用基础信息。
    /// </summary>
    private async Task<ConfigApplicationDto> UpdateApplicationAsync(string appId, SaveConfigApplicationRequest request, CancellationToken cancellationToken)
    {
        var entity = await RequireApplicationAsync(appId, cancellationToken);
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        await repository.UpdateApplicationAsync(entity, cancellationToken);
        return mapper.Map<ConfigApplicationDto>(entity);
    }
}
