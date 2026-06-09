using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Dto;
using HarborAdmin.Modules.Admin.Contracts.DynamicCrud.Request;

namespace HarborAdmin.Modules.Admin.Application.Services.DynamicCrud;

/// <summary>
/// 鉴权动态页验证资源处理器。
/// </summary>
public sealed class AuthDynamicTestResourceHandler : IAdminDynamicResourceHandler
{
    /// <summary>
    /// 动态资源处理器标识。
    /// </summary>
    public string HandlerKey => "auth.dynamic_test";

    /// <summary>
    /// 测试记录集合。
    /// </summary>
    private static readonly List<Dictionary<string, object?>> Records =
    [
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = "auth-dynamic-001",
            ["name"] = "动态权限样例 A",
            ["code"] = "DYN-A",
            ["status"] = 1,
            ["remark"] = "低权限用户不应看到备注",
            ["createTime"] = DateTimeOffset.UtcNow.AddDays(-2),
        },
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["id"] = "auth-dynamic-002",
            ["name"] = "动态权限样例 B",
            ["code"] = "DYN-B",
            ["status"] = 0,
            ["remark"] = "仅全量权限可见",
            ["createTime"] = DateTimeOffset.UtcNow.AddDays(-1),
        },
    ];

    /// <summary>
    /// 同步测试记录的锁对象。
    /// </summary>
    private static readonly Lock SyncRoot = new();

    /// <inheritdoc />
    public Task<DynamicQueryResultDto> QueryAsync(DynamicQueryRequest request, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            var items = Records
                .Where(record => MatchesSearch(record, request.Search))
                .Skip((Math.Max(request.Page, 1) - 1) * Math.Max(request.PageSize, 1))
                .Take(Math.Max(request.PageSize, 1))
                .Select(Clone)
                .ToArray();
            return Task.FromResult(new DynamicQueryResultDto(items, Records.Count));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, object?>?> GetAsync(string id, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            var record = Records.FirstOrDefault(item => string.Equals(Convert.ToString(item["id"]), id, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult(record is null ? null : Clone(record));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, object?>> CreateAsync(IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            var record = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase)
            {
                ["id"] = values.TryGetValue("id", out var id) && id is not null ? Convert.ToString(id) : Guid.NewGuid().ToString("N"),
                ["createTime"] = DateTimeOffset.UtcNow,
            };
            Records.Add(record);
            return Task.FromResult(Clone(record));
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyDictionary<string, object?>> UpdateAsync(string id, IReadOnlyDictionary<string, object?> values, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            var record = Records.First(item => string.Equals(Convert.ToString(item["id"]), id, StringComparison.OrdinalIgnoreCase));
            foreach (var (key, value) in values)
            {
                if (!string.Equals(key, "id", StringComparison.OrdinalIgnoreCase))
                {
                    record[key] = value;
                }
            }

            return Task.FromResult(Clone(record));
        }
    }

    /// <inheritdoc />
    public Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        lock (SyncRoot)
        {
            Records.RemoveAll(item => string.Equals(Convert.ToString(item["id"]), id, StringComparison.OrdinalIgnoreCase));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 判断记录是否匹配动态查询表单。
    /// </summary>
    private static bool MatchesSearch(IReadOnlyDictionary<string, object?> record, IReadOnlyDictionary<string, object?>? search)
    {
        if (search is null || search.Count == 0)
        {
            return true;
        }

        return search.All(pair =>
        {
            if (pair.Value is null || string.IsNullOrWhiteSpace(Convert.ToString(pair.Value)))
            {
                return true;
            }

            return record.TryGetValue(pair.Key, out var value)
                   && Convert.ToString(value)?.Contains(Convert.ToString(pair.Value)!, StringComparison.OrdinalIgnoreCase) == true;
        });
    }

    /// <summary>
    /// 克隆记录，避免调用方修改内存集合。
    /// </summary>
    private static IReadOnlyDictionary<string, object?> Clone(IReadOnlyDictionary<string, object?> record) =>
        new Dictionary<string, object?>(record, StringComparer.OrdinalIgnoreCase);
}
