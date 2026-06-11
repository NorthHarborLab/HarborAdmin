using System.Reflection;
using HarborAdmin.BuildingBlocks.Abstractions.Modules;

namespace HarborAdmin.Host.Infrastructure;

/// <summary>
/// MVC 模块控制器注册扩展。
/// </summary>
internal static class ModuleApplicationPartExtensions
{
    /// <summary>
    /// 将 Host 引用的 HarborAdmin 模块程序集自动注册为 MVC ApplicationPart。
    /// </summary>
    public static IMvcBuilder AddHarborModuleApplicationParts(this IMvcBuilder builder, IEnumerable<Assembly>? moduleAssemblies = null)
    {
        foreach (var assembly in HarborModuleAssemblyDiscovery.Discover(moduleAssemblies))
        {
            builder.AddApplicationPart(assembly);
        }

        return builder;
    }
}