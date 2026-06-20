using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using FreeSql;
using FreeSql.Internal.Model;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Abstractions.Enums;
using HarborAdmin.BuildingBlocks.Abstractions.ModelResults;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories;
using HarborAdmin.BuildingBlocks.Abstractions.Repositories.Models;
using HarborAdmin.BuildingBlocks.Data.Configs;
using HarborAdmin.BuildingBlocks.Data.DbContext;

namespace HarborAdmin.BuildingBlocks.Data.Repositories;

/// <summary>
/// FreeSql 实体查询仓储基类
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TDbContext">模块数据库上下文类型</typeparam>
public abstract class FreeSqlQueryRepository<TEntity, TDbContext>
    : HarborRepository<TEntity, TDbContext>, IHarborQueryRepository<TEntity>
    where TEntity : EntityBase, new()
    where TDbContext : IHarborModuleDbContext
{
    /// <summary>
    /// 初始化实体查询仓储
    /// </summary>
    /// <param name="db">模块数据库上下文</param>
    /// <param name="entityRegistry">实体数据库映射注册表</param>
    /// <param name="unitOfWorkManager">多库工作单元管理器</param>
    protected FreeSqlQueryRepository(TDbContext db, DbEntityRegistry entityRegistry, UnitOfWorkManagerCloud unitOfWorkManager)
        : base(db, entityRegistry, unitOfWorkManager)
    {
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(HarborQueryOptions? options, CancellationToken cancellationToken)
    {
        options ??= HarborQueryOptions.Empty;
        if (IsDepartmentScopeDenied(options))
        {
            return [];
        }

        var query = FreeSql.Select<TEntity>();
        query = ApplyQueryOptions(query, options);
        return await ToProjectedListAsync(query, options.SelectedFields, cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<PagedResult<TEntity>> PageAsync(HarborQueryOptions? options, CancellationToken cancellationToken)
    {
        options ??= HarborQueryOptions.Empty;
        if (IsDepartmentScopeDenied(options))
        {
            return PagedResult<TEntity>.From([], 0);
        }

        var query = FreeSql.Select<TEntity>();
        query = ApplyQueryOptions(query, options);
        var total = await query.CountAsync(cancellationToken);
        var items = await ToProjectedListAsync(
            query.Skip(options.Skip).Limit(options.PageSize),
            options.SelectedFields,
            cancellationToken);
        return PagedResult<TEntity>.From(items, total);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await FreeSql.Select<TEntity>()
            .Where(entity => entity.Id == id)
            .FirstAsync(cancellationToken);
    }

    /// <summary>
    /// 应用查询选项
    /// </summary>
    /// <param name="query">查询对象</param>
    /// <param name="options">查询选项</param>
    /// <returns>已应用查询选项的查询对象</returns>
    protected virtual ISelect<TEntity> ApplyQueryOptions(ISelect<TEntity> query, HarborQueryOptions options)
    {
        if (options.Filters is { Count: > 0 })
        {
            var dynamicFilter = ConvertToDynamicFilterInfo(options.Filters);
            if (dynamicFilter is not null)
            {
                query = query.WhereDynamicFilter(dynamicFilter);
            }
        }

        if (TryCreateDepartmentScopePredicate(options.AllowedDepartmentIds, out var predicate))
        {
            query = query.Where(predicate);
        }

        var sortField = ResolveEntityPropertyName(options.SortField);
        return query.OrderByPropertyNameIf(
            !string.IsNullOrWhiteSpace(sortField),
            sortField,
            string.Equals(options.SortOrder, "desc", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 判断部门数据权限是否拒绝当前查询
    /// </summary>
    /// <param name="options">查询选项</param>
    /// <returns>是否拒绝查询</returns>
    private static bool IsDepartmentScopeDenied(HarborQueryOptions options) =>
        options.AllowedDepartmentIds is { Count: 0 } && HasDepartmentScopeProperty();

    /// <summary>
    /// 创建部门数据权限表达式
    /// </summary>
    /// <param name="allowedDepartmentIds">允许访问的部门 ID</param>
    /// <param name="predicate">部门过滤表达式</param>
    /// <returns>是否创建成功</returns>
    private static bool TryCreateDepartmentScopePredicate(IReadOnlySet<long>? allowedDepartmentIds, out Expression<Func<TEntity, bool>> predicate)
    {
        predicate = static _ => true;
        if (allowedDepartmentIds is null)
        {
            return false;
        }

        var property = GetDepartmentScopeProperty();
        if (property is null)
        {
            return false;
        }

        if (allowedDepartmentIds.Count == 0)
        {
            predicate = static _ => false;
            return true;
        }

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var member = Expression.Property(parameter, property);
        var ids = allowedDepartmentIds.ToArray();
        var containsMethod = typeof(Enumerable)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(method => method.Name == nameof(Enumerable.Contains) && method.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(long));

        Expression body;
        if (Nullable.GetUnderlyingType(property.PropertyType) == typeof(long))
        {
            body = Expression.AndAlso(
                Expression.Property(member, nameof(Nullable<long>.HasValue)),
                Expression.Call(containsMethod, Expression.Constant(ids), Expression.Property(member, nameof(Nullable<long>.Value))));
        }
        else if (property.PropertyType == typeof(long))
        {
            body = Expression.Call(containsMethod, Expression.Constant(ids), member);
        }
        else
        {
            predicate = static _ => false;
            return true;
        }

        predicate = Expression.Lambda<Func<TEntity, bool>>(body, parameter);
        return true;
    }

    /// <summary>
    /// 判断实体是否包含部门权限字段
    /// </summary>
    /// <returns>是否包含部门字段</returns>
    private static bool HasDepartmentScopeProperty() => GetDepartmentScopeProperty() is not null;

    /// <summary>
    /// 获取部门权限字段
    /// </summary>
    /// <returns>部门字段</returns>
    private static PropertyInfo? GetDepartmentScopeProperty() =>
        typeof(TEntity).GetProperty("DeptId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

    /// <summary>
    /// 解析实体属性名
    /// </summary>
    /// <param name="field">字段名</param>
    /// <returns>实体属性名</returns>
    private static string? ResolveEntityPropertyName(string? field)
    {
        if (string.IsNullOrWhiteSpace(field))
        {
            return null;
        }

        return typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(property => string.Equals(property.Name, field, StringComparison.OrdinalIgnoreCase))
            ?.Name;
    }

    /// <summary>
    /// 按字段权限投影查询结果
    /// </summary>
    /// <param name="query">查询对象</param>
    /// <param name="selectedFields">允许投影的字段</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>实体列表</returns>
    private static async Task<List<TEntity>> ToProjectedListAsync(ISelect<TEntity> query, IReadOnlySet<string>? selectedFields, CancellationToken cancellationToken)
    {
        var projection = CreateProjectionExpression(selectedFields);
        return projection is null
            ? await query.ToListAsync(cancellationToken)
            : await query.ToListAsync(projection, cancellationToken);
    }

    /// <summary>
    /// 创建查询投影表达式
    /// </summary>
    /// <param name="selectedFields">允许投影的字段</param>
    /// <returns>投影表达式</returns>
    private static Expression<Func<TEntity, TEntity>>? CreateProjectionExpression(IReadOnlySet<string>? selectedFields)
    {
        if (selectedFields is null)
        {
            return null;
        }

        var selected = selectedFields.ToHashSet(StringComparer.OrdinalIgnoreCase);
        selected.Add(nameof(EntityBase.Id));
        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var bindings = typeof(TEntity)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => selected.Contains(property.Name) && property is { CanRead: true, CanWrite: true })
            .Select(property => Expression.Bind(property, Expression.Property(parameter, property)))
            .Cast<MemberBinding>()
            .ToArray();

        return Expression.Lambda<Func<TEntity, TEntity>>(
            Expression.MemberInit(Expression.New(typeof(TEntity)), bindings),
            parameter);
    }

    /// <summary>
    /// 按白名单应用动态筛选
    /// </summary>
    /// <param name="filters">动态筛选条件</param>
    /// <returns>FreeSql 动态筛选条件</returns>
    public static DynamicFilterInfo? ConvertToDynamicFilterInfo(IReadOnlyList<PageFilterRule> filters)
    {
        if (filters.Count == 0)
        {
            return null;
        }

        var fieldMap = typeof(TEntity)
            .GetProperties()
            .Where(property => property.GetMethod?.IsPublic == true)
            .ToDictionary(
                property => property.Name,
                property => new DynamicFilterField(
                    property.Name,
                    Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType,
                    GetAllowedOperators(property.PropertyType)),
                StringComparer.OrdinalIgnoreCase);

        var pageDynamicFilterInfo = new List<DynamicFilterInfo>();
        foreach (var rule in filters)
        {
            if (string.IsNullOrWhiteSpace(rule.Field) ||
                !fieldMap.TryGetValue(rule.Field, out var field) ||
                !field.Allows(rule.Operator))
            {
                continue;
            }

            var filter = CreateFilter(rule, field);
            if (filter is not null)
            {
                pageDynamicFilterInfo.Add(filter);
            }
        }

        if (pageDynamicFilterInfo.Count == 0)
        {
            return null;
        }

        return new DynamicFilterInfo
        {
            Logic = DynamicFilterLogic.And,
            Filters = pageDynamicFilterInfo,
        };
    }

    /// <summary>
    /// 仓储动态筛选字段映射
    /// </summary>
    /// <param name="Property">实体属性名</param>
    /// <param name="ValueType">字段值类型</param>
    /// <param name="Operators">允许的操作符</param>
    private sealed record DynamicFilterField(string Property, Type ValueType, IReadOnlySet<PageFilterOperator> Operators)
    {
        /// <summary>
        /// 判断操作符是否允许
        /// </summary>
        /// <param name="operator">操作符</param>
        /// <returns>是否允许</returns>
        public bool Allows(PageFilterOperator @operator) => Operators.Contains(@operator);
    }

    /// <summary>
    /// 获取属性允许的动态筛选操作符
    /// </summary>
    /// <param name="propertyType">属性类型</param>
    /// <returns>允许的操作符</returns>
    private static IReadOnlySet<PageFilterOperator> GetAllowedOperators(Type propertyType)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        if (type == typeof(string))
        {
            return new HashSet<PageFilterOperator> { PageFilterOperator.Eq, PageFilterOperator.Contains };
        }

        if (type == typeof(DateTime) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(DateOnly) ||
            type == typeof(TimeOnly) ||
            IsNumber(type))
        {
            return new HashSet<PageFilterOperator>
            {
                PageFilterOperator.Eq,
                PageFilterOperator.Gte,
                PageFilterOperator.Lte,
                PageFilterOperator.Between,
            };
        }

        return new HashSet<PageFilterOperator> { PageFilterOperator.Eq };
    }

    /// <summary>
    /// 判断类型是否为数字类型
    /// </summary>
    /// <param name="type">类型</param>
    /// <returns>是否为数字类型</returns>
    private static bool IsNumber(Type type)
    {
        return type == typeof(byte) ||
               type == typeof(sbyte) ||
               type == typeof(short) ||
               type == typeof(ushort) ||
               type == typeof(int) ||
               type == typeof(uint) ||
               type == typeof(long) ||
               type == typeof(ulong) ||
               type == typeof(float) ||
               type == typeof(double) ||
               type == typeof(decimal);
    }

    /// <summary>
    /// 创建 FreeSql 动态筛选条件
    /// </summary>
    /// <param name="rule">分页筛选条件</param>
    /// <param name="field">字段映射</param>
    /// <returns>FreeSql 动态筛选条件</returns>
    private static DynamicFilterInfo? CreateFilter(PageFilterRule rule, DynamicFilterField field)
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
    private static DynamicFilterInfo? CreateSingleValueFilter(PageFilterRule rule, DynamicFilterField field, DynamicFilterOperator @operator)
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
    private static DynamicFilterInfo? CreateBetweenFilter(PageFilterRule rule, DynamicFilterField field)
    {
        var values = rule.Values ?? (rule.Value is JsonElement { ValueKind: JsonValueKind.Array } element
            ? element.EnumerateArray().Cast<object?>().ToArray()
            : null) ;


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
