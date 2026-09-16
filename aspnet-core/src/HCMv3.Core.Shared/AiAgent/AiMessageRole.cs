namespace HCMv3.AiAgent;

/// <summary>Who produced a message in an AI assistant conversation.</summary>
public enum AiMessageRole
{
    /// <summary>Typed by the logged-in user.</summary>
    User = 0,

    /// <summary>Written by the model. Either a reply shown to the user, or a request to call tools (which is
    /// stored for the audit trail and never shown).</summary>
    Assistant = 1,

    /// <summary>The result of a tool the model called. Stored for the audit trail; never shown.</summary>
    Tool = 2
}
