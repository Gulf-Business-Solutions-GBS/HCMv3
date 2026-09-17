using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using HCMv3.AiAgent.Chat;
using HCMv3.Configuration;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AiAgent;

/// <summary>
/// The wire format, with no model and no network. These cover the ways small local models bend the
/// OpenAI format, because those are what break the assistant in practice rather than the happy path.
/// </summary>
// ReSharper disable once InconsistentNaming
public class HttpAiChatClient_Tests
{
    [Fact]
    public void Should_Read_A_Plain_Reply()
    {
        var response = HttpAiChatClient.ParseResponse(Completion(@"{""role"":""assistant"",""content"":""You have 25 days.""}"));

        response.Content.ShouldBe("You have 25 days.");
        response.ToolCalls.ShouldBeEmpty();
    }

    [Fact]
    public void Should_Read_Tool_Calls_With_Arguments_As_A_String()
    {
        var response = HttpAiChatClient.ParseResponse(Completion(
            @"{""role"":""assistant"",""content"":null,""tool_calls"":[{""id"":""call_1"",""type"":""function"",""function"":{""name"":""get_my_leave_requests"",""arguments"":""{\""includeHistory\"":true}""}}]}"));

        response.ToolCalls.Count.ShouldBe(1);
        response.ToolCalls[0].Id.ShouldBe("call_1");
        response.ToolCalls[0].Name.ShouldBe("get_my_leave_requests");
        response.ToolCalls[0].ArgumentsJson.ShouldBe(@"{""includeHistory"":true}");
    }

    [Fact]
    public void Should_Read_Tool_Calls_With_Arguments_As_An_Object_And_No_Id()
    {
        // Some Ollama versions send the arguments object itself and omit the id.
        var response = HttpAiChatClient.ParseResponse(Completion(
            @"{""role"":""assistant"",""content"":"""",""tool_calls"":[{""function"":{""name"":""list_leave_types"",""arguments"":{""x"":1}}}]}"));

        response.ToolCalls.Count.ShouldBe(1);
        response.ToolCalls[0].ArgumentsJson.ShouldBe(@"{""x"":1}");
        response.ToolCalls[0].Id.ShouldStartWith("call_");
    }

    [Theory]
    [InlineData("<think>let me see</think>\n\nYou have 25 days.", "You have 25 days.")]
    [InlineData("<think>\n\n</think>\n\nمرحباً", "مرحباً")]
    [InlineData("Answer first. <think>ran out of tokens", "Answer first.")]
    [InlineData("reasoning the server half-stripped</think>The answer.", "The answer.")]
    [InlineData("No reasoning at all.", "No reasoning at all.")]
    public void Should_Remove_The_Models_Reasoning(string content, string expected)
    {
        HttpAiChatClient.StripThinking(content).ShouldBe(expected);
    }

    [Fact]
    public void Should_Recover_A_Tool_Call_The_Model_Wrote_As_Text()
    {
        var response = HttpAiChatClient.ParseResponse(Completion(
            @"{""role"":""assistant"",""content"":""<tool_call>\n{\""name\"": \""get_my_leave_balances\"", \""arguments\"": {}}\n</tool_call>""}"));

        response.ToolCalls.Count.ShouldBe(1);
        response.ToolCalls[0].Name.ShouldBe("get_my_leave_balances");
        response.ToolCalls[0].ArgumentsJson.ShouldBe("{}");
        response.Content.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("not json")]
    [InlineData(@"{""error"":""model not found""}")]
    [InlineData(@"{""choices"":[]}")]
    public void Should_Reject_A_Response_That_Is_Not_A_Completion(string body)
    {
        Should.Throw<AiChatClientException>(() => HttpAiChatClient.ParseResponse(body));
    }

    [Fact]
    public void Should_Build_The_Request_In_OpenAI_Format()
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.OK, Completion(@"{""content"":""ok""}")));

        var body = client.BuildRequestBody(new AiChatRequest
        {
            Messages = new List<AiChatMessage>
            {
                AiChatMessage.System("rules"),
                AiChatMessage.User("balance?"),
                AiChatMessage.Assistant(null, new List<AiToolCall> { new() { Id = "c1", Name = "get_my_leave_balances", ArgumentsJson = "{}" } }),
                AiChatMessage.Tool("c1", "get_my_leave_balances", @"{""balances"":[]}")
            },
            Tools = new List<AiToolDefinition>
            {
                new() { Name = "get_my_leave_balances", Description = "d", Parameters = new JsonObject { ["type"] = "object" } }
            }
        });

        body["model"]!.GetValue<string>().ShouldBe("qwen3:4b");
        body["stream"]!.GetValue<bool>().ShouldBeFalse();

        var messages = body["messages"]!.AsArray();
        messages.Count.ShouldBe(4);

        // Thinking switched off through Qwen3's soft switch, on the system prompt only.
        messages[0]!["content"]!.GetValue<string>().ShouldEndWith("/no_think");
        messages[1]!["content"]!.GetValue<string>().ShouldBe("balance?");

        messages[2]!["tool_calls"]![0]!["function"]!["name"]!.GetValue<string>().ShouldBe("get_my_leave_balances");
        messages[3]!["role"]!.GetValue<string>().ShouldBe("tool");
        messages[3]!["tool_call_id"]!.GetValue<string>().ShouldBe("c1");

        body["tools"]![0]!["function"]!["name"]!.GetValue<string>().ShouldBe("get_my_leave_balances");
    }

    [Fact]
    public async Task Should_Post_To_The_Configured_Endpoint_With_The_Api_Key()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Completion(@"{""content"":""hi""}"));
        var client = CreateClient(handler, new Dictionary<string, string>
        {
            ["AiAgent:BaseUrl"] = "https://gpu.example.com/v1/",
            ["AiAgent:Model"] = "qwen3-32b",
            ["AiAgent:ApiKey"] = "secret",
            ["AiAgent:AllowExternalModelHost"] = "true"
        });

        var response = await client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("hi") } });

        response.Content.ShouldBe("hi");
        handler.LastRequest.RequestUri!.ToString().ShouldBe("https://gpu.example.com/v1/chat/completions");
        handler.LastRequest.Headers.Authorization!.Parameter.ShouldBe("secret");
        handler.LastBody.ShouldContain(@"""model"":""qwen3-32b""");
    }

    [Fact]
    public async Task Should_Report_An_Error_Status_As_A_Client_Exception()
    {
        var client = CreateClient(new StubHandler(HttpStatusCode.NotFound, @"{""error"":""model 'qwen3:4b' not found""}"));

        var ex = await Should.ThrowAsync<AiChatClientException>(() =>
            client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("hi") } }));

        ex.IsTimeout.ShouldBeFalse();
        ex.Message.ShouldContain("404");
    }

    [Fact]
    public async Task Should_Report_An_Unreachable_Host_As_A_Client_Exception()
    {
        var client = CreateClient(new StubHandler(new HttpRequestException("connection refused")));

        await Should.ThrowAsync<AiChatClientException>(() =>
            client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("hi") } }));
    }

    [Fact]
    public async Task Should_Report_A_Timeout_As_A_Timeout()
    {
        var client = CreateClient(new StubHandler(new TaskCanceledException("timed out")));

        var ex = await Should.ThrowAsync<AiChatClientException>(() =>
            client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("hi") } }));

        ex.IsTimeout.ShouldBeTrue();
    }

    // ------------------------------------------------------------------ data stays on the network

    [Theory]
    [InlineData("http://localhost:11434/v1")]
    [InlineData("http://127.0.0.1:11434/v1")]
    [InlineData("http://10.0.0.5:11434/v1")]
    [InlineData("http://172.20.1.9/v1")]
    [InlineData("http://192.168.1.20:8000/v1")]
    [InlineData("http://[::1]:11434/v1")]
    public async Task Should_Allow_A_Model_On_This_Machine_Or_The_Private_Network(string baseUrl)
    {
        var handler = new StubHandler(HttpStatusCode.OK, Completion(@"{""content"":""hi""}"));
        var client = CreateClient(handler, new Dictionary<string, string> { ["AiAgent:BaseUrl"] = baseUrl });

        (await client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("hi") } })).Content.ShouldBe("hi");
    }

    [Theory]
    [InlineData("https://api.openai.com/v1")]
    [InlineData("https://dashscope.aliyuncs.com/compatible-mode/v1")]
    [InlineData("http://8.8.8.8/v1")]
    [InlineData("http://172.32.0.1/v1")]
    [InlineData("http://11.0.0.1/v1")]
    public async Task Should_Refuse_A_Model_Outside_The_Network_Without_Sending_Anything(string baseUrl)
    {
        var handler = new StubHandler(HttpStatusCode.OK, Completion(@"{""content"":""hi""}"));
        var client = CreateClient(handler, new Dictionary<string, string> { ["AiAgent:BaseUrl"] = baseUrl });
        // What the public names really resolve to, without depending on the test machine's DNS.
        client.HostResolver = _ => Task.FromResult(new[] { System.Net.IPAddress.Parse("104.18.33.45") });

        var ex = await Should.ThrowAsync<AiChatClientException>(() =>
            client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("my salary is 1200 KD") } }));

        ex.Message.ShouldContain("may not be sent there");
        handler.LastRequest.ShouldBeNull(); // nothing left the server
    }

    [Fact]
    public async Task Should_Refuse_A_Local_Sounding_Name_That_Resolves_To_A_Public_Address()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Completion(@"{""content"":""hi""}"));
        var client = CreateClient(handler, new Dictionary<string, string> { ["AiAgent:BaseUrl"] = "http://gpu-server:11434/v1" });
        client.HostResolver = _ => Task.FromResult(new[] { System.Net.IPAddress.Parse("10.0.0.7"), System.Net.IPAddress.Parse("203.0.113.9") });

        await Should.ThrowAsync<AiChatClientException>(() =>
            client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("hi") } }));

        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Allow_A_Private_Server_Name()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Completion(@"{""content"":""hi""}"));
        var client = CreateClient(handler, new Dictionary<string, string> { ["AiAgent:BaseUrl"] = "http://gpu-server:11434/v1" });
        client.HostResolver = _ => Task.FromResult(new[] { System.Net.IPAddress.Parse("10.0.0.7") });

        (await client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("hi") } })).Content.ShouldBe("hi");
    }

    [Fact]
    public async Task Should_Refuse_A_Name_That_Cannot_Be_Resolved()
    {
        var handler = new StubHandler(HttpStatusCode.OK, Completion(@"{""content"":""hi""}"));
        var client = CreateClient(handler, new Dictionary<string, string> { ["AiAgent:BaseUrl"] = "http://no-such-host:11434/v1" });
        client.HostResolver = _ => throw new System.Net.Sockets.SocketException(11001);

        await Should.ThrowAsync<AiChatClientException>(() =>
            client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("hi") } }));

        handler.LastRequest.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_Follow_A_Redirect_From_The_Model_Host()
    {
        // A local host answering "moved" must not get the request forwarded anywhere; it is simply an error.
        var handler = new StubHandler(HttpStatusCode.TemporaryRedirect, "");
        var client = CreateClient(handler);

        await Should.ThrowAsync<AiChatClientException>(() =>
            client.CompleteAsync(new AiChatRequest { Messages = { AiChatMessage.User("hi") } }));
    }

    [Fact]
    public void The_Production_Handler_Should_Not_Follow_Redirects_Or_Use_A_Proxy()
    {
        using var provider = new AiModelHttpHandlerProvider();

        var field = typeof(AiModelHttpHandlerProvider).GetField("_handler",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var handler = (System.Net.Http.SocketsHttpHandler)field!.GetValue(provider);

        handler!.AllowAutoRedirect.ShouldBeFalse();
        handler.UseProxy.ShouldBeFalse();
    }

    [Fact]
    public void External_Hosts_Should_Be_Off_Unless_Explicitly_Switched_On()
    {
        var accessor = Substitute.For<IAppConfigurationAccessor>();
        accessor.Configuration.Returns(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>()).Build());

        new AiAgentConfiguration(accessor).AllowExternalModelHost.ShouldBeFalse();
    }

    private static string Completion(string messageJson) => @"{""choices"":[{""index"":0,""message"":" + messageJson + "}]}";

    private static HttpAiChatClient CreateClient(StubHandler handler, Dictionary<string, string> settings = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings ?? new Dictionary<string, string>())
            .Build();

        var accessor = Substitute.For<IAppConfigurationAccessor>();
        accessor.Configuration.Returns(configuration);

        return new HttpAiChatClient(new AiModelHttpHandlerProvider(handler), new AiAgentConfiguration(accessor));
    }

    private class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;
        private readonly Exception _exception;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        public StubHandler(Exception exception)
        {
            _exception = exception;
        }

        public HttpRequestMessage LastRequest { get; private set; }

        public string LastBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content == null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            if (_exception != null)
            {
                throw _exception;
            }

            return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
        }
    }
}
