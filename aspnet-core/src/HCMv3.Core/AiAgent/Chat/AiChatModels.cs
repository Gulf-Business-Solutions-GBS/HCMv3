using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace HCMv3.AiAgent.Chat;

/// <summary>
/// One message sent to the model. Mirrors the OpenAI chat-completions shape, which is the one Ollama, vLLM
/// and the hosted Qwen endpoints all accept -- so the model host is a configuration choice, not code.
/// </summary>
public class AiChatMessage
{
    public const string SystemRole = "system";
    public const string UserRole = "user";
    public const string AssistantRole = "assistant";
    public const string ToolRole = "tool";

    public string Role { get; set; }

    public string Content { get; set; }

    /// <summary>Assistant messages only: the tools it asked to run.</summary>
    public List<AiToolCall> ToolCalls { get; set; } = new();

    /// <summary>Tool messages only: which call this is the result of.</summary>
    public string ToolCallId { get; set; }

    /// <summary>Tool messages only.</summary>
    public string ToolName { get; set; }

    public static AiChatMessage System(string content) => new() { Role = SystemRole, Content = content };

    public static AiChatMessage User(string content) => new() { Role = UserRole, Content = content };

    public static AiChatMessage Assistant(string content, List<AiToolCall> toolCalls = null) =>
        new() { Role = AssistantRole, Content = content, ToolCalls = toolCalls ?? new List<AiToolCall>() };

    public static AiChatMessage Tool(string toolCallId, string toolName, string content) =>
        new() { Role = ToolRole, ToolCallId = toolCallId, ToolName = toolName, Content = content };
}

/// <summary>A tool the model asked to run.</summary>
public class AiToolCall
{
    public string Id { get; set; }

    public string Name { get; set; }

    /// <summary>
    /// The arguments exactly as the model produced them. Always a JSON object's text, but NOT trusted to be
    /// valid JSON or to match the schema -- small models get both wrong, so the tool validates.
    /// </summary>
    public string ArgumentsJson { get; set; }
}

/// <summary>A tool offered to the model.</summary>
public class AiToolDefinition
{
    public string Name { get; set; }

    public string Description { get; set; }

    /// <summary>JSON Schema for the arguments.</summary>
    public JsonObject Parameters { get; set; }
}

public class AiChatRequest
{
    public List<AiChatMessage> Messages { get; set; } = new();

    public List<AiToolDefinition> Tools { get; set; } = new();
}

public class AiChatResponse
{
    /// <summary>The reply text with any reasoning block removed. Null or empty when the model only called
    /// tools.</summary>
    public string Content { get; set; }

    public List<AiToolCall> ToolCalls { get; set; } = new();
}

/// <summary>
/// The model host could not be reached or returned something unusable. Deliberately separate from
/// <c>UserFriendlyException</c>: the caller decides what the user is told, and the detail here (URLs, raw
/// responses) is for the log, not the screen.
/// </summary>
public class AiChatClientException : Exception
{
    public bool IsTimeout { get; }

    public AiChatClientException(string message, Exception innerException = null, bool isTimeout = false)
        : base(message, innerException)
    {
        IsTimeout = isTimeout;
    }
}
