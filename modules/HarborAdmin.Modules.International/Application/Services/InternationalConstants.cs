namespace HarborAdmin.Modules.International.Application.Services;

/// <summary>
/// 国际化模块常量。
/// </summary>
internal static class InternationalConstants
{
    /// <summary>
    /// 未命中指定语言时使用的默认语言。
    /// </summary>
    internal const string DefaultLocale = "zh-CN";

    /// <summary>
    /// AI 翻译业务 Key。
    /// </summary>
    internal const string TranslateBusinessKey = "international.translate";

    /// <summary>
    /// AI 翻译完成回调 Topic。
    /// </summary>
    internal const string TranslationCompletedTopic = "harbor.international.translation.completed.v1";
}
