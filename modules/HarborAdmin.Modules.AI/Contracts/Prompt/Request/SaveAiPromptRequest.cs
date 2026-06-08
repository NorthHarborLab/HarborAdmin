using System.ComponentModel.DataAnnotations;

namespace HarborAdmin.Modules.AI.Contracts.Prompt.Request;

/// <summary>
/// 保存 AI Prompt 请求。
/// </summary>
public sealed class SaveAiPromptRequest
{
    /// <summary>
    /// Prompt Key。
    /// </summary>
    [Required(ErrorMessage = "Prompt Key 不能为空。")]
    [MaxLength(64)]
    public string PromptKey { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    [Required(ErrorMessage = "Prompt 名称不能为空。")]
    [MaxLength(120)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本号。
    /// </summary>
    [Range(0, int.MaxValue, ErrorMessage = "版本号不合法。")]
    public int Version { get; set; } = 1;

    /// <summary>
    /// 系统 Prompt（Markdown）。
    /// </summary>
    [Required(ErrorMessage = "系统 Prompt 不能为空。")]
    public string SystemPromptMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// 用户 Prompt（Markdown）。
    /// </summary>
    [Required(ErrorMessage = "用户 Prompt 不能为空。")]
    public string UserPromptMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// 变量 JSON。
    /// </summary>
    public string? VariablesJson { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}
