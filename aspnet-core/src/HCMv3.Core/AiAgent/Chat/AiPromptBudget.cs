using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.AiAgent.Chat;

/// <summary>
/// Keeps every request to the model within the model's context window, so the model can never silently lose
/// the assistant's rules.
///
/// <para>
/// <b>Why.</b> A model host that receives more tokens than its context window does not fail -- Ollama drops the
/// START of the request, which is exactly where the system prompt with the assistant's rules sits. A long
/// administrator conversation could therefore reach the model with its rules missing and no error anywhere.
/// (Tenant isolation and permissions do not depend on the prompt -- they are enforced by the tools and ABP --
/// but the prompt's rules are what keep the assistant honest: no invented numbers, no claiming something was
/// submitted, reply in the user's language.)
/// </para>
///
/// <para>
/// <b>What is never removed:</b> the system prompt, the tool definitions, and the user's current question.
/// <b>What goes first:</b> the oldest earlier messages of the conversation. Only if that is not enough are the
/// results of tools called while answering THIS question shortened -- replaced by a note, so every tool call
/// keeps its result and the model can ask again. If the fixed part alone is too big, the request is refused
/// with <see cref="AiPromptTooLargeException"/>; it is never sent short.
/// </para>
///
/// <para>
/// Token counts are ESTIMATED, deliberately on the high side (the real tokenizer is on the model host).
/// Over-estimating only trims history a little early; under-estimating would defeat the point.
/// </para>
/// </summary>
public static class AiPromptBudget
{
    /// <summary>Role markers and separators the chat template adds around each message.</summary>
    public const int PerMessageOverhead = 8;

    public const string OmittedToolResult =
        "{\"note\":\"This result was left out to stay within the model's limit. Call the tool again if you still need it.\"}";

    /// <summary>
    /// A conservative estimate: ASCII text (English, JSON) at 2.5 characters per token, anything else (Arabic)
    /// at 1.5. Real Qwen tokenization is roughly 3-4 and 2-3 respectively, so this errs towards "too big".
    /// </summary>
    public static int EstimateTokens(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var ascii = 0;
        foreach (var ch in text)
        {
            if (ch < 128)
            {
                ascii++;
            }
        }

        var other = text.Length - ascii;
        return (int)Math.Ceiling(ascii / 2.5) + (int)Math.Ceiling(other / 1.5);
    }

    public static int Estimate(AiChatMessage message)
    {
        var tokens = PerMessageOverhead + EstimateTokens(message.Content);

        foreach (var call in message.ToolCalls ?? new List<AiToolCall>())
        {
            tokens += PerMessageOverhead + EstimateTokens(call.Name) + EstimateTokens(call.ArgumentsJson) + EstimateTokens(call.Id);
        }

        return tokens + EstimateTokens(message.ToolCallId) + EstimateTokens(message.ToolName);
    }

    public static int Estimate(AiToolDefinition tool) =>
        PerMessageOverhead + EstimateTokens(tool.Name) + EstimateTokens(tool.Description) + EstimateTokens(tool.Parameters?.ToJsonString());

    public static int Estimate(AiChatRequest request) =>
        request.Messages.Sum(Estimate) + (request.Tools ?? new List<AiToolDefinition>()).Sum(Estimate);

    /// <summary>
    /// Returns a request that fits in <paramref name="budgetTokens"/>, trimmed as described on the class. The
    /// original request is not changed -- the caller keeps the full conversation; only what is SENT is trimmed.
    /// </summary>
    /// <exception cref="AiPromptTooLargeException">The system prompt, tools and current question alone exceed
    /// the budget.</exception>
    public static AiChatRequest Fit(AiChatRequest request, int budgetTokens, out AiPromptFitReport report)
    {
        report = new AiPromptFitReport { EstimatedTokensBefore = Estimate(request), BudgetTokens = budgetTokens };

        var messages = request.Messages.ToList();
        var tools = request.Tools ?? new List<AiToolDefinition>();
        var total = report.EstimatedTokensBefore;

        if (total <= budgetTokens)
        {
            report.EstimatedTokensAfter = total;
            return request;
        }

        var systemCount = messages.Count > 0 && messages[0].Role == AiChatMessage.SystemRole ? 1 : 0;
        var currentQuestion = messages.FindLastIndex(m => m.Role == AiChatMessage.UserRole);
        if (currentQuestion < 0)
        {
            throw new InvalidOperationException("A request to the model must contain the user's question.");
        }

        // 1. Oldest earlier conversation first. A history left starting with an assistant reply is an answer to a
        //    question that is no longer there, so that goes too. System messages anywhere before the question --
        //    the rules, and the per-user details placed just before it -- are never candidates.
        int OldestHistoryIndex()
        {
            for (var i = systemCount; i < currentQuestion; i++)
            {
                if (messages[i].Role != AiChatMessage.SystemRole)
                {
                    return i;
                }
            }

            return -1;
        }

        var oldest = OldestHistoryIndex();
        while (total > budgetTokens && oldest >= 0)
        {
            do
            {
                total -= Estimate(messages[oldest]);
                messages.RemoveAt(oldest);
                currentQuestion--;
                report.HistoryMessagesDropped++;
                oldest = OldestHistoryIndex();
            }
            while (oldest >= 0 && messages[oldest].Role != AiChatMessage.UserRole);
        }

        // 2. Then the results of tools already called for this question, oldest first -- shortened, not removed,
        //    so each tool call still has its result.
        for (var i = currentQuestion + 1; i < messages.Count && total > budgetTokens; i++)
        {
            var message = messages[i];
            if (message.Role != AiChatMessage.ToolRole || (message.Content?.Length ?? 0) <= OmittedToolResult.Length)
            {
                continue;
            }

            var shortened = AiChatMessage.Tool(message.ToolCallId, message.ToolName, OmittedToolResult);
            total += Estimate(shortened) - Estimate(message);
            messages[i] = shortened;
            report.ToolResultsShortened++;
        }

        // 3. Never send it short.
        if (total > budgetTokens)
        {
            throw new AiPromptTooLargeException(
                $"The request needs about {total} tokens but the model allows {budgetTokens}, even with the conversation history removed.");
        }

        report.EstimatedTokensAfter = total;
        return new AiChatRequest { Messages = messages, Tools = tools };
    }
}

public class AiPromptFitReport
{
    public int BudgetTokens { get; set; }

    public int EstimatedTokensBefore { get; set; }

    public int EstimatedTokensAfter { get; set; }

    public int HistoryMessagesDropped { get; set; }

    public int ToolResultsShortened { get; set; }

    public bool Trimmed => HistoryMessagesDropped > 0 || ToolResultsShortened > 0;
}

/// <summary>The part of a request that may never be removed is itself bigger than the model allows.</summary>
public class AiPromptTooLargeException : Exception
{
    public AiPromptTooLargeException(string message) : base(message)
    {
    }
}
