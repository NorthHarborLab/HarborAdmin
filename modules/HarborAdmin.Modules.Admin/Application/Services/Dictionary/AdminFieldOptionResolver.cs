using System.Text.Json;
using HarborAdmin.Modules.Admin.Contracts.Access.Dto;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Dto;
using HarborAdmin.Modules.Admin.Infrastructure.Caching;

namespace HarborAdmin.Modules.Admin.Application.Services.Dictionary;

/// <summary>
/// Admin 字段选项解析器。
/// </summary>
public sealed class AdminFieldOptionResolver(AdminDictionaryService dictionaryService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// 解析动态字段选项。
    /// </summary>
    public async Task<IReadOnlyList<DynamicFieldOptionDto>?> ResolveDynamicOptionsAsync(
        FeatureFieldCacheItem field,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(field.DictCode))
        {
            var options = await dictionaryService.ListOptionsAsync(field.DictCode, field.DataType.ToString(), cancellationToken);
            return options.Select(item => new DynamicFieldOptionDto(item.Label, item.Value, item.Color, item.Disabled)).ToArray();
        }

        return ParseOptions<DynamicFieldOptionDto>(field.OptionsJson);
    }

    /// <summary>
    /// 解析会话字段选项。
    /// </summary>
    public async Task<IReadOnlyList<FeatureResourceFieldOptionDto>?> ResolveResourceOptionsAsync(
        FeatureFieldCacheItem field,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(field.DictCode))
        {
            var options = await dictionaryService.ListOptionsAsync(field.DictCode, field.DataType.ToString(), cancellationToken);
            return options.Select(item => new FeatureResourceFieldOptionDto(item.Label, item.Value, item.Color, item.Disabled)).ToArray();
        }

        return ParseOptions<FeatureResourceFieldOptionDto>(field.OptionsJson);
    }

    private static IReadOnlyList<TOption>? ParseOptions<TOption>(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<IReadOnlyList<TOption>>(optionsJson, JsonOptions);
    }
}
