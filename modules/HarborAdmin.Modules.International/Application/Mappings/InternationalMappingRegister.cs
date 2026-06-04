using HarborAdmin.Modules.International.Contracts.Dtos;
using HarborAdmin.Modules.International.Domain.Entities;
using Mapster;

namespace HarborAdmin.Modules.International.Application.Mappings;

/// <summary>
/// 国际化模块 Mapster 映射配置。
/// </summary>
public sealed class InternationalMappingRegister : IRegister
{
    private const string DefaultLocale = "zh-CN";

    /// <inheritdoc />
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<InternationalPage, InternationalPageDto>();
        config.NewConfig<InternationalEntryTranslation, InternationalEntryTranslationDto>();
        config.NewConfig<InternationalEntry, InternationalEntryDto>()
            .Map(destination => destination.DefaultValue,
                source => source.Translations
                    .Where(translation => translation.Locale == DefaultLocale)
                    .Select(translation => translation.Value)
                    .FirstOrDefault())
            .Map(destination => destination.Translations,
                source => source.Translations.OrderBy(translation => translation.Locale))
            .Map(destination => destination.Children,
                _ => Array.Empty<InternationalEntryDto>());
    }
}
