using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.AiAgent.Chat;

/// <summary>
/// <see cref="IAiChatClient"/> over the OpenAI chat-completions HTTP API, which Ollama, vLLM and the hosted
/// Qwen endpoints all speak.
///
/// <para>
/// Hand-written against the wire format rather than an SDK so the feature adds no package to the solution,
/// and because the tolerance below -- for small local models that bend the format -- is exactly what an SDK
/// would not offer.
/// </para>
/// </summary>
public class HttpAiChatClient : IAiChatClient, ITransientDependency
{
    private static readonly Regex ThinkBlock =
        new(@"<think>.*?</think>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TextToolCall =
        new(@"<tool_call>\s*(\{.*?\})\s*</tool_call>", RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly AiModelHttpHandlerProvider _handlerProvider;
    private readonly AiAgentConfiguration _configuration;

    public HttpAiChatClient(AiModelHttpHandlerProvider handlerProvider, AiAgentConfiguration configuration)
    {
        _handlerProvider = handlerProvider;
        _configuration = configuration;
    }

    /// <summary>For tests: how a hostname is resolved when checking it is on the private network.</summary>
    public Func<string, Task<System.Net.IPAddress[]>> HostResolver { get; set; }

    /// <summary>For tests: how long to pause before trying a host that is not answering again.</summary>
    public Func<int, TimeSpan> RetryPause { get; set; } = PauseBeforeAttempt;

    public async Task<AiChatResponse> CompleteAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(_configuration.ChatCompletionsUrl, UriKind.Absolute, out var endpoint))
        {
            throw new AiChatClientException($"AiAgent:BaseUrl '{_configuration.BaseUrl}' is not a valid address.");
        }

        // Before anything is built or sent: employee data may only go to this machine or the private network.
        await AiModelHostGuard.EnsureAllowedAsync(endpoint, _configuration.AllowExternalModelHost, HostResolver);

        // Last line of defence: callers trim with AiPromptBudget.Fit before calling. Anything still too big is a
        // bug, and it must fail loudly here -- the model host would otherwise drop the system prompt silently.
        var estimated = AiPromptBudget.Estimate(request);
        if (estimated > _configuration.PromptBudgetTokens)
        {
            throw new AiPromptTooLargeException(
                $"Refusing to send about {estimated} tokens to a model limited to {_configuration.PromptBudgetTokens}; it would drop the start of the request.");
        }

        var body = BuildRequestBody(request);
        var json = body.ToJsonString();

        // A host that is starting, or loading the model back into memory, refuses connections or answers "busy"
        // for a while and is then perfectly well. Waiting through that is the honest answer; giving up in the
        // first second and saying the assistant is unavailable is not. See StartupWaitSeconds.
        var waitingSince = DateTime.UtcNow;
        var attempt = 0;

        while (true)
        {
            attempt++;

            try
            {
                return await SendOnceAsync(endpoint, json, cancellationToken);
            }
            catch (AiChatClientException ex) when (ex.IsHostNotAnswering && !cancellationToken.IsCancellationRequested)
            {
                var waitedFor = DateTime.UtcNow - waitingSince;
                var budget = TimeSpan.FromSeconds(_configuration.StartupWaitSeconds);
                var pause = RetryPause(attempt);

                if (waitedFor + pause >= budget)
                {
                    throw;
                }

                await Task.Delay(pause, cancellationToken);
            }
        }
    }

    /// <summary>
    /// Backs off gently: a host that is only busy answers on the next try, and one that is loading a model from
    /// disk takes tens of seconds. Capped so a long wait is still made up of regular attempts.
    /// </summary>
    private static TimeSpan PauseBeforeAttempt(int attempt) =>
        TimeSpan.FromSeconds(Math.Min(15, Math.Pow(2, Math.Min(attempt, 4))));

