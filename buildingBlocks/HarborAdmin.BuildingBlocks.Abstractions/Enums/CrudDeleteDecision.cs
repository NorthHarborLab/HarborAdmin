namespace HarborAdmin.BuildingBlocks.Abstractions.Enums;

/// <summary>
/// CRUD 删除决策。
/// </summary>
public enum CrudDeleteDecision
{
    /// <summary>
    /// 执行物理删除。
    /// </summary>
    PhysicalDelete,

    /// <summary>
    /// 执行软删除。
    /// </summary>
    SoftDelete,

    /// <summary>
    /// 拒绝删除。
    /// </summary>
    Reject
}