using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HCMv3.ApiClient;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.MobileClient;

public class ConnectRetryHandler_Tests
{
    private static Exception ConnectTimeout() =>
        new TaskCanceledException("The operation was canceled.",
            new TimeoutException("A connection could not be established within the configured ConnectTimeout."));

    private static Exception ConnectionRefused() =>
        new HttpRequestException(HttpRequestError.ConnectionError, "No connection could be made.");

    /// <summary>Plays back a scripted list of outcomes: an exception to throw, or null for a 200 response.</summary>
    private class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<Exception> _outcomes;

        public ScriptedHandler(params Exception[] outcomes)
        {
            _outcomes = new Queue<Exception>(outcomes);
        }

        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var outcome = _outcomes.Count > 0 ? _outcomes.Dequeue() : null;
            if (outcome != null)
            {
                throw outcome;
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }

    private static async Task<(ScriptedHandler inner, Exception error)> SendAsync(int maxAttempts, params Exception[] outcomes)
    {
        var inner = new ScriptedHandler(outcomes);
        using var client = new HttpClient(new ConnectRetryHandler(maxAttempts) { InnerHandler = inner });
        try
        {
            await client.PostAsync("https://server.example/api/services/app/MyGeoPunch/RecordMyGeoPunch", new StringContent("{}"));
            return (inner, null);
        }
        catch (Exception ex)
        {
            return (inner, ex);
        }
    }

    [Fact]
    public async Task Should_Retry_When_The_Connection_Times_Out()
    {
        var (inner, error) = await SendAsync(3, ConnectTimeout());

        error.ShouldBeNull();
        inner.Calls.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Retry_When_The_Connection_Is_Refused()
    {
        var (inner, error) = await SendAsync(3, ConnectionRefused(), ConnectionRefused());

        error.ShouldBeNull();
        inner.Calls.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Give_Up_After_The_Last_Attempt()
    {
        var (inner, error) = await SendAsync(3, ConnectTimeout(), ConnectTimeout(), ConnectTimeout(), null);

        error.ShouldNotBeNull();
        inner.Calls.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Not_Retry_A_Failure_After_The_Request_Reached_The_Server()
    {
        var (inner, error) = await SendAsync(3, new HttpRequestException(HttpRequestError.ResponseEnded, "The response ended prematurely."));

        error.ShouldNotBeNull();
        inner.Calls.ShouldBe(1);
    }

    [Fact]
    public void Should_Not_Treat_A_Cancelled_Caller_As_A_Connect_Failure()
    {
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();

        ConnectRetryHandler.IsConnectFailure(ConnectTimeout(), cancelled.Token).ShouldBeFalse();
    }

    [Fact]
    public void Should_Reject_Fewer_Than_One_Attempt()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new ConnectRetryHandler(0));
    }
}
