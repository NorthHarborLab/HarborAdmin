using HarborAdmin.BuildingBlocks.Abstractions.Exception;

namespace HarborAdmin.Modules.Admin.Application.Services.Access;

/// <summary>
/// Admin 字段输入权限校验器。
/// </summary>
public sealed class AdminFieldInputValidator(AdminRuntimeAccessService accessService)
{
    /// <summary>
    /// 校验提交字段是否允许编辑；字段编码直接使用请求字典 key。
    /// </summary>
    public async Task EnsureEditableAsync(long userId, string featureCode, IReadOnlyDictionary<string, object?> values, AdminFieldSurface surface,
        CancellationToken cancellationToken = default)
    {
        var permissions = await accessService.GetFieldPermissionsAsync(userId, featureCode, surface, cancellationToken);
        if (permissions.IsSuperAdmin)
        {
            return;
        }

        var denied = values.Keys
            .Where(key => !permissions.EditableFields.Contains(key))
            .ToArray();
        if (denied.Length > 0)
        {
            throw new ForbiddenDomainException($"没有编辑字段的权限：{string.Join(", ", denied)}");
        }
    }
}