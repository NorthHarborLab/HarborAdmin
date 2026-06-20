using System.Globalization;
using System.Text.Json;
using FreeSql;
using FreeSql.Internal.Model;
using HarborAdmin.BuildingBlocks.Abstractions.Enums;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;

namespace HarborAdmin.BuildingBlocks.Data.Extends;

/// <summary>
/// Harbor 查询选项动态查询扩展
/// </summary>
public static class HarborQueryOptionsDynamicQueryExtensions
{
    /// <summary>
    /// 按白名单应用动态筛选
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="query">查询对象</param>
    /// <param name="options">查询选项</param>
    /// <param name="fields">允许筛选字段</param>
    /// <returns>已应用筛选的查询对象</returns>
    public static ISelect<TEntity> ApplyDynamicFilters<TEntity>(this ISelect<TEntity> query, HarborQueryOptions options,
        IReadOnlyDictionary<string, PageDynamicField> fields)
        where TEntity : class
    {
        if (options.Filters is null || options.Filters.Count == 0)
        {
            return query;
        }

        var filters = new List<DynamicFilterInfo>();
        foreach (var rule in options.Filters)
        {
            if (!fields.TryGetValue(rule.Field, out var field) || !field.Allows(rule.Operator))
            {
                continue;
            }

            var filter = CreateFilter(rule, field);
            if (filter is not null)
            {
                filters.Add(filter);
            }
        }

        return filters.Count == 0
            ? query
            : query.WhereDynamicFilter(new DynamicFilterInfo
            {
                Logic = DynamicFilterLogic.And,
                Filters = filters,
            });
    }

    /// <summary>
    /// 按白名单应用动态排序
    /// </summary>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <param name="query">查询对象</param>
    /// <param name="options">查询选项</param>
    /// <param name="sortFields">允许排序字段</param>
    /// <param name="defaultSort">默认排序</param>
    /// <returns>已应用排序的查询对象</returns>
    public static ISelect<TEntity> ApplyDynamicSorting<TEntity>(this ISelect<TEntity> query, HarborQueryOptions options,
        IReadOnlyDictionary<string, string> sortFields, Func<ISelect<TEntity>, ISelect<TEntity>> defaultSort)
        where TEntity : class
    {
        var sortField = options.SortField?.Trim();
        var ascending = string.Equals(options.SortOrder, "asc", StringComparison.OrdinalIgnoreCase);
        var descending = string.Equals(options.SortOrder, "desc", StringComparison.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(sortField) || (!ascending && !descending))
        {
            return defaultSort(query);
        }

        return sortFields.TryGetValue(sortField, out var property)
            ? query.OrderByPropertyName(property, ascending)
            : defaultSort(query);
    }

    /// <summary>
    /// 创建 FreeSql 动态筛选条件
    /// </summary>
    /// <param name="rule">分页筛选条件</param>
    /// <param name="field">字段映射</param>
    /// <returns>FreeSql 动态筛选条件</returns>
    private static DynamicFilterInfo? CreateFilter(PageFilterRule rule, PageDynamicField field)
    {
        return rule.Operator switch
        {
            PageFilterOperator.Eq => CreateSingleValueFilter(rule, field, DynamicFilterOperator.Equal),
            PageFilterOperator.Contains => CreateSingleValueFilter(rule, field, DynamicFilterOperator.Contains),
            PageFilterOperator.Gte => CreateSingleValueFilter(rule, field, DynamicFilterOperator.GreaterThanOrEqual),
            PageFilterOperator.Lte => CreateSingleValueFilter(rule, field, DynamicFilterOperator.LessThanOrEqual),
            PageFilterOperator.Between => CreateBetweenFilter(rule, field),
            _ => null,
        };
    }

