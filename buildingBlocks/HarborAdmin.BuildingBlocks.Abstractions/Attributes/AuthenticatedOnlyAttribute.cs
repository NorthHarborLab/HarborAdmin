namespace HarborAdmin.BuildingBlocks.Abstractions.Attributes;

/// <summary>
/// 标记接口只需要登录，不需要权限点绑定。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AuthenticatedOnlyAttribute : Attribute;
