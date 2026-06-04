using System.Net;

namespace HarborAdmin.AIWorker.Infrastructure;

/// <summary>
/// AI 供应商异常。
/// </summary>
public sealed class AiProviderException(HttpStatusCode statusCode, string body) : Exception(Sanitize(body))
{
    /// <summary>
    /// HTTP 状态码。
    /// </summary>
    public HttpStatusCode StatusCode { get; } = statusCode;

    /// <summary>
    /// 错误分类。
    /// </summary>
    public string Category => StatusCode switch
    {
        HttpStatusCode.Unauthorized => "Unauthorized",
        HttpStatusCode.Forbidden => "QuotaExceeded",
        HttpStatusCode.PaymentRequired => "QuotaExceeded",
        HttpStatusCode.TooManyRequests => "RateLimited",
        HttpStatusCode.BadRequest => "InvalidRequest",
        HttpStatusCode.RequestTimeout => "Timeout",
        HttpStatusCode.ServiceUnavailable => "Unavailable",
        HttpStatusCode.BadGateway => "Unavailable",
        HttpStatusCode.GatewayTimeout => "Unavailable",
        _ => "Unknown"
    };

    /// <summary>
    /// 是否限额错误。
    /// </summary>
    public bool IsQuotaError => Category is "QuotaExceeded" or "RateLimited";

    /// <summary>
    /// 是否可回退。
    /// </summary>
    public bool IsRecoverable => Category is "RateLimited" or "Timeout" or "Unavailable" or "Unknown";

    private static string Sanitize(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return "AI provider returned an error.";
        }

        var normalized = body.ReplaceLineEndings(" ").Trim();
        return normalized.Length <= 500 ? normalized : normalized[..500];
    }
}
