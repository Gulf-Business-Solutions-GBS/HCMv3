namespace HCMv3.AiAgent;

/// <summary>
/// Limits for the AI assistant. Kept together because several of them trade against each other: a longer
/// history makes the model better at follow-up questions and slower on a CPU-only host, and more tool hops
/// let it answer compound questions at the cost of a longer worst-case wait.
/// </summary>
public static class AiAgentConsts
{
    /// <summary>What a user may type in one message. Generous for a question, small enough that nobody
    /// pastes a document into it.</summary>
    public const int MaxUserMessageLength = 2000;

    public const int MaxTitleLength = 120;

    public const int MaxToolNameLength = 100;

    /// <summary>
    /// Model calls allowed while answering one message. Each tool call the model makes costs one more model
    /// call to read its result, so this bounds how long a confused model can loop before the user gets an
    /// answer. Five covers "look up types, look up balance, then answer" with room to spare.
    /// </summary>
    public const int MaxModelCallsPerMessage = 5;

    /// <summary>
    /// Earlier user and assistant messages replayed to the model. Only the visible conversation is replayed,
    /// never earlier tool results, so this is cheap in tokens and cannot orphan a tool result from its call.
    /// </summary>
    public const int MaxHistoryMessages = 20;

    /// <summary>Tool output handed back to the model. Small models lose the thread on long results, and
    /// nothing a leave or payslip question needs comes close.</summary>
    public const int MaxToolResultLength = 8000;
}
