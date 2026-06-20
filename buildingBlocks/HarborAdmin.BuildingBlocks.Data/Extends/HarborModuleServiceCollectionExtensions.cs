using HarborAdmin.BuildingBlocks.Data.DbContext;
using Microsoft.Extensions.DependencyInjection;

namespace HarborAdmin.BuildingBlocks.Data.Extends;

/// <summary>
/// Harbor 模块通用依赖注入扩展。
/// </summary>
public static class HarborModuleServiceCollectionExtensions
{
    /// <summary>
    /// 注册模块 DbContext 与仓储。
    /// </summary>
    /// <typeparam name="TDbContext">模块 DbContext 接口类型。</typeparam>
    /// <typeparam name="TDbContextImplementation">模块 DbContext 实现类型。</typeparam>
    /// <typeparam name="TRepository">模块仓储接口类型。</typeparam>
    /// <typeparam name="TRepositoryImplementation">模块仓储实现类型。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <param name="repositoryLifetime">仓储生命周期。</param>
    /// <param name="dbContextLifetime">DbContext 生命周期。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddHarborModuleData<TDbContext, TDbContextImplementation, TRepository, TRepositoryImplementation>(
        this IServiceCollection services,
        ServiceLifetime repositoryLifetime = ServiceLifetime.Singleton,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Singleton)
        where TDbContext : class, IHarborModuleDbContext
        where TDbContextImplementation : class, TDbContext
        where TRepository : class
        where TRepositoryImplementation : class, TRepository
    {
        services.Add(new ServiceDescriptor(typeof(TDbContext), typeof(TDbContextImplementation), dbContextLifetime));
        services.Add(new ServiceDescriptor(typeof(TRepository), typeof(TRepositoryImplementation), repositoryLifetime));
        return services;
    }

    /// <summary>
    /// 注册模块 DbContext、仓储与模块 ServiceContext。
    /// </summary>
    /// <typeparam name="TDbContext">模块 DbContext 接口类型。</typeparam>
    /// <typeparam name="TDbContextImplementation">模块 DbContext 实现类型。</typeparam>
    /// <typeparam name="TRepository">模块仓储接口类型。</typeparam>
    /// <typeparam name="TRepositoryImplementation">模块仓储实现类型。</typeparam>
    /// <typeparam name="TServiceContext">模块 ServiceContext 类型。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <param name="repositoryLifetime">仓储生命周期。</param>
    /// <param name="dbContextLifetime">DbContext 生命周期。</param>
    /// <param name="serviceContextLifetime">ServiceContext 生命周期。</param>
    /// <returns>原服务集合。</returns>
    public static IServiceCollection AddHarborModuleData<TDbContext, TDbContextImplementation, TRepository, TRepositoryImplementation, TServiceContext>(
        this IServiceCollection services,
        ServiceLifetime repositoryLifetime = ServiceLifetime.Singleton,
        ServiceLifetime dbContextLifetime = ServiceLifetime.Singleton,
        ServiceLifetime serviceContextLifetime = ServiceLifetime.Scoped)
        where TDbContext : class, IHarborModuleDbContext
        where TDbContextImplementation : class, TDbContext
        where TRepository : class
        where TRepositoryImplementation : class, TRepository
        where TServiceContext : class
    {
        services.AddHarborModuleData<TDbContext, TDbContextImplementation, TRepository, TRepositoryImplementation>(
            repositoryLifetime,
            dbContextLifetime);
        services.Add(new ServiceDescriptor(typeof(TServiceContext), typeof(TServiceContext), serviceContextLifetime));
        return services;
    }
}