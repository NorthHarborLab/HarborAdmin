using HarborAdmin.BuildingBlocks.Abstractions.Exception;

namespace HarborAdmin.BuildingBlocks.Abstractions.Application;

/// <summary>
/// Harbor 应用服务通用能力。
/// </summary>
public abstract class HarborApplicationService
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

/// <summary>
/// Harbor 基础 CRUD 应用服务。
/// </summary>
/// <typeparam name="TDto">输出 DTO 类型。</typeparam>
/// <typeparam name="TSaveRequest">保存请求类型。</typeparam>
public abstract class HarborApplicationService<TDto, TSaveRequest> : HarborApplicationService, ICrudApplicationService<TDto, TSaveRequest>
{
    /// <inheritdoc />
    public abstract Task<IReadOnlyList<TDto>> ListAsync(CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<TDto> GetAsync(long id, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task<TDto> SaveAsync(long? id, TSaveRequest request, CancellationToken cancellationToken = default);

    /// <inheritdoc />
    public abstract Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}


