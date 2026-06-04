using Mapster;

namespace HarborAdmin.BuildingBlocks.Mapping;

/// <summary>
/// 基于 Mapster 的 Harbor 对象映射器
/// </summary>
public sealed class HarborMapper(TypeAdapterConfig config) : IHarborMapper
{
    /// <inheritdoc />
    public TTarget Map<TTarget>(object source) =>
        source.Adapt<TTarget>(config);

    /// <inheritdoc />
    public TTarget Map<TSource, TTarget>(TSource source) =>
        source.Adapt<TSource, TTarget>(config);

    /// <inheritdoc />
    public TTarget Map<TSource, TTarget>(TSource source, TTarget target) =>
        source.Adapt(target, config);
}
