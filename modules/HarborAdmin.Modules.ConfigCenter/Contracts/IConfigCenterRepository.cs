using HarborAdmin.Modules.ConfigCenter.Domain;

namespace HarborAdmin.Modules.ConfigCenter.Contracts;

/// <summary>
/// 配置中心持久化仓储接口
/// </summary>
/// <remarks>
/// 定义应用,草稿配置项,发布记录及发布快照的读写契约
/// 默认实现:<see cref="Infrastructure.FreeSqlConfigCenterRepository"/>
/// </remarks>
public interface IConfigCenterRepository
{
    /// <summary>
    /// 列出所有已注册的应用
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按 <see cref="ConfigApplication.AppId"/> 排序的应用列表</returns>
    Task<IReadOnlyList<ConfigApplication>> ListApplicationsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 按应用标识查询应用
    /// </summary>
    /// <param name="appId">应用唯一标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>应用实体;不存在时返回 null</returns>
    Task<ConfigApplication?> GetApplicationByAppIdAsync(string appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 插入新应用
    /// </summary>
    /// <param name="application">应用实体(插入后 <see cref="ConfigApplication.Id"/> 由实现填充)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已持久化的应用实体</returns>
    Task<ConfigApplication> InsertApplicationAsync(ConfigApplication application, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新应用元数据
    /// </summary>
    /// <param name="application">含主键的完整应用实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateApplicationAsync(ConfigApplication application, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除应用及其下所有草稿,发布记录与快照项
    /// </summary>
    /// <param name="appId">应用唯一标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteApplicationAsync(string appId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出指定应用与环境下的草稿配置项
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按分组、键名排序的配置项列表</returns>
    Task<IReadOnlyList<ConfigItem>> ListItemsAsync(string appId, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按主键查询草稿配置项
    /// </summary>
    /// <param name="id">配置项主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>配置项实体;不存在时返回 null</returns>
    Task<ConfigItem?> GetItemAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 插入草稿配置项
    /// </summary>
    /// <param name="item">配置项实体(插入后 <see cref="ConfigItem.Id"/> 由实现填充)</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已持久化的配置项</returns>
    Task<ConfigItem> InsertItemAsync(ConfigItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新草稿配置项
    /// </summary>
    /// <param name="item">含主键的完整配置项实体</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task UpdateItemAsync(ConfigItem item, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除草稿配置项
    /// </summary>
    /// <param name="id">配置项主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteItemAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出指定应用与环境下的发布历史
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>按 <see cref="ConfigRelease.Version"/> 降序排列的发布列表</returns>
    Task<IReadOnlyList<ConfigRelease>> ListReleasesAsync(string appId, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取最新一次发布记录
    /// </summary>
    /// <param name="appId">应用标识</param>
    /// <param name="environment">环境名称</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最新发布实体；从未发布时返回 <see langword="null"/></returns>
    Task<ConfigRelease?> GetLatestReleaseAsync(string appId, string environment, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按发布主键查询发布记录
    /// </summary>
    /// <param name="releaseId">发布记录主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发布实体；不存在时返回 <see langword="null"/></returns>
    Task<ConfigRelease?> GetReleaseByIdAsync(long releaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 列出某次发布下的全部快照配置项
    /// </summary>
    /// <param name="releaseId">发布记录主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>快照配置项列表</returns>
    Task<IReadOnlyList<ConfigReleaseItem>> ListReleaseItemsAsync(long releaseId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在事务内插入发布记录及其快照项
    /// </summary>
    /// <param name="release">发布头信息（插入后 <see cref="ConfigRelease.Id"/> 由实现填充）</param>
    /// <param name="items">快照项集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>已持久化的发布记录</returns>
    Task<ConfigRelease> InsertReleaseAsync(ConfigRelease release, IReadOnlyList<ConfigReleaseItem> items, CancellationToken cancellationToken = default);
}