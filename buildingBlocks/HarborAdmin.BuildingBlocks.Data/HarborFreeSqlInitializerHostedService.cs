using Microsoft.Extensions.Hosting;

namespace HarborAdmin.BuildingBlocks.Data;

/// <summary>
/// 启动时初始化 FreeSqlCloud，触发数据库注册与按模块实体同步表结构。
/// </summary>
internal sealed class HarborFreeSqlInitializerHostedService(HarborFreeSqlCloud cloud) : IHostedService
{
    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        // HostedService 启动时强制解析 HarborFreeSqlCloud，触发延迟注册和结构同步逻辑。
        GC.KeepAlive(cloud);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
