namespace HarborAdmin.BuildingBlocks.Caching.Attributes;

/// <summary>
/// 标记允许参与 typed cache key 解析的模型属性。
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class CacheKeyPartAttribute : Attribute;
