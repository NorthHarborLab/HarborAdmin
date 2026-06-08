namespace HarborAdmin.Modules.ConfigCenter.Infrastructure.Clients;

/// <summary>
/// 空实现的通知客户端,用于 ConfigCenter 核心进程(无需自我通知)
/// </summary>
public sealed class NoOpConfigCenterNotifyClient : IConfigCenterNotifyClient
{
    /// <inheritdoc />
    public Task NotifyPublishedAsync(string appId, long releaseId, CancellationToken cancellationToken = default) =>
        Task.CompletedTask;
}
