using System.Collections;
using System.Reflection;
using HarborAdmin.BuildingBlocks.Abstractions.Attributes;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// Admin 字段权限投影服务。
/// </summary>
public sealed class AdminFieldProjectionService
{
    /// <summary>
    /// 不需要递归裁剪的标量类型集合。
    /// </summary>
    private static readonly HashSet<Type> ScalarTypes =
    [
        typeof(string),
        typeof(decimal),
        typeof(DateTime),
        typeof(DateTimeOffset),
        typeof(TimeSpan),
        typeof(Guid),
    ];

    /// <summary>
    /// 按数据库字段权限裁剪返回值；字段编码直接使用字典 key 或属性名。
    /// </summary>
    public object? Project(object? value, AdminFieldPermissionSet permissions)
    {
        if (value is null || permissions.IsSuperAdmin)
        {
            return value;
        }

        return ProjectValue(value, permissions);
    }

    /// <summary>
    /// 按运行时值类型分派字段投影逻辑。
    /// </summary>
    private object? ProjectValue(object? value, AdminFieldPermissionSet permissions)
    {
        if (value is null)
        {
            return null;
        }

        var type = value.GetType();
        if (IsScalar(type))
        {
            return value;
        }

        if (value is IDictionary dictionary)
        {
            return ProjectDictionary(dictionary, permissions);
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            return ProjectEnumerable(enumerable, permissions, type);
        }

        ProjectObject(value, permissions);
        return value;
    }

    /// <summary>
    /// 按字典 key 匹配数据库字段编码并裁剪字典值。
    /// </summary>
    private Dictionary<string, object?> ProjectDictionary(IDictionary dictionary, AdminFieldPermissionSet permissions)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (DictionaryEntry entry in dictionary)
        {
            var key = Convert.ToString(entry.Key) ?? string.Empty;
            result[key] = ShouldKeepField(key) || permissions.VisibleFields.Contains(key)
                ? ProjectValue(entry.Value, permissions)
                : null;
        }

        return result;
    }

    /// <summary>
    /// 裁剪集合内每一项并尽量保持目标集合元素类型。
    /// </summary>
    private object ProjectEnumerable(IEnumerable enumerable, AdminFieldPermissionSet permissions, Type expectedType)
    {
        var elementType = ResolveElementType(expectedType) ?? typeof(object);
        var listType = typeof(List<>).MakeGenericType(elementType);
        var result = (IList)Activator.CreateInstance(listType)!;
        foreach (var item in enumerable)
        {
            result.Add(ProjectValue(item, permissions));
        }

        if (expectedType.IsArray)
        {
            var array = Array.CreateInstance(elementType, result.Count);
            result.CopyTo(array, 0);
            return array;
        }

        return result;
    }

    /// <summary>
    /// 按对象属性名匹配数据库字段编码并裁剪对象属性。
    /// </summary>
    private void ProjectObject(object value, AdminFieldPermissionSet permissions)
    {
        foreach (var property in value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            if (ShouldKeepField(property))
            {
                continue;
            }

            if (IsContainerProperty(property))
            {
                property.SetValue(value, ProjectValue(property.GetValue(value), permissions, property.PropertyType));
                continue;
            }

            if (!permissions.VisibleFields.Contains(property.Name))
            {
                property.SetValue(value, GetHiddenValue(property.PropertyType));
                continue;
            }

            if (!IsScalar(property.PropertyType))
            {
                property.SetValue(value, ProjectValue(property.GetValue(value), permissions, property.PropertyType));
            }
        }
    }

    /// <summary>
    /// 使用目标属性类型裁剪嵌套值。
    /// </summary>
    private object? ProjectValue(object? value, AdminFieldPermissionSet permissions, Type expectedType)
    {
        if (value is null)
        {
            return null;
        }

        var type = value.GetType();
        if (IsScalar(type))
        {
            return value;
        }

        if (value is IDictionary dictionary)
        {
            return ProjectDictionary(dictionary, permissions);
        }

        if (value is IEnumerable enumerable && value is not string)
        {
            return ProjectEnumerable(enumerable, permissions, expectedType);
        }

        ProjectObject(value, permissions);
        return value;
    }

    /// <summary>
    /// 判断属性是否为分页或 API 包装对象的承载属性。
    /// </summary>
    private static bool IsContainerProperty(PropertyInfo property) =>
        string.Equals(property.Name, "Items", StringComparison.OrdinalIgnoreCase)
        || string.Equals(property.Name, "Data", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 判断字典字段是否需要绕过字段权限裁剪。
    /// </summary>
    private static bool ShouldKeepField(string fieldName) =>
        string.Equals(fieldName, "Id", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 判断对象属性是否需要绕过字段权限裁剪。
    /// </summary>
    private static bool ShouldKeepField(PropertyInfo property) =>
        string.Equals(property.Name, "Id", StringComparison.OrdinalIgnoreCase)
        || property.GetCustomAttribute<FieldPermissionIgnoreAttribute>(true) is not null;

    /// <summary>
    /// 获取隐藏字段写回对象时使用的默认值。
    /// </summary>
    private static object? GetHiddenValue(Type propertyType)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        return propertyType.IsValueType && Nullable.GetUnderlyingType(propertyType) is null
            ? Activator.CreateInstance(type)
            : null;
    }

    /// <summary>
    /// 解析集合类型的元素类型。
    /// </summary>
    private static Type? ResolveElementType(Type type)
    {
        if (type.IsArray)
        {
            return type.GetElementType();
        }

        if (type.IsGenericType && type.GetGenericArguments().Length == 1)
        {
            return type.GetGenericArguments()[0];
        }

        return type.GetInterfaces()
            .Where(item => item.IsGenericType && item.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            .Select(item => item.GetGenericArguments()[0])
            .FirstOrDefault();
    }

    /// <summary>
    /// 判断类型是否为无需继续递归的标量类型。
    /// </summary>
    private static bool IsScalar(Type type)
    {
        var effectiveType = Nullable.GetUnderlyingType(type) ?? type;
        return effectiveType.IsPrimitive || effectiveType.IsEnum || ScalarTypes.Contains(effectiveType);
    }
}
