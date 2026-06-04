namespace HarborAdmin.Modules.ConfigCenter.Application.Abstractions;

/// <summary>
/// 配置发布后的通知客户端抽象
/// Host 在写库成功后调用,通知 ConfigCenter 核心进程刷新缓存并向订阅方广播;
/// ConfigCenter 进程自身使用空实现,避免循环通知
/// </summary>
/// <seealso cref="HarborAdmin.Modules.ConfigCenter.Infrastructure.Clients.TcpConfigCenterNotifyClient"/>
/// <seealso cref="HarborAdmin.Modules.ConfigCenter.Infrastructure.Clients.NoOpConfigCenterNotifyClient"/>
public interface IConfigCenterNotifyClient
{
    /// <summary>
    /// 通知 ConfigCenter 服务:指定应用已产生新发布
    /// </summary>
    /// <param name="appId">应用标</param>
    /// <param name="releaseId">新写入的发布记录主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>通知完成时返回已结束的任务</returns>
    /// <exception cref="InvalidOperationException">TCP 通知失败或未收到有效 <c>publishNotifyAck</c> 时抛出(由 TCP 实现抛出)</exception>
    Task NotifyPublishedAsync(string appId, long releaseId, CancellationToken cancellationToken = default);
}
