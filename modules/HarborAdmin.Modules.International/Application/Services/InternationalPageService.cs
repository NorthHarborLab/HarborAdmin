using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.BuildingBlocks.Mapping;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Contracts.Page.Dto;
using HarborAdmin.Modules.International.Contracts.Page.Request;
using HarborAdmin.Modules.International.Domain.Entities;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化页面管理服务。
/// </summary>
public sealed class InternationalPageService(
    IInternationalPageRepository pageRepository,
    IInternationalGroupRepository groupRepository,
    IInternationalVersionRepository versionRepository,
    InternationalCacheCoordinator cacheCoordinator,
    IHarborMapper mapper)
{
    /// <summary>
    /// 列出页面命名空间。
    /// </summary>
    public async Task<IReadOnlyList<InternationalPageDto>> ListPagesAsync(CancellationToken cancellationToken = default)
    {
        var pages = await pageRepository.ListPagesAsync(cancellationToken);
        return pages.Select(mapper.Map<InternationalPageDto>).ToList();
    }

    /// <summary>
    /// 列出页面分组树。
    /// </summary>
    public async Task<IReadOnlyList<InternationalGroupNodeDto>> ListPageTreeAsync(CancellationToken cancellationToken = default)
    {
        var groups = await groupRepository.ListGroupsAsync(cancellationToken);
        var pages = (await pageRepository.ListPagesAsync(cancellationToken))
            .GroupBy(page => page.GroupId)
            .ToDictionary(group => group.Key ?? 0, group => group.OrderBy(page => page.FullPath, StringComparer.Ordinal).ToList());
        var groupLookup = groups
            .GroupBy(group => group.ParentId)
            .ToDictionary(
                group => group.Key ?? 0,
                group => group.OrderBy(item => item.SortOrder).ThenBy(item => item.Path, StringComparer.Ordinal).ToList());

        return BuildGroupNodes(groupLookup, pages, 0);
    }

    /// <summary>
    /// 保存页面资源分组（创建或更新）。
    /// </summary>
    public async Task<InternationalGroupNodeDto> SaveGroupAsync(long? id, SaveInternationalGroupRequest request, CancellationToken cancellationToken = default)
    {
        var groupKey = NormalizePathSegment(request.Key, "分组键");
        var groupName = request.Name.Trim();
        if (string.IsNullOrWhiteSpace(groupName))
        {
            throw new ValidationDomainException("分组名称不能为空。");
        }

        var parent = request.ParentId is null
            ? null
            : await groupRepository.GetGroupAsync(request.ParentId.Value, cancellationToken)
              ?? throw new NotFoundDomainException($"父级分组 '{request.ParentId}' 不存在。");

        if (id is null)
        {
            var path = BuildGroupPath(parent?.Path, groupKey);
            if (await groupRepository.GetGroupByPathAsync(path, cancellationToken) is not null)
            {
                throw new ConflictDomainException($"资源分组 '{path}' 已存在。");
            }

            var created = await groupRepository.InsertGroupAsync(new InternationalGroup
            {
                ParentId = parent?.Id,
                Key = groupKey,
                Path = path,
                Name = groupName,
                SortOrder = request.SortOrder
            }, cancellationToken);
            await cacheCoordinator.InvalidateAllAsync(cancellationToken);
            return ToGroupNode(created);
        }

        var group = await groupRepository.GetGroupAsync(id.Value, cancellationToken)
            ?? throw new NotFoundDomainException($"资源分组 '{id}' 不存在。");
        if (parent?.Id == group.Id)
        {
            throw new ValidationDomainException("父级分组不能选择自身。");
        }

        var allGroups = await groupRepository.ListGroupsAsync(cancellationToken);
        var descendantIds = FindDescendantGroupIds(allGroups, group.Id);
        if (parent is not null && descendantIds.Contains(parent.Id))
        {
            throw new ValidationDomainException("父级分组不能选择当前分组的子级。");
        }

        var newPath = BuildGroupPath(parent?.Path, groupKey);
        var existing = await groupRepository.GetGroupByPathAsync(newPath, cancellationToken);
        if (existing is not null && existing.Id != group.Id)
        {
            throw new ConflictDomainException($"资源分组 '{newPath}' 已存在。");
        }

        var oldPath = group.Path;
        group.ParentId = parent?.Id;
        group.Key = groupKey;
        group.Path = newPath;
        group.Name = groupName;
        group.SortOrder = request.SortOrder;

        var changedGroups = new List<InternationalGroup> { group };
        foreach (var child in allGroups.Where(item => descendantIds.Contains(item.Id)).OrderBy(item => item.Path))
        {
            child.Path = RewritePathPrefix(child.Path, oldPath, newPath);
            changedGroups.Add(child);
        }

        var pages = await pageRepository.ListPagesAsync(cancellationToken);
        var changedPages = pages
            .Where(page => page.GroupId == group.Id || (page.GroupId is not null && descendantIds.Contains(page.GroupId.Value)))
            .ToList();
        foreach (var page in changedPages)
        {
            var pageGroup = changedGroups.First(item => item.Id == page.GroupId);
            page.FullPath = $"{pageGroup.Path}/{page.PageKey}";
        }

        await groupRepository.UpdateGroupsAsync(changedGroups, cancellationToken);
        await pageRepository.UpdatePagesAsync(changedPages, cancellationToken);
        await cacheCoordinator.InvalidateAllAsync(cancellationToken);
        return ToGroupNode(group);
    }

    /// <summary>
    /// 保存页面命名空间（创建或更新）。
    /// </summary>
    public async Task<InternationalPageDto> SavePageAsync(long? id, SaveInternationalPageRequest request, CancellationToken cancellationToken = default)
    {
        if (id is null)
        {
            return await CreatePageAsync(request, cancellationToken);
        }

        return await UpdatePageAsync(id.Value, request, cancellationToken);
    }

    /// <summary>
    /// 删除页面命名空间及其全部翻译条目。
    /// </summary>
    public async Task DeletePageAsync(long id, CancellationToken cancellationToken = default)
    {
        var page = await RequirePageAsync(id, cancellationToken);
        await pageRepository.DeletePageAsync(id, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.FullPath, cancellationToken);
    }

    /// <summary>
    /// 发布页面版本。
    /// </summary>
    public async Task<InternationalPageDto> PublishPageVersionAsync(long pageId, CancellationToken cancellationToken = default)
    {
        await versionRepository.IncreasePageVersionAsync(pageId, cancellationToken);
        var page = await RequirePageAsync(pageId, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.FullPath, cancellationToken);
        return mapper.Map<InternationalPageDto>(page);
    }

    internal async Task<InternationalPage> RequirePageAsync(long id, CancellationToken cancellationToken) =>
        await pageRepository.GetPageAsync(id, cancellationToken)
        ?? throw new NotFoundDomainException($"国际化页面 '{id}' 不存在。");

    /// <summary>
    /// 创建页面命名空间并失效全局资源缓存。
    /// </summary>
    private async Task<InternationalPageDto> CreatePageAsync(SaveInternationalPageRequest request, CancellationToken cancellationToken)
    {
        var pagePath = NormalizePagePath(request.FullPath);
        var existing = await pageRepository.GetPageByPathAsync(pagePath.FullPath, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictDomainException($"国际化页面 '{pagePath.FullPath}' 已存在。");
        }

        var group = await EnsureGroupPathAsync(pagePath.GroupSegments, cancellationToken);
        var page = new InternationalPage
        {
            GroupId = group?.Id,
            PageKey = pagePath.PageKey,
            FullPath = pagePath.FullPath,
            Version = 0,
            Name = request.Name.Trim(),
            Remark = request.Remark?.Trim()
        };

        var created = await pageRepository.InsertPageAsync(page, cancellationToken);
        // 新页面会改变全量版本和 bundle 列表，需要失效整个国际化缓存域。
        await cacheCoordinator.InvalidateAllAsync(cancellationToken);
        return mapper.Map<InternationalPageDto>(created);
    }

    /// <summary>
    /// 更新页面命名空间并处理页面 key 变更后的缓存失效。
    /// </summary>
    private async Task<InternationalPageDto> UpdatePageAsync(long id, SaveInternationalPageRequest request, CancellationToken cancellationToken)
    {
        var page = await RequirePageAsync(id, cancellationToken);
        var pagePath = NormalizePagePath(request.FullPath);
        if (!string.Equals(page.FullPath, pagePath.FullPath, StringComparison.Ordinal))
        {
            var existing = await pageRepository.GetPageByPathAsync(pagePath.FullPath, cancellationToken);
            if (existing is not null && existing.Id != page.Id)
            {
                throw new ConflictDomainException($"国际化页面 '{pagePath.FullPath}' 已存在。");
            }
        }

        var oldPath = page.FullPath;
        var group = await EnsureGroupPathAsync(pagePath.GroupSegments, cancellationToken);
        page.GroupId = group?.Id;
        page.PageKey = pagePath.PageKey;
        page.FullPath = pagePath.FullPath;
        page.Name = request.Name.Trim();
        page.Remark = request.Remark?.Trim();

        await pageRepository.UpdatePageAsync(page, cancellationToken);
        // FullPath 参与单页面 bundle 缓存 key，改名时旧路径和新路径都必须失效。
        await cacheCoordinator.InvalidatePageAsync(page.Id, oldPath, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.FullPath, cancellationToken);
        return mapper.Map<InternationalPageDto>(page);
    }

    /// <summary>
    /// 递归构建资源分组树。
    /// </summary>
    private IReadOnlyList<InternationalGroupNodeDto> BuildGroupNodes(
        IReadOnlyDictionary<long, List<InternationalGroup>> groupLookup,
        IReadOnlyDictionary<long, List<InternationalPage>> pageLookup,
        long parentId)
    {
        if (!groupLookup.TryGetValue(parentId, out var groups))
        {
            return [];
        }

        return groups
            .Select(group => new InternationalGroupNodeDto(
                group.Id,
                group.ParentId,
                group.Key,
                group.Path,
                group.Name,
                group.SortOrder,
                pageLookup.TryGetValue(group.Id, out var pages)
                    ? pages.Select(mapper.Map<InternationalPageDto>).ToList()
                    : [],
                BuildGroupNodes(groupLookup, pageLookup, group.Id)))
            .ToList();
    }

    /// <summary>
    /// 确保完整路径中的分组逐级存在。
    /// </summary>
    private async Task<InternationalGroup?> EnsureGroupPathAsync(IReadOnlyList<string> groupSegments, CancellationToken cancellationToken)
    {
        InternationalGroup? parent = null;
        for (var index = 0; index < groupSegments.Count; index++)
        {
            var path = string.Join('/', groupSegments.Take(index + 1));
            var group = await groupRepository.GetGroupByPathAsync(path, cancellationToken);
            if (group is null)
            {
                group = await groupRepository.InsertGroupAsync(new InternationalGroup
                {
                    ParentId = parent?.Id,
                    Key = groupSegments[index],
                    Path = path,
                    Name = groupSegments[index],
                    SortOrder = index
                }, cancellationToken);
            }

            parent = group;
        }

        return parent;
    }

    /// <summary>
    /// 规范化页面完整路径并拆出分组与页面键。
    /// </summary>
    private static InternationalPagePath NormalizePagePath(string value)
    {
        var segments = value
            .Trim()
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length < 2)
        {
            throw new ValidationDomainException("页面路径必须包含模块和页面，例如 international/list。");
        }

        foreach (var segment in segments)
        {
            if (segment.Contains('.') || segment.Contains(':'))
            {
                throw new ValidationDomainException("页面路径片段不能包含 '.' 或 ':'。");
            }

            if (string.Equals(segment, "i18n", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationDomainException("业务国际化路径必须使用 international，不能使用 i18n。");
            }
        }

        return new InternationalPagePath(
            string.Join('/', segments),
            segments[^1],
            segments[..^1]);
    }

    /// <summary>
    /// 规范化单个路径片段。
    /// </summary>
    private static string NormalizePathSegment(string value, string displayName)
    {
        var segment = value.Trim();
        if (string.IsNullOrWhiteSpace(segment))
        {
            throw new ValidationDomainException($"{displayName}不能为空。");
        }

        if (segment.Contains('/') || segment.Contains('\\') || segment.Contains('.') || segment.Contains(':'))
        {
            throw new ValidationDomainException($"{displayName}不能包含 '/', '\\', '.' 或 ':'。");
        }

        if (string.Equals(segment, "i18n", StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationDomainException("业务国际化路径必须使用 international，不能使用 i18n。");
        }

        return segment;
    }

    /// <summary>
    /// 拼接分组路径。
    /// </summary>
    private static string BuildGroupPath(string? parentPath, string key) =>
        string.IsNullOrWhiteSpace(parentPath) ? key : $"{parentPath}/{key}";

    /// <summary>
    /// 查找分组的全部子孙分组。
    /// </summary>
    private static HashSet<long> FindDescendantGroupIds(IReadOnlyList<InternationalGroup> groups, long parentId)
    {
        var lookup = groups.GroupBy(group => group.ParentId ?? 0).ToDictionary(group => group.Key, group => group.ToList());
        var result = new HashSet<long>();
        var stack = new Stack<long>();
        stack.Push(parentId);
        while (stack.Count > 0)
        {
            var current = stack.Pop();
            if (!lookup.TryGetValue(current, out var children))
            {
                continue;
            }

            foreach (var child in children)
            {
                if (result.Add(child.Id))
                {
                    stack.Push(child.Id);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 替换路径前缀。
    /// </summary>
    private static string RewritePathPrefix(string path, string oldPrefix, string newPrefix) =>
        string.Equals(path, oldPrefix, StringComparison.Ordinal)
            ? newPrefix
            : $"{newPrefix}{path[oldPrefix.Length..]}";

    /// <summary>
    /// 转换分组节点。
    /// </summary>
    private static InternationalGroupNodeDto ToGroupNode(InternationalGroup group) =>
        new(
            group.Id,
            group.ParentId,
            group.Key,
            group.Path,
            group.Name,
            group.SortOrder,
            [],
            []);

    /// <summary>
    /// 国际化页面路径拆分结果。
    /// </summary>
    private sealed record InternationalPagePath(string FullPath, string PageKey, IReadOnlyList<string> GroupSegments);
}
