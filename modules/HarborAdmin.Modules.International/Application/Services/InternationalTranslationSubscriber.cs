using DotNetCore.CAP;
using HarborAdmin.Client.AI.Invocation;

namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化 AI 翻译回调订阅器。
/// </summary>
public sealed class InternationalTranslationSubscriber(InternationalService service) : ICapSubscribe
{
    /// <summary>
    /// 处理 AI 翻译完成回调。
    /// </summary>
    [CapSubscribe(InternationalService.TranslationCompletedTopic)]
    public Task HandleAsync(AiBusinessResponse response, CancellationToken cancellationToken = default) =>
        service.ApplyAiTranslationAsync(response, cancellationToken);
}

