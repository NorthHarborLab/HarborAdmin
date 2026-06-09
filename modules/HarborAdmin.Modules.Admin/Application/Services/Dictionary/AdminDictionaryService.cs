using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Application.Services.Shared;
using HarborAdmin.Modules.Admin.Contracts.Dictionary.Dto;
using HarborAdmin.Modules.Admin.Contracts.Dictionary.Request;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.Modules.Admin.Infrastructure.Caching;

namespace HarborAdmin.Modules.Admin.Application.Services.Dictionary;

/// <summary>
/// Admin 字典服务。
/// </summary>
public sealed class AdminDictionaryService(AdminServiceContext context)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 查询字典类型。
    /// </summary>
    public async Task<IReadOnlyList<AdminDictionaryDto>> ListDictionariesAsync(string? keyword, CancellationToken cancellationToken)
    {
        await EnsureBuiltInDictionariesAsync(cancellationToken);
        var normalized = keyword?.Trim();
        var query = context.Orm.Select<AdminDictionary>();
        if (!string.IsNullOrWhiteSpace(normalized))
        {
            query = query.Where(item => item.DictCode.Contains(normalized) || item.Name.Contains(normalized));
        }

        var databaseItems = await query
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.DictCode)
            .ToListAsync(cancellationToken);
        var items = databaseItems.Select(MapDictionary)
            .Concat(AdminDictionaryBuiltIns.ListDictionaries(normalized))
            .GroupBy(item => item.DictCode, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.DictCode)
            .ToArray();
        return items;
    }

    /// <summary>
    /// 新建字典类型。
    /// </summary>
    public async Task<AdminDictionaryDto> CreateDictionaryAsync(SaveAdminDictionaryRequest request, CancellationToken cancellationToken)
    {
        var dictCode = NormalizeRequired(request.DictCode, "字典编码不能为空。");
        if (await context.Orm.Select<AdminDictionary>().Where(item => item.DictCode == dictCode).AnyAsync(cancellationToken))
        {
            throw new ConflictDomainException($"Dictionary '{dictCode}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var dictionary = new AdminDictionary
        {
            DictCode = dictCode,
            CreatedAt = now,
        };
        ApplyDictionary(dictionary, request, now);
        await context.Orm.Insert(dictionary).ExecuteAffrowsAsync(cancellationToken);
        await context.InvalidateDictionaryRuntimeAsync(cancellationToken);
        return MapDictionary(dictionary);
    }

    /// <summary>
    /// 更新字典类型。
    /// </summary>
    public async Task<AdminDictionaryDto> UpdateDictionaryAsync(string dictCode, SaveAdminDictionaryRequest request, CancellationToken cancellationToken)
    {
        var normalized = NormalizeRequired(dictCode, "字典编码不能为空。");
        var dictionary = await context.Orm.Select<AdminDictionary>()
            .Where(item => item.DictCode == normalized)
            .ToOneAsync(cancellationToken)
                         ?? throw new NotFoundDomainException($"Dictionary '{normalized}' was not found.");
        ApplyDictionary(dictionary, request, DateTimeOffset.UtcNow);
        dictionary.DictCode = normalized;
        await context.Orm.Update<AdminDictionary>().SetSource(dictionary).ExecuteAffrowsAsync(cancellationToken);
        await context.InvalidateDictionaryRuntimeAsync(cancellationToken);
        return MapDictionary(dictionary);
    }

    /// <summary>
    /// 删除字典类型。
    /// </summary>
    public async Task DeleteDictionaryAsync(string dictCode, CancellationToken cancellationToken)
    {
        var normalized = NormalizeRequired(dictCode, "字典编码不能为空。");
        var dictionary = await context.Orm.Select<AdminDictionary>()
            .Where(item => item.DictCode == normalized)
            .ToOneAsync(cancellationToken)
                         ?? throw new NotFoundDomainException($"Dictionary '{normalized}' was not found.");
        await context.Orm.Delete<AdminDictionaryItem>()
            .Where(item => item.DictCode == normalized)
            .ExecuteAffrowsAsync(cancellationToken);
        await context.Orm.Delete<AdminDictionary>()
            .Where(item => item.Id == dictionary.Id)
            .ExecuteAffrowsAsync(cancellationToken);
        await context.InvalidateDictionaryRuntimeAsync(cancellationToken);
    }

    /// <summary>
    /// 查询字典项。
    /// </summary>
    public async Task<IReadOnlyList<AdminDictionaryItemDto>> ListItemsAsync(string dictCode, CancellationToken cancellationToken)
    {
        await EnsureBuiltInDictionariesAsync(cancellationToken);
        var normalized = NormalizeRequired(dictCode, "字典编码不能为空。");
        var items = await context.Orm.Select<AdminDictionaryItem>()
            .Where(item => item.DictCode == normalized)
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.ItemValue)
            .ToListAsync(cancellationToken);
        return items.Select(MapItem).ToArray();
    }

    /// <summary>
    /// 查询运行时字典选项。
    /// </summary>
    public async Task<IReadOnlyList<AdminDictionaryOptionDto>> ListOptionsAsync(string dictCode, string? dataType, CancellationToken cancellationToken)
    {
        var normalized = NormalizeRequired(dictCode, "字典编码不能为空。");
        var dictionary = await context.Orm.Select<AdminDictionary>()
            .Where(item => item.DictCode == normalized)
            .ToOneAsync(cancellationToken);
        if (dictionary is { Enabled: false })
        {
            return [];
        }

        var databaseOptions = await context.Orm.Select<AdminDictionaryItem>()
            .Where(item => item.DictCode == normalized && item.Enabled)
            .OrderBy(item => item.SortOrder)
            .OrderBy(item => item.ItemValue)
            .ToListAsync(cancellationToken);
        if (databaseOptions.Count > 0)
        {
            return databaseOptions
                .Select(item => new AdminDictionaryOptionDto(
                    item.ItemLabel,
                    ToJsonValue(item.ItemValue, dataType),
                    item.Color,
                    false))
                .ToArray();
        }

        return AdminDictionaryBuiltIns.GetOptions(normalized) ?? [];
    }

    /// <summary>
    /// 新建字典项。
    /// </summary>
    public async Task<AdminDictionaryItemDto> CreateItemAsync(string dictCode, SaveAdminDictionaryItemRequest request, CancellationToken cancellationToken)
    {
        var dictionary = await LoadDictionaryAsync(dictCode, cancellationToken);
        var itemValue = NormalizeRequired(request.ItemValue, "字典项值不能为空。");
        if (await context.Orm.Select<AdminDictionaryItem>().Where(item => item.DictCode == dictionary.DictCode && item.ItemValue == itemValue).AnyAsync(cancellationToken))
        {
            throw new ConflictDomainException($"Dictionary item '{dictionary.DictCode}.{itemValue}' already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var item = new AdminDictionaryItem
        {
            AdminDictionaryId = dictionary.Id,
            DictCode = dictionary.DictCode,
            ItemValue = itemValue,
            CreatedAt = now,
        };
        ApplyItem(item, request, now);
        await context.Orm.Insert(item).ExecuteAffrowsAsync(cancellationToken);
        await context.InvalidateDictionaryRuntimeAsync(cancellationToken);
        return MapItem(item);
    }

    /// <summary>
    /// 更新字典项。
    /// </summary>
    public async Task<AdminDictionaryItemDto> UpdateItemAsync(string dictCode, long itemId, SaveAdminDictionaryItemRequest request, CancellationToken cancellationToken)
    {
        var dictionary = await LoadDictionaryAsync(dictCode, cancellationToken);
        var item = await context.Orm.Select<AdminDictionaryItem>()
            .Where(item => item.Id == itemId && item.DictCode == dictionary.DictCode)
            .ToOneAsync(cancellationToken)
                   ?? throw new NotFoundDomainException($"Dictionary item '{itemId}' was not found.");
        var nextValue = NormalizeRequired(request.ItemValue, "字典项值不能为空。");
        if (!string.Equals(item.ItemValue, nextValue, StringComparison.OrdinalIgnoreCase)
            && await context.Orm.Select<AdminDictionaryItem>().Where(row => row.DictCode == dictionary.DictCode && row.ItemValue == nextValue).AnyAsync(cancellationToken))
        {
            throw new ConflictDomainException($"Dictionary item '{dictionary.DictCode}.{nextValue}' already exists.");
        }

        item.AdminDictionaryId = dictionary.Id;
        item.DictCode = dictionary.DictCode;
        item.ItemValue = nextValue;
        ApplyItem(item, request, DateTimeOffset.UtcNow);
        await context.Orm.Update<AdminDictionaryItem>().SetSource(item).ExecuteAffrowsAsync(cancellationToken);
        await context.InvalidateDictionaryRuntimeAsync(cancellationToken);
        return MapItem(item);
    }

    /// <summary>
    /// 删除字典项。
    /// </summary>
    public async Task DeleteItemAsync(string dictCode, long itemId, CancellationToken cancellationToken)
    {
        var normalized = NormalizeRequired(dictCode, "字典编码不能为空。");
        await context.Orm.Delete<AdminDictionaryItem>()
            .Where(item => item.Id == itemId && item.DictCode == normalized)
            .ExecuteAffrowsAsync(cancellationToken);
        await context.InvalidateDictionaryRuntimeAsync(cancellationToken);
    }

    private async Task<AdminDictionary> LoadDictionaryAsync(string dictCode, CancellationToken cancellationToken)
    {
        var normalized = NormalizeRequired(dictCode, "字典编码不能为空。");
        return await context.Orm.Select<AdminDictionary>()
            .Where(item => item.DictCode == normalized)
            .ToOneAsync(cancellationToken)
               ?? throw new NotFoundDomainException($"Dictionary '{normalized}' was not found.");
    }

    private async Task EnsureBuiltInDictionariesAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var seeded = false;
        foreach (var seed in AdminDictionaryBuiltIns.ListSeeds())
        {
            var dictionary = await context.Orm.Select<AdminDictionary>()
                .Where(item => item.DictCode == seed.DictCode)
                .ToOneAsync(cancellationToken);
            if (dictionary is null)
            {
                dictionary = new AdminDictionary
                {
                    CreatedAt = now,
                    DictCode = seed.DictCode,
                    Enabled = true,
                    Name = seed.Name,
                    Remark = "系统内置字典",
                    SortOrder = seed.SortOrder,
                    UpdatedAt = now,
                };
                await context.Orm.Insert(dictionary).ExecuteAffrowsAsync(cancellationToken);
                seeded = true;
            }

            var existingValues = (await context.Orm.Select<AdminDictionaryItem>()
                    .Where(item => item.DictCode == seed.DictCode)
                    .ToListAsync(cancellationToken))
                .Select(item => item.ItemValue)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var seedItem in seed.Items)
            {
                var itemValue = Convert.ToString(seedItem.Value, global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
                if (existingValues.Contains(itemValue))
                {
                    continue;
                }

                await context.Orm.Insert(new AdminDictionaryItem
                {
                    AdminDictionaryId = dictionary.Id,
                    Color = seedItem.Color,
                    CreatedAt = now,
                    DictCode = seed.DictCode,
                    Enabled = !seedItem.Disabled,
                    ItemLabel = seedItem.Label,
                    ItemValue = itemValue,
                    SortOrder = existingValues.Count + 1,
                    UpdatedAt = now,
                }).ExecuteAffrowsAsync(cancellationToken);
                existingValues.Add(itemValue);
                seeded = true;
            }
        }

        if (seeded)
        {
            await context.InvalidateDictionaryRuntimeAsync(cancellationToken);
        }
    }

    private static void ApplyDictionary(AdminDictionary dictionary, SaveAdminDictionaryRequest request, DateTimeOffset now)
    {
        dictionary.DictCode = NormalizeRequired(request.DictCode, "字典编码不能为空。");
        dictionary.Name = NormalizeRequired(request.Name, "字典名称不能为空。");
        dictionary.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
        dictionary.SortOrder = request.SortOrder;
        dictionary.Enabled = request.Enabled;
        dictionary.UpdatedAt = now;
    }

    private static void ApplyItem(AdminDictionaryItem item, SaveAdminDictionaryItemRequest request, DateTimeOffset now)
    {
        item.ItemLabel = NormalizeRequired(request.ItemLabel, "字典项文本不能为空。");
        item.Color = string.IsNullOrWhiteSpace(request.Color) ? null : request.Color.Trim();
        item.Remark = string.IsNullOrWhiteSpace(request.Remark) ? null : request.Remark.Trim();
        item.SortOrder = request.SortOrder;
        item.Enabled = request.Enabled;
        item.UpdatedAt = now;
    }

    private static AdminDictionaryDto MapDictionary(AdminDictionary dictionary) =>
        new(
            dictionary.Id,
            dictionary.DictCode,
            dictionary.Name,
            dictionary.Remark,
            dictionary.SortOrder,
            dictionary.Enabled,
            dictionary.CreatedAt,
            dictionary.UpdatedAt ?? dictionary.CreatedAt);

    private static AdminDictionaryItemDto MapItem(AdminDictionaryItem item) =>
        new(
            item.Id,
            item.DictCode,
            item.ItemValue,
            item.ItemLabel,
            item.Color,
            item.Remark,
            item.SortOrder,
            item.Enabled,
            item.CreatedAt,
            item.UpdatedAt ?? item.CreatedAt);

    private static string NormalizeRequired(string value, string message)
    {
        var normalized = value.Trim();
        return string.IsNullOrWhiteSpace(normalized)
            ? throw new ValidationDomainException(message)
            : normalized;
    }

    private static JsonElement ToJsonValue(string value, string? dataType)
    {
        object typedValue = dataType?.Trim().ToLowerInvariant() switch
        {
            "bool" or "boolean" => bool.TryParse(value, out var boolValue) ? boolValue : value == "1",
            "decimal" => decimal.TryParse(value, out var decimalValue) ? decimalValue : value,
            "int" or "integer" or "long" => long.TryParse(value, out var longValue) ? longValue : value,
            _ => value,
        };
        return JsonSerializer.SerializeToElement(typedValue, JsonOptions);
    }
}
