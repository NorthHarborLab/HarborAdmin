using HarborAdmin.BuildingBlocks.Abstractions.Application;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;

namespace HarborAdmin.BuildingBlocks.Application;

/// <summary>
/// Harbor 应用服务通用能力。
/// </summary>
public abstract class HarborApplicationService : IHarborApplicationService
{
    /// <summary>
    /// 当前 UTC 时间。
    /// </summary>
    protected virtual DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <summary>
    /// 要求对象存在。
    /// </summary>
    /// <typeparam name="T">对象类型。</typeparam>
    /// <param name="value">对象值。</param>
    /// <param name="message">不存在时的错误消息。</param>
    /// <returns>非空对象。</returns>
    protected static T RequireFound<T>(T? value, string message) where T : class =>
        value ?? throw new NotFoundDomainException(message);
}

