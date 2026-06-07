namespace HarborAdmin.Modules.Admin.Application.Abstractions;

/// <summary>
/// Admin 动态资源处理器解析器。
/// </summary>
public interface IAdminDynamicResourceHandlerResolver
{
    /// <summary>
    /// 根据视图编码解析资源处理器。
    /// </summary>
    Task<IAdminDynamicResourceHandler> ResolveAsync(string viewCode, CancellationToken cancellationToken);
}
