using HarborAdmin.Modules.Admin.Domain.Entities;

namespace HarborAdmin.Modules.Admin.Application.Mappings;

/// <summary>
/// Feature Action DTO 映射源。
/// </summary>
internal sealed record AdminFeatureActionMappingSource(AdminFeatureAction Action, IReadOnlyList<long> ApiIds);