    private async Task<AiChatResponse> SendOnceAsync(Uri endpoint, string json, CancellationToken cancellationToken)
    {
        using var client = _handlerProvider.CreateClient(TimeSpan.FromSeconds(_configuration.TimeoutSeconds));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrEmpty(_configuration.ApiKey))
        {
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _configuration.ApiKey);
        }

        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(httpRequest, cancellationToken);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new AiChatClientException(
                $"The model at {_configuration.ChatCompletionsUrl} did not answer within {_configuration.TimeoutSeconds}s.", ex, isTimeout: true);
        }
        catch (HttpRequestException ex)
        {
            // Nothing listening, or the connection dropped: the host is down or still coming up, which is worth
            // waiting for rather than reporting.
            throw new AiChatClientException(
                $"The model at {_configuration.ChatCompletionsUrl} could not be reached.", ex, isHostNotAnswering: true);
        }

        using (response)
        {
            var text = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new AiChatClientException(
                    $"The model at {_configuration.ChatCompletionsUrl} returned {(int)response.StatusCode}: {Truncate(text, 500)}",
                    isHostNotAnswering: IsNotAnsweringYet(response.StatusCode, text));
            }

            return ParseResponse(text);
        }
    }

    /// <summary>
    /// Told apart from a real refusal: "busy", "unavailable" and a gateway that has nothing behind it yet all
    /// mean try again shortly. Ollama also answers 500 while a model is still being loaded, and says so in the
    /// body -- a status code alone would not catch it.
    /// </summary>
    private static bool IsNotAnsweringYet(System.Net.HttpStatusCode status, string body)
    {
        if (status == System.Net.HttpStatusCode.TooManyRequests ||
            status == System.Net.HttpStatusCode.BadGateway ||
            status == System.Net.HttpStatusCode.ServiceUnavailable ||
            status == System.Net.HttpStatusCode.GatewayTimeout)
        {
            return true;
        }

        if ((int)status < 500 || body == null)
        {
            return false;
        }

        return body.IndexOf("loading", StringComparison.OrdinalIgnoreCase) >= 0
               || body.IndexOf("try again", StringComparison.OrdinalIgnoreCase) >= 0
               || body.IndexOf("not ready", StringComparison.OrdinalIgnoreCase) >= 0
               || body.IndexOf("server busy", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public JsonObject BuildRequestBody(AiChatRequest request)
    {
        var messages = new JsonArray();

        for (var i = 0; i < request.Messages.Count; i++)
        {
            var message = request.Messages[i];
            var content = message.Content ?? string.Empty;

            // Qwen3's documented soft switch. Put on the system prompt so it holds for the whole exchange
            // rather than having to be repeated on every user turn.
            if (_configuration.DisableThinking && i == 0 && message.Role == AiChatMessage.SystemRole)
            {
                content += "\n/no_think";
            }

            var node = new JsonObject
            {
                ["role"] = message.Role,
                ["content"] = content
            };

            if (message.ToolCalls is { Count: > 0 })
            {
                var calls = new JsonArray();
                foreach (var call in message.ToolCalls)
                {
                    calls.Add(new JsonObject
                    {
                        ["id"] = call.Id,
                        ["type"] = "function",
                        ["function"] = new JsonObject
                        {
                            ["name"] = call.Name,
                            ["arguments"] = call.ArgumentsJson ?? "{}"
                        }
                    });
                }

                node["tool_calls"] = calls;
            }

            if (message.Role == AiChatMessage.ToolRole)
            {
                node["tool_call_id"] = message.ToolCallId;
                node["name"] = message.ToolName;
            }

            messages.Add(node);
        }

        var body = new JsonObject
        {
            ["model"] = _configuration.Model,
            ["messages"] = messages,
            ["stream"] = false,
            ["temperature"] = _configuration.Temperature,
            // The answer's own share of the context window, so question plus answer never exceed it.
            ["max_tokens"] = _configuration.ReservedOutputTokens
        };

        if (request.Tools is { Count: > 0 })
        {
            var tools = new JsonArray();
            foreach (var tool in request.Tools)
            {
                tools.Add(new JsonObject
                {
                    ["type"] = "function",
                    ["function"] = new JsonObject
                    {
                        ["name"] = tool.Name,
                        ["description"] = tool.Description,
                        // Cloned: a JsonNode can only have one parent, and definitions are built per request
                        // but a caller could reasonably reuse one.
                        ["parameters"] = tool.Parameters?.DeepClone() ?? new JsonObject { ["type"] = "object" }
                    }
                });
            }

            body["tools"] = tools;
        }

        return body;
    }

    public static AiChatResponse ParseResponse(string json)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new AiChatClientException($"The model returned something that is not JSON: {Truncate(json, 200)}", ex);
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("choices", out var choices)
                || choices.ValueKind != JsonValueKind.Array
                || choices.GetArrayLength() == 0
                || !choices[0].TryGetProperty("message", out var message))
            {
                throw new AiChatClientException($"The model's response has no message: {Truncate(json, 200)}");
            }

            var content = message.TryGetProperty("content", out var contentElement) && contentElement.ValueKind == JsonValueKind.String
                ? contentElement.GetString()
                : null;

            var toolCalls = new List<AiToolCall>();

            if (message.TryGetProperty("tool_calls", out var callsElement) && callsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var call in callsElement.EnumerateArray())
                {
                    if (!call.TryGetProperty("function", out var function)
                        || !function.TryGetProperty("name", out var nameElement)
                        || nameElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    toolCalls.Add(new AiToolCall
                    {
                        Id = call.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String && !string.IsNullOrEmpty(id.GetString())
                            ? id.GetString()
                            : NewCallId(),
                        Name = nameElement.GetString(),
                        ArgumentsJson = ReadArguments(function)
                    });
                }
            }

            content = StripThinking(content);

            // A small model sometimes writes its tool call into the reply text in Qwen's own markup instead of
            // the structured field -- the server's parser missed it. Left alone, the user would see raw JSON and
            // the tool would never run.
            if (toolCalls.Count == 0 && content != null && content.Contains("<tool_call>", StringComparison.Ordinal))
            {
                toolCalls.AddRange(ExtractTextToolCalls(content));
                content = TextToolCall.Replace(content, string.Empty).Trim();
            }

            return new AiChatResponse { Content = content, ToolCalls = toolCalls };
        }
    }

    /// <summary>
    /// Removes the model's reasoning from a reply. Handles the complete block, a block cut off before it closed
    /// (the reply ran out of tokens mid-thought, so there is no answer to keep), and a bare closing tag, which
    /// some servers leave when they strip only the opening one.
    /// </summary>
    public static string StripThinking(string content)
    {
        if (content == null)
        {
            return null;
        }

        var result = ThinkBlock.Replace(content, string.Empty);

        var unterminated = result.IndexOf("<think>", StringComparison.OrdinalIgnoreCase);
        if (unterminated >= 0)
        {
            result = result.Substring(0, unterminated);
        }

        const string closingTag = "</think>";
        var orphanClose = result.IndexOf(closingTag, StringComparison.OrdinalIgnoreCase);
        if (orphanClose >= 0)
        {
            result = result.Substring(orphanClose + closingTag.Length);
        }

        return result.Trim();
    }

    private static IEnumerable<AiToolCall> ExtractTextToolCalls(string content)
    {
        foreach (Match match in TextToolCall.Matches(content))
        {
            AiToolCall parsed = null;
            try
            {
                using var call = JsonDocument.Parse(match.Groups[1].Value);
                if (call.RootElement.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String)
                {
                    parsed = new AiToolCall
                    {
                        Id = NewCallId(),
                        Name = name.GetString(),
                        ArgumentsJson = call.RootElement.TryGetProperty("arguments", out var args)
                            ? (args.ValueKind == JsonValueKind.String ? args.GetString() : args.GetRawText())
                            : "{}"
                    };
                }
            }
            catch (JsonException)
            {
                // Not a call after all; leave it out rather than guess.
            }

            if (parsed != null)
            {
                yield return parsed;
            }
        }
    }

    /// <summary>OpenAI sends arguments as a JSON string; some Ollama versions send the object itself.</summary>
    private static string ReadArguments(JsonElement function)
    {
        if (!function.TryGetProperty("arguments", out var arguments))
        {
            return "{}";
        }

        return arguments.ValueKind switch
        {
            JsonValueKind.String => string.IsNullOrWhiteSpace(arguments.GetString()) ? "{}" : arguments.GetString(),
            JsonValueKind.Object => arguments.GetRawText(),
            _ => "{}"
        };
    }

    private static string NewCallId() => "call_" + Guid.NewGuid().ToString("N").Substring(0, 12);

    private static string Truncate(string value, int length) =>
        value == null || value.Length <= length ? value : value.Substring(0, length) + "...";
}
