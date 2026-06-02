using System.Reflection;
using FreeSql;
using FreeSql.Aop;
using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Data.Auth;
using HarborAdmin.BuildingBlocks.Data.Configs;
using Yitter.IdGenerator;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// FreeSql 库注册与过滤器配置
/// </summary>
public static class DbRegistration
{
    private static int _snowflakeInitialized;

    /// <summary>
    /// 向 <see cref="HarborFreeSqlCloud"/> 注册单个数据库
    /// </summary>
    public static void RegisterDb(HarborFreeSqlCloud cloud, DbConnectionConfig dbConfig, ICurrentUser? currentUser, ushort snowflakeWorkerId)
    {
        InitializeSnowflakeIdGenerator(snowflakeWorkerId);

        cloud.Register(dbConfig.Key, () =>
        {
            var dataType = ParseDataType(dbConfig.DataType);
            var builder = new FreeSqlBuilder()
                .UseConnectionString(dataType, dbConfig.ConnectionString)
                .UseAutoSyncStructure(false);

            if (dbConfig.SlaveList is { Length: > 0 })
            {
                var slaves = dbConfig.SlaveList.Select(s => s.ConnectionString).ToArray();
                var weights = dbConfig.SlaveList.Select(s => s.Weight).ToArray();
                builder.UseSlave(slaves).UseSlaveWeight(weights);
            }

            var fsql = builder.Build();

            // 软删除是全局约束，所有实现 ISoftDelete 的实体默认只查询未删除数据。
            fsql.GlobalFilter.ApplyOnly<ISoftDelete>(FilterNames.Delete, a => a.IsDeleted == false);
            fsql.Aop.AuditValue += (_, e) =>
            {
                // EntityBase.Id 默认使用雪花 ID；实体显式声明自增时由数据库生成。
                if (e.AuditValueType is AuditValueType.Insert or AuditValueType.InsertOrUpdate &&
                    ShouldGenerateSnowflakeId(e))
                {
                    e.Value = YitIdHelper.NextId();
                }

                // 新增时统一补 CreatedAt，避免业务层重复维护审计字段。
                if (e.AuditValueType is AuditValueType.Insert or AuditValueType.InsertOrUpdate &&
                    e.Property.Name == nameof(IAuditable.CreatedAt) &&
                    e.Value is null)
                {
                    e.Value = DateTimeOffset.UtcNow;
                }

                // 更新和插入更新场景都刷新 UpdatedAt，确保写入路径上的修改时间一致。
                if (e.AuditValueType is AuditValueType.Update or AuditValueType.InsertOrUpdate &&
                    e.Property.Name == nameof(IAuditable.UpdatedAt))
                {
                    e.Value = DateTimeOffset.UtcNow;
                }
            };
            fsql.Aop.ConfigEntityProperty += (_, e) =>
            {
                // EntityBase.Id 统一标记为主键；默认非自增，插入时由雪花 ID 填充。
                if (IsEntityBaseId(e.EntityType, e.Property))
                {
                    e.ModifyResult.IsPrimary = true;
                    e.ModifyResult.IsIdentity = HasIdentityColumn(e.Property);
                    return;
                }

                if (dataType != DataType.PostgreSQL)
                {
                    return;
                }

                var propertyType = Nullable.GetUnderlyingType(e.Property.PropertyType) ?? e.Property.PropertyType;
                if (propertyType != typeof(DateTimeOffset))
                {
                    return;
                }

                // PostgreSQL 需要显式映射 DateTimeOffset，否则不同驱动版本可能生成不一致的列类型。
                e.ModifyResult.MapType = typeof(DateTime);
                e.ModifyResult.DbType = "timestamp with time zone";
            };

            if (dbConfig.SyncStructure)
            {
                // SyncStructure 由 AddHarborFreeSql 在注册后按实体类型调用
            }

            return fsql;
        });
    }

    /// <summary>
    /// 同步指定实体表结构。
    /// </summary>
    public static void SyncStructure(IFreeSql fsql, params Type[] entityTypes)
    {
        if (entityTypes.Length == 0)
        {
            return;
        }

        fsql.CodeFirst.SyncStructure(entityTypes);
    }

    /// <summary>
    /// 将配置文件中的数据库类型字符串转换为 FreeSql 使用的 <see cref="DataType"/>。
    /// </summary>
    private static DataType ParseDataType(string dataType) =>
        dataType.Trim().ToLowerInvariant() switch
        {
            "sqlite" => DataType.Sqlite,
            "postgresql" or "postgres" => DataType.PostgreSQL,
            "sqlserver" or "mssql" => DataType.SqlServer,
            "mysql" => DataType.MySql,
            _ => throw new NotSupportedException($"Database DataType '{dataType}' is not supported.")
        };

    /// <summary>
    /// 初始化雪花 ID 生成器；Yitter 使用全局静态生成器，因此进程内只设置一次。
    /// </summary>
    private static void InitializeSnowflakeIdGenerator(ushort workerId)
    {
        if (Interlocked.CompareExchange(ref _snowflakeInitialized, 1, 0) == 0)
        {
            YitIdHelper.SetIdGenerator(new IdGeneratorOptions(workerId));
        }
    }

    /// <summary>
    /// 判断当前审计字段是否需要在插入时补雪花 ID。
    /// </summary>
    private static bool ShouldGenerateSnowflakeId(AuditValueEventArgs e)
    {
        if (e.Column.CsType != typeof(long) || !IsEntityBaseId(e.Property.DeclaringType, e.Property))
        {
            return false;
        }

        if (HasIdentityColumn(e.Property))
        {
            return false;
        }

        return e.Value switch
        {
            null => true,
            long id => id == default,
            _ => false
        };
    }

    /// <summary>
    /// 判断属性是否是 <see cref="EntityBase"/> 体系中的 <c>Id</c> 主键。
    /// </summary>
    private static bool IsEntityBaseId(Type? entityType, PropertyInfo property)
    {
        var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        return entityType is not null &&
               typeof(EntityBase).IsAssignableFrom(entityType) &&
               property.Name == nameof(EntityBase.Id) &&
               propertyType == typeof(long);
    }

    /// <summary>
    /// 判断实体属性是否显式声明为数据库自增列。
    /// </summary>
    private static bool HasIdentityColumn(PropertyInfo property) =>
        property.GetCustomAttribute<ColumnAttribute>(false) is { IsIdentity: true };
}
