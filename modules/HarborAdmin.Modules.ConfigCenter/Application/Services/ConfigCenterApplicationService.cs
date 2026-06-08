using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.ConfigCenter.Application.Abstractions;
using HarborAdmin.Modules.ConfigCenter.Contracts.Dtos;
using HarborAdmin.Modules.ConfigCenter.Contracts.Requests;
using HarborAdmin.Modules.ConfigCenter.Domain.Entities;

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
        .Select(application => mapper.Map<ConfigApplicationDto>(application))
        .ToList();

    /// <summary>
    /// 注册新应用。
    /// </summary>
    public async Task<ConfigApplicationDto> CreateApplicationAsync(CreateConfigApplicationRequest request, CancellationToken cancellationToken = default)
    {
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
            Description = request.Description?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        var created = await repository.InsertApplicationAsync(entity, cancellationToken);
        return mapper.Map<ConfigApplicationDto>(created);
    }

    /// <summary>
    /// 更新应用元数据。
    /// </summary>
    public async Task<ConfigApplicationDto> UpdateApplicationAsync(string appId, UpdateConfigApplicationRequest request,
        CancellationToken cancellationToken = default)
    {
        var entity = await RequireApplicationAsync(appId, cancellationToken);
        entity.Name = request.Name.Trim();
        entity.Description = request.Description?.Trim();
        await repository.UpdateApplicationAsync(entity, cancellationToken);
        return mapper.Map<ConfigApplicationDto>(entity);
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
}
