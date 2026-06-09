using HarborAdmin.Modules.Admin.Application.Abstractions;
using HarborAdmin.Modules.Admin.Domain.Entities;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Modules.Admin.Contracts.FeatureDesign;
using HarborAdmin.Modules.Admin.Infrastructure.Contexts;

namespace HarborAdmin.Modules.Admin.Infrastructure.Resolvers;

/// <summary>
/// 基于元数据的动态资源处理器解析器。
/// </summary>
public sealed class AdminDynamicResourceHandlerResolver(
    IAdminDbContext db,
    IEnumerable<IAdminDynamicResourceHandler> handlers) : IAdminDynamicResourceHandlerResolver
{
    private readonly IReadOnlyDictionary<string, IAdminDynamicResourceHandler> handlersByKey =
        handlers.ToDictionary(handler => handler.HandlerKey, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<IAdminDynamicResourceHandler> ResolveAsync(string viewCode, CancellationToken cancellationToken)
    {
        var feature = await db.Orm.Select<AdminFeature>()
                          .Where(item => item.FeatureCode == viewCode
                                         && item.Enabled
                                         && item.NodeType != AdminFeatureNodeType.Category)
                          .FirstAsync(cancellationToken)
                      ?? throw new NotFoundDomainException($"Dynamic feature '{viewCode}' was not found.");
        var handlerKey = string.IsNullOrWhiteSpace(feature.HandlerKey)
            ? feature.FeatureCode
            : feature.HandlerKey;

        if (handlersByKey.TryGetValue(handlerKey, out var handler))
        {
            return handler;
        }

        throw new NotFoundDomainException($"Dynamic resource handler '{handlerKey}' was not registered.");
    }
}
