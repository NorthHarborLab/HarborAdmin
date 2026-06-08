using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.Admin.Contracts.System.Request;

/// <summary>
/// 按 key 失效请求。
/// </summary>
public sealed class InvalidateCacheKeyRequest
{
    /// <summary>
    /// 缓存键。
    /// </summary>
    [Required(ErrorMessage = "缓存键不能为空。")]
    [MaxLength(512)]
    public string Key { get; set; } = string.Empty;
}
