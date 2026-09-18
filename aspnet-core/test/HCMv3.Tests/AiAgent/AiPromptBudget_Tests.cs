using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using HCMv3.AiAgent.Chat;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// The model must never receive a request with its rules cut off. These pin the order things are given up in:
/// earlier conversation first, then the size of this question's tool results -- and never the system prompt,
/// the tools or the question itself, which are refused rather than shortened.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AiPromptBudget_Tests
{
    private const string Rules = "RULES: never invent numbers; never claim something was submitted.";

    private static AiChatRequest Conversation(int earlierExchanges, int charactersEach = 400)
    {
        var request = new AiChatRequest();
        request.Messages.Add(AiChatMessage.System(Rules));

        for (var i = 1; i <= earlierExchanges; i++)
        {
            request.Messages.Add(AiChatMessage.User($"question {i} " + new string('q', charactersEach)));
            request.Messages.Add(AiChatMessage.Assistant($"answer {i} " + new string('a', charactersEach)));
        }

        request.Messages.Add(AiChatMessage.User("the current question"));
        request.Tools.Add(new AiToolDefinition
        {
            Name = "get_my_leave_balances",
            Description = "Balances.",
            Parameters = new JsonObject { ["type"] = "object" }
        });

        return request;
    }

    [Fact]
    public void A_Request_That_Fits_Should_Be_Sent_Unchanged()
    {
        var request = Conversation(2);

        var fitted = AiPromptBudget.Fit(request, 10_000, out var report);

        fitted.ShouldBeSameAs(request);
        report.Trimmed.ShouldBeFalse();
    }

    [Fact]
    public void The_Oldest_Conversation_Should_Go_First_And_The_Rules_Never()
    {
        var request = Conversation(10);
        var budget = AiPromptBudget.Estimate(request) / 2;

        var fitted = AiPromptBudget.Fit(request, budget, out var report);

        AiPromptBudget.Estimate(fitted).ShouldBeLessThanOrEqualTo(budget);
        report.HistoryMessagesDropped.ShouldBeGreaterThan(0);

        // The rules are still first and whole; the tools and the current question are all still there.
        fitted.Messages.First().Role.ShouldBe(AiChatMessage.SystemRole);
        fitted.Messages.First().Content.ShouldBe(Rules);
        fitted.Tools.Count.ShouldBe(1);
        fitted.Messages.Last().Content.ShouldBe("the current question");

        // What was dropped is the OLDEST: the newest earlier exchange survived, the first did not.
        fitted.Messages.ShouldNotContain(m => m.Content.StartsWith("question 1 "));
        fitted.Messages.ShouldContain(m => m.Content.StartsWith("answer 10 "));

        // And what is left of the history starts with a question, not with an answer to a missing one.
        fitted.Messages[1].Role.ShouldBe(AiChatMessage.UserRole);
    }

    [Fact]
    public void The_Per_User_Details_Message_Before_The_Question_Should_Never_Be_Dropped()
    {
        // Layout used for prompt caching: rules first, history, then the user's details, then the question.
        var request = Conversation(10);
        request.Messages.Insert(request.Messages.Count - 1, AiChatMessage.System("Today is 2026-09-18. You are talking to Sara."));

        var fitted = AiPromptBudget.Fit(request, AiPromptBudget.Estimate(request) / 3, out var report);

        report.HistoryMessagesDropped.ShouldBeGreaterThan(0);
        fitted.Messages[0].Content.ShouldBe(Rules);
        fitted.Messages[^2].Content.ShouldBe("Today is 2026-09-18. You are talking to Sara.");
        fitted.Messages[^1].Content.ShouldBe("the current question");
    }

    [Fact]
    public void The_Original_Conversation_Should_Not_Be_Changed()
    {
        var request = Conversation(10);
        var countBefore = request.Messages.Count;

        AiPromptBudget.Fit(request, AiPromptBudget.Estimate(request) / 2, out _);

        request.Messages.Count.ShouldBe(countBefore);
    }

    [Fact]
    public void Tool_Results_Should_Be_Shortened_Only_Once_The_History_Is_Gone_And_Keep_Their_Calls()
    {
        var request = Conversation(2);
        request.Messages.Add(AiChatMessage.Assistant(null, new List<AiToolCall> { new() { Id = "c1", Name = "get_my_leave_balances", ArgumentsJson = "{}" } }));
        request.Messages.Add(AiChatMessage.Tool("c1", "get_my_leave_balances", "{\"balances\":\"" + new string('x', 6000) + "\"}"));

        var fixedPart = AiPromptBudget.Estimate(new AiChatRequest
        {
            Messages = { request.Messages[0], request.Messages[5], request.Messages[6] },
            Tools = request.Tools
        });

        var fitted = AiPromptBudget.Fit(request, fixedPart + 100, out var report);

        report.HistoryMessagesDropped.ShouldBe(4);
        report.ToolResultsShortened.ShouldBe(1);

        // The call and its (shortened) result are both still there, in order.
        fitted.Messages[^2].ToolCalls.Single().Id.ShouldBe("c1");
        fitted.Messages[^1].ToolCallId.ShouldBe("c1");
        fitted.Messages[^1].Content.ShouldBe(AiPromptBudget.OmittedToolResult);
        fitted.Messages[0].Content.ShouldBe(Rules);
    }

    [Fact]
    public void A_Request_Whose_Rules_Tools_And_Question_Do_Not_Fit_Should_Be_Refused_Not_Shortened()
    {
        var request = Conversation(0);
        request.Messages[^1] = AiChatMessage.User(new string('w', 5000));

        Should.Throw<AiPromptTooLargeException>(() => AiPromptBudget.Fit(request, 500, out _));
    }

    [Fact]
    public void Arabic_Should_Count_As_More_Tokens_Than_English_Of_The_Same_Length()
    {
        AiPromptBudget.EstimateTokens(new string('ب', 300)).ShouldBeGreaterThan(AiPromptBudget.EstimateTokens(new string('b', 300)));
    }

    [Fact]
    public void The_Tool_Definitions_Should_Count()
    {
        var withoutTools = Conversation(0);
        withoutTools.Tools.Clear();

        AiPromptBudget.Estimate(Conversation(0)).ShouldBeGreaterThan(AiPromptBudget.Estimate(withoutTools));
    }
}