    /// <summary>
    /// 创建单值筛选条件
    /// </summary>
    /// <param name="rule">分页筛选条件</param>
    /// <param name="field">字段映射</param>
    /// <param name="operator">FreeSql 操作符</param>
    /// <returns>FreeSql 动态筛选条件</returns>
    private static DynamicFilterInfo? CreateSingleValueFilter(PageFilterRule rule, PageDynamicField field, DynamicFilterOperator @operator)
    {
        if (!TryConvertValue(rule.Value, field.ValueType, out var value) || IsEmpty(value))
        {
            return null;
        }

        return new DynamicFilterInfo
        {
            Field = field.Property,
            Operator = @operator,
            Value = value,
        };
    }

    /// <summary>
    /// 创建区间筛选条件
    /// </summary>
    /// <param name="rule">分页筛选条件</param>
    /// <param name="field">字段映射</param>
    /// <returns>FreeSql 动态筛选条件</returns>
    private static DynamicFilterInfo? CreateBetweenFilter(PageFilterRule rule, PageDynamicField field)
    {
        var values = rule.Values ?? ExtractJsonArray(rule.Value);
        if (values is null || values.Count < 2)
        {
            return null;
        }

        if (!TryConvertValue(values[0], field.ValueType, out var start) ||
            !TryConvertValue(values[1], field.ValueType, out var end) ||
            IsEmpty(start) ||
            IsEmpty(end))
        {
            return null;
        }

        return new DynamicFilterInfo
        {
            Field = field.Property,
            Operator = DynamicFilterOperator.Range,
            Value = new[] { start, end },
        };
    }

    /// <summary>
    /// 提取 JSON 数组
    /// </summary>
    /// <param name="value">JSON 值</param>
    /// <returns>数组值</returns>
    private static IReadOnlyList<object?>? ExtractJsonArray(object? value)
    {
        return value is JsonElement { ValueKind: JsonValueKind.Array } element
            ? element.EnumerateArray().Cast<object?>().ToArray()
            : null;
    }

    /// <summary>
    /// 转换动态筛选值类型
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="converted">转换后的值</param>
    /// <returns>是否转换成功</returns>
    private static bool TryConvertValue(object? value, Type targetType, out object? converted)
    {
        converted = null;
        var type = Nullable.GetUnderlyingType(targetType) ?? targetType;
        try
        {
            if (value is JsonElement element)
            {
                return TryConvertJsonElement(element, type, out converted);
            }

            if (value is null)
            {
                return false;
            }

            if (type.IsInstanceOfType(value))
            {
                converted = value;
                return true;
            }

            if (type.IsEnum)
            {
                converted = value is string text
                    ? Enum.Parse(type, text, true)
                    : Enum.ToObject(type, Convert.ToInt32(value, CultureInfo.InvariantCulture));
                return true;
            }

            if (type == typeof(DateTimeOffset))
            {
                converted = DateTimeOffset.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
                return true;
            }

            if (type == typeof(DateTime))
            {
                converted = DateTime.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture);
                return true;
            }

            converted = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            return true;
        }
        catch
        {
            converted = null;
            return false;
        }
    }

    /// <summary>
    /// 转换 JSON 动态筛选值类型
    /// </summary>
    /// <param name="element">JSON 值</param>
    /// <param name="targetType">目标类型</param>
    /// <param name="converted">转换后的值</param>
    /// <returns>是否转换成功</returns>
    private static bool TryConvertJsonElement(JsonElement element, Type targetType, out object? converted)
    {
        converted = null;
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                return TryConvertValue(element.GetString(), targetType, out converted);
            case JsonValueKind.Number:
                converted = targetType == typeof(long) ? element.GetInt64() : element.GetInt32();
                if (targetType.IsEnum)
                {
                    converted = Enum.ToObject(targetType, converted);
                }

                return true;
            case JsonValueKind.True:
            case JsonValueKind.False:
                converted = element.GetBoolean();
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 判断筛选值是否为空
    /// </summary>
    /// <param name="value">筛选值</param>
    /// <returns>是否为空</returns>
    private static bool IsEmpty(object? value) => value is null || value is string text && string.IsNullOrWhiteSpace(text);
}