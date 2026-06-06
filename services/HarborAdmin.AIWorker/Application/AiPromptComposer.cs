using System.Text;
using System.Text.Json;
using HarborAdmin.BuildingBlocks.Abstractions.Api;
using HarborAdmin.BuildingBlocks.Abstractions.Exception;
using HarborAdmin.Client.AI.Constants;
using HarborAdmin.Client.AI.Invocation;
using HarborAdmin.Modules.AI.Contracts.Snapshots;

namespace HarborAdmin.AIWorker.Application;

/// <summary>
/// AI Prompt 组装器。
/// </summary>
public sealed class AiPromptComposer
{
    /// <summary>
    /// 组装消息。
    /// </summary>
    public IReadOnlyList<AiMessage> Compose(
        AiBusinessSnapshot business,
        AiBusinessRequest request,
        AiPromptSnapshot? prompt,
        IReadOnlyList<AiKnowledgeSnapshot> knowledgeBases,
        out IReadOnlyList<AiReference> references,
        out int contextLength)
    {
        if (!business.AllowKnowledgeText && !string.IsNullOrWhiteSpace(request.KnowledgeText))
        {
            throw new ValidationDomainException(AiErrorCodes.OverrideNotAllowed);
        }

        if (string.Equals(request.KnowledgeTextMode, "Override", StringComparison.OrdinalIgnoreCase) &&
            !business.AllowKnowledgeTextOverride)
        {
            throw new ValidationDomainException(AiErrorCodes.KnowledgeOverrideNotAllowed);
        }

        var variables = request.PromptVariables ?? new Dictionary<string, string>();
        ValidateVariables(prompt, variables);
        var messages = new List<AiMessage>();
        var system = new StringBuilder();
        Append(system, ApplyVariables(prompt?.SystemPromptMarkdown, variables));
        Append(system, ApplyVariables(request.PromptOverride, variables));

        var knowledgeBuilder = new StringBuilder();
        var localReferences = new List<AiReference>();
        var overrideKnowledge = string.Equals(request.KnowledgeTextMode, "Override", StringComparison.OrdinalIgnoreCase);
        if (!overrideKnowledge)
        {
            foreach (var knowledgeBase in knowledgeBases)
            {
                Append(knowledgeBuilder, knowledgeBase.ContentMarkdown);
                if (knowledgeBase.AppendReferences)
                {
                    localReferences.Add(new AiReference(knowledgeBase.KnowledgeKey, knowledgeBase.Name));
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(request.KnowledgeText) &&
            (overrideKnowledge || business.AllowKnowledgeTextAppend))
        {
            Append(knowledgeBuilder, request.KnowledgeText);
        }

        if (knowledgeBuilder.Length > 0)
        {
            Append(system, "## Knowledge");
            Append(system, knowledgeBuilder.ToString());
        }

        if (system.Length > 0)
        {
            messages.Add(new AiMessage("system", system.ToString()));
        }

        if (request.Messages is not null)
        {
            messages.AddRange(request.Messages.Where(m => !string.IsNullOrWhiteSpace(m.Content) || m.Parts is { Count: > 0 }));
        }

        var userPrompt = ApplyVariables(prompt?.UserPromptMarkdown, variables);
        if (!string.IsNullOrWhiteSpace(userPrompt))
        {
            messages.Add(new AiMessage("user", userPrompt));
        }

        if (!string.IsNullOrWhiteSpace(request.Input))
        {
            messages.Add(new AiMessage("user", request.Input));
        }

        references = localReferences;
        contextLength = messages.Sum(EstimateTokens);
        return messages;
    }

    private static void ValidateVariables(AiPromptSnapshot? prompt, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrWhiteSpace(prompt?.VariablesJson))
        {
            return;
        }

        using var document = JsonDocument.Parse(prompt.VariablesJson);
        foreach (var name in RequiredVariables(document.RootElement))
        {
            if (!variables.TryGetValue(name, out var value) || string.IsNullOrWhiteSpace(value))
            {
                throw new ValidationDomainException($"AI_PROMPT_VARIABLE_REQUIRED:{name}");
            }
        }
    }

    private static IEnumerable<string> RequiredVariables(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                {
                    yield return item.GetString()!;
                }
            }
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("required", out var required) && required.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in required.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.String))
                {
                    yield return item.GetString()!;
                }
            }

            foreach (var property in root.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object &&
                    property.Value.TryGetProperty("required", out var isRequired) &&
                    isRequired.ValueKind is JsonValueKind.True)
                {
                    yield return property.Name;
                }
            }
        }
    }

    private static int EstimateTokens(AiMessage message)
    {
        var chars = (message.Content?.Length ?? 0) + (message.Parts?.Sum(p => (p.Text?.Length ?? 0) + (p.ResultJson?.Length ?? 0)) ?? 0);
        return Math.Max(1, (int)Math.Ceiling(chars / 3.0));
    }

    private static string? ApplyVariables(string? template, IReadOnlyDictionary<string, string> variables)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        var result = template;
        foreach (var (key, value) in variables)
        {
            result = result.Replace("{" + key + "}", value, StringComparison.Ordinal);
        }

        return result;
    }

    private static void Append(StringBuilder builder, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (builder.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine();
        }

        builder.Append(value.Trim());
    }
}
