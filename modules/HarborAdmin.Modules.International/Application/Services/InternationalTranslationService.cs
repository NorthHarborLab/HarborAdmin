using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Client.AI.Clients;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.International.Application.Abstractions;
using HarborAdmin.Modules.International.Domain.Entities;
using HarborAdmin.Modules.International.Contracts.Entry.Request;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化 AI 翻译服务。
/// </summary>
public sealed class InternationalTranslationService(
    IInternationalRepository repository,
    InternationalPageService pageService,
    InternationalEntryService entryService,
    InternationalCacheCoordinator cacheCoordinator,
    IAiClient aiClient)
{
    /// <summary>
    /// 请求 AI 翻译条目。
    /// </summary>
    public async Task<AiBusinessResponse> TranslateEntryAsync(
        long entryId,
        TranslateInternationalEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = await entryService.RequireEntryAsync(entryId, cancellationToken);
        var source = GetTranslationValue(entry.Translations, InternationalConstants.DefaultLocale)
                     ?? entry.Translations.FirstOrDefault()?.Value
                     ?? string.Empty;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new ValidationDomainException($"国际化条目 '{entryId}' 缺少源语言文本。");
        }

        var targetLocales = request.TargetLocales is null || request.TargetLocales.Count == 0
            ? ["en-US", "zh-HK", "zh-TW"]
            : request.TargetLocales;
        // 把 entryId 和目标语言写入 Context，便于异步回调或当前响应直接应用翻译结果。
        var response = await aiClient.InvokeAsync(new AiBusinessRequest(
            InternationalConstants.TranslateBusinessKey,
            Model: request.Model,
            PromptOverride: request.PromptOverride,
            PromptVariables: new Dictionary<string, string>
            {
                ["sourceLocale"] = InternationalConstants.DefaultLocale,
                ["targetLocales"] = string.Join(", ", targetLocales),
                ["content"] = source
            },
            KnowledgeText: request.KnowledgeText,
            KnowledgeTextMode: request.KnowledgeTextMode,
            Context: new Dictionary<string, string>
            {
                ["entryId"] = entryId.ToString(),
                ["targetLocales"] = string.Join(",", targetLocales)
            },
            CallbackName: InternationalConstants.TranslationCompletedTopic,
            Input: $"Translate the following {InternationalConstants.DefaultLocale} text to {string.Join(", ", targetLocales)} and return a JSON object whose keys are locales and values are translations.\n\n{source}"),
            cancellationToken);

        if (response.Success)
        {
            // 当前调用成功时立即尝试应用；异步回调仍可通过 subscriber 复用同一个应用入口。
            await ApplyAiTranslationAsync(response, cancellationToken);
        }

        return response;
    }

    /// <summary>
    /// 应用 AI 翻译结果。
    /// </summary>
    public async Task ApplyAiTranslationAsync(AiBusinessResponse response, CancellationToken cancellationToken = default)
    {
        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            return;
        }

        if (response.Context is null ||
            !response.Context.TryGetValue("entryId", out var entryIdText) ||
            !long.TryParse(entryIdText, out var entryId))
        {
            return;
        }

        var translations = ParseTranslationContent(response.Content)
            .Select(item => new InternationalEntryTranslation { Locale = item.Key, Value = item.Value })
            .ToList();
        if (translations.Count == 0)
        {
            return;
        }

        var entry = await entryService.RequireEntryAsync(entryId, cancellationToken);
        await repository.UpsertEntryTranslationsAsync(entryId, translations, cancellationToken);
        var page = await pageService.RequirePageAsync(entry.PageId, cancellationToken);
        await cacheCoordinator.InvalidatePageAsync(page.Id, page.FullPath, cancellationToken);
    }

    /// <summary>
    /// 获取指定 locale 的翻译文本。
    /// </summary>
    private static string? GetTranslationValue(IReadOnlyList<InternationalEntryTranslation> translations, string locale) =>
        translations.FirstOrDefault(t => string.Equals(t.Locale, locale, StringComparison.Ordinal))?.Value;

    /// <summary>
    /// 从 AI 输出中解析 locale 到译文的 JSON 字典。
    /// </summary>
    private static IReadOnlyDictionary<string, string> ParseTranslationContent(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        if (start >= 0 && end > start)
        {
            // AI 可能在 JSON 前后追加解释文本，仅截取首尾大括号之间的 JSON 对象。
            content = content[start..(end + 1)];
        }

        return JsonSerializer.Deserialize<Dictionary<string, string>>(
                   content,
                   new JsonSerializerOptions(JsonSerializerDefaults.Web))
               ?? new Dictionary<string, string>();
    }
}
