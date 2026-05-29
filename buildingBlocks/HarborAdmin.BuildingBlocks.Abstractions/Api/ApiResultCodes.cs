namespace HarborAdmin.BuildingBlocks.Abstractions.Api;

/// <summary>与前端 Vben <c>successCode: 0</c> 对齐的 API 业务码。</summary>
public static class ApiResultCodes
{
    /// <summary>成功。</summary>
    public const int Success = 0;

    /// <summary>请求参数错误。</summary>
    public const int BadRequest = 400;

    /// <summary>未授权。</summary>
    public const int Unauthorized = 401;

    /// <summary>禁止访问。</summary>
    public const int Forbidden = 403;

    /// <summary>资源不存在。</summary>
    public const int NotFound = 404;

    /// <summary>服务器内部错误。</summary>
    public const int InternalError = 500;
}
