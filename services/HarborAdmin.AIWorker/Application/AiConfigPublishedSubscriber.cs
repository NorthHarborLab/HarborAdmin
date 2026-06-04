using DotNetCore.CAP;
using HarborAdmin.Modules.AI.Contracts.Constants;
using HarborAdmin.Modules.AI.Contracts.Dtos;

namespace HarborAdmin.AIWorker.Application;

/// <summary>
/// AI 配置发布事件订阅器。
/// </summary>
public sealed class AiConfigPublishedSubscriber(AiRuntimeConfigCache configCache) : ICapSubscribe
{
    /// <summary>
    /// 热加载最新发布快照。
    /// </summary>
    [CapSubscribe(AiEventTopics.ConfigPublished)]
    public Task HandleAsync(AiConfigPublishedEvent @event, CancellationToken cancellationToken = default) =>
        configCache.LoadReleaseAsync(@event.ReleaseId, cancellationToken);
}
