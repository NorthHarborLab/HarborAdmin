using FreeSql;
using FreeSql.Aop;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;
using HarborAdmin.BuildingBlocks.Data.Auth;
using HarborAdmin.BuildingBlocks.Data.Configs;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// FreeSql 库注册与过滤器配置
/// </summary>
public static class DbRegistration
{
    /// <summary>
    /// 向 <see cref="HarborFreeSqlCloud"/> 注册单个数据库
    /// </summary>
    public static void RegisterDb(HarborFreeSqlCloud cloud, DbConnectionConfig dbConfig, ICurrentUser? currentUser)
    {
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
            fsql.GlobalFilter.ApplyOnly<ISoftDelete>(FilterNames.Delete, a => a.IsDeleted == false);
            fsql.Aop.AuditValue += (_, e) =>
            {
                if (e.AuditValueType is AuditValueType.Insert or AuditValueType.InsertOrUpdate &&
                    e.Property.Name == nameof(IAuditable.CreatedAt) &&
                    e.Value is null)
                {
                    e.Value = DateTimeOffset.UtcNow;
                }

                if (e.AuditValueType is AuditValueType.Update or AuditValueType.InsertOrUpdate &&
                    e.Property.Name == nameof(IAuditable.UpdatedAt))
                {
                    e.Value = DateTimeOffset.UtcNow;
                }
            };
            fsql.Aop.ConfigEntityProperty += (_, e) =>
            {
                if (typeof(EntityBase).IsAssignableFrom(e.EntityType) &&
                    e.Property.Name == nameof(EntityBase.Id))
                {
                    e.ModifyResult.IsPrimary = true;
                    e.ModifyResult.IsIdentity = true;
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

    private static DataType ParseDataType(string dataType) =>
        dataType.Trim().ToLowerInvariant() switch
        {
            "sqlite" => DataType.Sqlite,
            "postgresql" or "postgres" => DataType.PostgreSQL,
            "sqlserver" or "mssql" => DataType.SqlServer,
            "mysql" => DataType.MySql,
            _ => throw new NotSupportedException($"Database DataType '{dataType}' is not supported.")
        };
}