using System.Linq.Expressions;
using HarborAdmin.BuildingBlocks.Caching.Attributes;

namespace HarborAdmin.BuildingBlocks.Caching.Internal;

/// <summary>
/// 强类型缓存 Where 表达式解析器。
/// 将 <c>cache.Get().Where(x => x.Id == id &amp;&amp; x.Locale == locale)</c> 解析为 key 模板需要的字段值。
/// </summary>
internal static class ExpressionKeyParser
{
    /// <summary>
    /// 解析缓存模型等值表达式中的 key 字段值。
    /// </summary>
    public static IReadOnlyDictionary<string, object?> Parse<TModel>(Expression<Func<TModel, bool>> predicate)
    {
        // key 字段名按大小写不敏感处理，和模板属性解析规则保持一致。
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        Read(predicate.Body, values);
        return values;
    }

    /// <summary>
    /// 递归读取表达式节点并收集 key 字段。
    /// </summary>
    private static void Read(Expression expression, IDictionary<string, object?> values)
    {
        if (expression is BinaryExpression { NodeType: ExpressionType.AndAlso } and)
        {
            // 仅展开 &&，每个子表达式仍然必须是等值条件。
            Read(and.Left, values);
            Read(and.Right, values);
            return;
        }

        if (expression is not BinaryExpression { NodeType: ExpressionType.Equal } equal)
        {
            throw new NotSupportedException("Typed cache Where only supports equality expressions joined by &&.");
        }

        var (member, valueExpression) = GetMemberAndValueExpression(equal.Left, equal.Right);
        if (member.GetCustomAttributes(typeof(CacheKeyPartAttribute), true).Length == 0)
        {
            throw new NotSupportedException($"Property '{member.Name}' must declare [CacheKeyPart] before it can be used in typed cache Where.");
        }

        // valueExpression 可能是常量，也可能是闭包变量或方法结果，需要统一求值。
        values[member.Name] = Evaluate(valueExpression);
    }

    /// <summary>
    /// 从等值表达式两侧识别模型属性与待求值表达式。
    /// </summary>
    private static (System.Reflection.PropertyInfo Member, Expression ValueExpression) GetMemberAndValueExpression(Expression left, Expression right)
    {
        if (left is MemberExpression { Member: System.Reflection.PropertyInfo leftProperty })
        {
            return (leftProperty, right);
        }

        if (right is MemberExpression { Member: System.Reflection.PropertyInfo rightProperty })
        {
            return (rightProperty, left);
        }

        throw new NotSupportedException("Typed cache Where expression must compare a model property with a value.");
    }

    /// <summary>
    /// 计算表达式当前值。
    /// </summary>
    private static object? Evaluate(Expression expression)
    {
        if (expression is ConstantExpression constant)
        {
            return constant.Value;
        }

        // 编译一个极小的 lambda 处理闭包变量，例如 w => w.Id == request.Id。
        var converted = Expression.Convert(expression, typeof(object));
        return Expression.Lambda<Func<object?>>(converted).Compile().Invoke();
    }
}
