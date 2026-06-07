namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// API 路径模板匹配工具。
/// </summary>
internal static class AccessPathMatcher
{
    /// <summary>
    /// 判断请求路径是否匹配 API 模板（支持 <c>{param}</c> 占位符）。
    /// </summary>
    public static bool Matches(string template, string path)
    {
        var templateParts = template.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        var pathParts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (templateParts.Length != pathParts.Length)
        {
            return false;
        }

        for (var i = 0; i < templateParts.Length; i++)
        {
            if (templateParts[i].StartsWith('{') && templateParts[i].EndsWith('}'))
            {
                continue;
            }

            if (!string.Equals(templateParts[i], pathParts[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }
}
