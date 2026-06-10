using HarborAdmin.BuildingBlocks.Abstractions.Attributes;
using HarborAdmin.BuildingBlocks.Abstractions.Auth;

namespace HarborAdmin.BuildingBlocks.Abstractions.Domain;

/// <summary>
/// 以 <see cref="long"/> 为主键的实体基类
/// </summary>
public abstract class EntityBase : IEntity<long>
{
    /// <summary>
    /// 实体主键；字段权限裁剪时必须保留。
    /// </summary>
    [FieldPermissionIgnore]
    public long Id { get; set; }
}
