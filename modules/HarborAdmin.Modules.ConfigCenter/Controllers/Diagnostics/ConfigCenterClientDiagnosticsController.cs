using HarborAdmin.Client.ConfigCenter;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace HarborAdmin.Modules.ConfigCenter.Controllers.Diagnostics;

/// <summary>
/// 当前进程作为 ConfigCenter 运行中客户端的只读诊断接口。
/// </summary>
/// <param name="state">配置中心客户端状态</param>
/// <param name="configuration">当前进程配置</param>
[ApiController]
[Route("api/admin/config-center/client-state")]
public sealed class ConfigCenterClientDiagnosticsController(IConfigCenterClientState state, IConfiguration configuration) : ControllerBase
{
    /// <summary>
    /// 获取当前连接状态、版本与可选配置键的当前值。
    /// </summary>
    /// <param name="key">可选配置键。</param>
    [HttpGet]
    public Task<ApiResult<object>> Get([FromQuery] string? key = null)
    {
        string? remoteValue = null;
        if (!string.IsNullOrWhiteSpace(key))
        {
            state.CurrentData.TryGetValue(key.Trim(), out remoteValue);
        }

        return Task.FromResult(ApiResult.Ok<object>(new
        {
            state.AppId,
            state.ClientId,
            state.Connected,
            state.Version,
            state.LastConnectedAt,
            state.LastReloadAt,
            state.LastError,
            Key = key,
            RemoteValue = remoteValue,
            ConfigurationValue = string.IsNullOrWhiteSpace(key) ? null : configuration[key.Trim()]
        }));
    }
}