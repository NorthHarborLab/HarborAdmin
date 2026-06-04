namespace HarborAdmin.BuildingBlocks.Mapping;

/// <summary>
/// Harbor 对象映射器
/// </summary>
public interface IHarborMapper
{
    /// <summary>
    /// 映射到目标类型
    /// </summary>
    /// <typeparam name="TTarget">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <returns>映射后的目标对象</returns>
    TTarget Map<TTarget>(object source);

    /// <summary>
    /// 映射到目标类型
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TTarget">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <returns>映射后的目标对象</returns>
    TTarget Map<TSource, TTarget>(TSource source);

    /// <summary>
    /// 映射到已有目标对象
    /// </summary>
    /// <typeparam name="TSource">源类型</typeparam>
    /// <typeparam name="TTarget">目标类型</typeparam>
    /// <param name="source">源对象</param>
    /// <param name="target">需要更新的目标对象</param>
    /// <returns>更新后的目标对象</returns>
    TTarget Map<TSource, TTarget>(TSource source, TTarget target);
}