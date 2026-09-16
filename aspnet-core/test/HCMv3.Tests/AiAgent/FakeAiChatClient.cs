using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HCMv3.AiAgent.Chat;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// A scripted model. Each call takes the next scripted step, and every request is recorded as a snapshot --
/// the service keeps appending to the same request object between calls, so without a copy every recorded
/// request would look like the last one.
/// </summary>
public class FakeAiChatClient : IAiChatClient
{
    private readonly Queue<Func<AiChatResponse>> _steps = new();

    public List<AiChatRequest> Requests { get; } = new();

    public FakeAiChatClient RepliesWith(string content)
    {
        _steps.Enqueue(() => new AiChatResponse { Content = content });
        return this;
    }

    public FakeAiChatClient CallsTool(string name, string argumentsJson = "{}")
    {
        _steps.Enqueue(() => new AiChatResponse
        {
            ToolCalls = new List<AiToolCall>
            {
                new() { Id = "call_" + Guid.NewGuid().ToString("N").Substring(0, 8), Name = name, ArgumentsJson = argumentsJson }
            }
        });
        return this;
    }

    public FakeAiChatClient Fails(bool isTimeout = false)
    {
        _steps.Enqueue(() => throw new AiChatClientException("model down", isTimeout: isTimeout));
        return this;
    }

    public Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(new AiChatRequest
        {
            Messages = request.Messages.Select(m => new AiChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                ToolCalls = m.ToolCalls.ToList(),
                ToolCallId = m.ToolCallId,
                ToolName = m.ToolName
            }).ToList(),
            Tools = request.Tools.ToList()
        });

        if (_steps.Count == 0)
        {
            throw new InvalidOperationException("The model was called more times than the test scripted.");
        }

        return Task.FromResult(_steps.Dequeue()());
    }
}
