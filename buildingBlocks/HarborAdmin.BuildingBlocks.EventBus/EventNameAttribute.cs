namespace HarborAdmin.BuildingBlocks.EventBus;

/// <summary>
/// 声明集成事件发布时使用的 CAP topic 名称。
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventNameAttribute(string name) : Attribute
{
    /// <summary>
    /// CAP topic 名称。
    /// </summary>
    public string Name { get; } = string.IsNullOrWhiteSpace(name)
        ? throw new ArgumentException("Event name cannot be empty.", nameof(name))
        : name;
}
