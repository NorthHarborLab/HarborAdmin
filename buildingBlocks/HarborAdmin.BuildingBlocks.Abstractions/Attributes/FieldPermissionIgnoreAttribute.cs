namespace HarborAdmin.BuildingBlocks.Abstractions.Attributes;

/// <summary>
/// 标记字段权限裁剪时必须保留的系统字段。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class FieldPermissionIgnoreAttribute : Attribute;
