namespace HarborAdmin.Modules.International.Infrastructure.Caching;

/// <summary>
/// 国际化缓存 key 与 tag 常量。
/// </summary>
public static class InternationalCacheKeys
{
    /// <summary>
    /// 全量国际化缓存失效 tag。
    /// </summary>
    public const string AllTag = "harbor:international:all";

    /// <summary>
    /// 国际化版本缓存主键。
    /// </summary>
    public const string VersionKey = "version";

    /// <summary>
    /// 国际化全量资源包缓存主键。
    /// </summary>
    public const string BundleKey = "bundle";

    /// <summary>
    /// 单页面资源包 tag 模板。
    /// </summary>
    public const string PageTagTemplate = "harbor:international:page:{PageKey}";

    /// <summary>
    /// 单页面资源包按页面主键失效 tag 模板。
    /// </summary>
    public const string PageIdTagTemplate = "harbor:international:page-id:{PageId}";

    /// <summary>
    /// 构造单页面资源包 tag。
    /// </summary>
    public static string PageTag(string pageKey) => $"harbor:international:page:{pageKey}";

    /// <summary>
    /// 构造单页面资源包按页面主键失效 tag。
    /// </summary>
    public static string PageIdTag(long pageId) => $"harbor:international:page-id:{pageId}";
}
