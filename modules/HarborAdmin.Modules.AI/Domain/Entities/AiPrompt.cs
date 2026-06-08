using FreeSql.DataAnnotations;
using HarborAdmin.BuildingBlocks.Abstractions.Domain;

namespace HarborAdmin.Modules.AI.Domain.Entities;

/// <summary>
/// AI Prompt。
/// </summary>
[DbKey("AdminDb")]
[Index("ux_ai_prompt_key_version", $"{nameof(PromptKey)},{nameof(Version)}", true)]
public sealed class AiPrompt : AuditableEntity
{
    /// <summary>
    /// Prompt Key。
    /// </summary>
    public string PromptKey { get; set; } = string.Empty;

    /// <summary>
    /// 名称。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 版本。
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// System Prompt Markdown。
    /// </summary>
    [Column(StringLength = -1)]
    public string SystemPromptMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// User Prompt Markdown。
    /// </summary>
    [Column(StringLength = -1)]
    public string UserPromptMarkdown { get; set; } = string.Empty;

    /// <summary>
    /// 变量说明 JSON。
    /// </summary>
    [Column(StringLength = -1)]
    public string? VariablesJson { get; set; }

    /// <summary>
    /// 是否启用。
    /// </summary>
    public bool Enabled { get; set; }
}
