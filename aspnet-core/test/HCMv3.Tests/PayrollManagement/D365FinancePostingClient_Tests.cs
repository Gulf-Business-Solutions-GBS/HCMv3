using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.UI;
using HCMv3.Integrations;
using HCMv3.Integrations.Services;
using HCMv3.OrganizationManagement;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Costing;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// <see cref="D365FinancePostingClient"/> against a scripted D365: legacy <c>PostTablewiseJV_D360</c>,
/// <c>PostJournalHeader</c> and <c>PostMultiJournalLines</c> (<c>uscCostingFile.ascx.cs</c> lines 1079-1495).
/// </summary>
// ReSharper disable once InconsistentNaming
public class D365FinancePostingClient_Tests : FinancePostingTestBase
{
    private const string TokenUrl = "https://login.test/token";
    private const string HeaderUrl = "https://d365.test/data/LedgerJournalHeaders";
    private const string LineUrl = "https://d365.test/data/$batch";

    private readonly ScriptedHandler _handler = new();
    private readonly List<FinancePostingEvent> _events = new();

    private D365FinancePostingClient Create(bool switchOn = true, int changesetLimit = 1)
    {
        var config = Substitute.For<ID365FinanceConfigReader>();
        config.GetAsync(Arg.Any<string>()).Returns(call => call.Arg<string>() switch
        {
            D365FinanceSettingKeys.RequestUrl => TokenUrl,
            D365FinanceSettingKeys.RequestUrlJournalHeader => HeaderUrl,
            D365FinanceSettingKeys.RequestUrlJournalLine => LineUrl,
            D365FinanceSettingKeys.ChangesetLimit => changesetLimit.ToString(),
            _ => "value"
        });

        var postingSwitch = Substitute.For<ID365FinancePostingSwitch>();
        postingSwitch.IsPostingAllowedAsync().Returns(switchOn);

        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient(_handler, disposeHandler: false));

        return new D365FinancePostingClient(config, postingSwitch, factory, Repo<CostElement>())
        {
            LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>()
        };
    }

    private IFinancePostingObserver Observer()
    {
        var observer = Substitute.For<IFinancePostingObserver>();
        observer.OnPostingEventAsync(Arg.Do<FinancePostingEvent>(e => _events.Add(e))).Returns(Task.CompletedTask);
        return observer;
    }

    private static List<JournalVoucherLine> TwoLines() => new() { Line("E1", 10, 0), Line("", 0, 10) };

    private void TokenAndHeaderSucceed()
    {
        _handler.Respond(TokenUrl, HttpStatusCode.OK, "{\"access_token\":\"tok\"}");
        _handler.Respond(HeaderUrl, HttpStatusCode.OK, "{\"JournalBatchNumber\":\"JV-777\",\"dataAreaId\":\"tgtc\"}");
    }

    [Fact]
    public async Task Should_Refuse_Before_Any_Request_When_Switched_Off()
    {
        var ex = await Should.ThrowAsync<UserFriendlyException>(() =>
            Create(switchOn: false).PostJournalAsync(new PayrollArea(), TwoLines(), "H", Observer()));

        ex.Message.ShouldBe(Text("D365FinancePostingSwitchedOff"));
        _handler.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Report_The_Header_And_Every_Batch_With_Its_Batch_Number()
    {
        TokenAndHeaderSucceed();
        _handler.Respond(LineUrl, HttpStatusCode.OK, "--batchresponse\r\nHTTP/1.1 201 Created\r\n");
        _handler.Respond(LineUrl, HttpStatusCode.OK, "--batchresponse\r\nHTTP/1.1 201 Created\r\n");

        var result = await Create().PostJournalAsync(new PayrollArea(), TwoLines(), "Employee Payroll/Salary For Sep-2026", Observer());

        result.JournalBatchNumber.ShouldBe("JV-777");
        result.BatchCount.ShouldBe(2);
        result.FailedBatchCount.ShouldBe(0);

        _events.Count.ShouldBe(3);
        _events[0].Message.ShouldBe("Successfull : Journal Header Created (Employee Payroll/Salary For Sep-2026) : JournalBatchNumber : JV-777");
        _events[1].Message.ShouldContain("Successfull : JournalBatchNumber : JV-777");
        _events[1].Message.ShouldContain(",ChangeSet : 1-1");

        // Content ids run on across batches, as legacy's ContentId did.
        _events[2].Message.ShouldContain(",SrNo : 2");
        _events[2].Message.ShouldContain(",ChangeSet : 2-2");
        _handler.Bodies(LineUrl).Last().ShouldContain("Content-ID: 2");
    }

    /// <summary>Legacy PostMultiJournalLines logged a failed batch and returned; the next batch still went.</summary>
    [Fact]
    public async Task Should_Log_A_Rejected_Batch_With_The_Full_D365_Reply_And_Post_The_Next_One()
    {
        TokenAndHeaderSucceed();
        _handler.Respond(LineUrl, HttpStatusCode.BadRequest, "{\"Message\":\"Account 7101 is not valid for company tgtc\"}");
        _handler.Respond(LineUrl, HttpStatusCode.OK, "HTTP/1.1 201 Created");

        var result = await Create().PostJournalAsync(new PayrollArea(), TwoLines(), "H", Observer());

        result.BatchCount.ShouldBe(2);
        result.FailedBatchCount.ShouldBe(1);

        var failed = _events.Single(e => !e.Succeeded);
        failed.JournalBatchNumber.ShouldBe("JV-777");
        failed.Message.ShouldContain("Account 7101 is not valid for company tgtc");
        failed.Message.ShouldContain("Error in JournalBatchNumber : JV-777");
        failed.Message.ShouldContain(D365FinancePostingClient.JsonSeparator);
        failed.Message.ShouldContain("\"AccountDisplayValue\"");
        failed.Message.ShouldContain("Bearer [REDACTED]");
        failed.Message.ShouldNotContain("tok\"");

        _events.Last().Succeeded.ShouldBeTrue();
    }

    /// <summary>OData answers the batch 200 even when a changeset inside it failed; that batch is reported as failed.</summary>
    [Fact]
    public async Task Should_Treat_A_Failed_Changeset_Inside_A_200_Batch_As_A_Failed_Batch()
    {
        TokenAndHeaderSucceed();
        _handler.Respond(LineUrl, HttpStatusCode.OK,
            "--batchresponse_1\r\nContent-Type: application/http\r\n\r\nHTTP/1.1 400 Bad Request\r\n\r\n{\"error\":\"Dimension Brand is closed\"}");

        var result = await Create(changesetLimit: 10).PostJournalAsync(new PayrollArea(), TwoLines(), "H", Observer());

        result.FailedBatchCount.ShouldBe(1);
        _events.Single(e => !e.Succeeded).Message.ShouldContain("Dimension Brand is closed");
    }

    [Fact]
    public async Task Should_Throw_And_Report_D365s_Reply_When_The_Header_Is_Refused()
    {
        _handler.Respond(TokenUrl, HttpStatusCode.OK, "{\"access_token\":\"tok\"}");
        _handler.Respond(HeaderUrl, HttpStatusCode.BadRequest, "{\"Message\":\"Journal name General does not exist\"}");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            Create().PostJournalAsync(new PayrollArea(), TwoLines(), "H", Observer()));

        var failed = _events.Single();
        failed.Succeeded.ShouldBeFalse();
        failed.Message.ShouldContain("Journal name General does not exist");
        _handler.Bodies(LineUrl).ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Throw_With_The_Sign_In_Reply_When_The_Token_Is_Refused()
    {
        _handler.Respond(TokenUrl, HttpStatusCode.Unauthorized, "{\"error\":\"invalid_client\",\"error_description\":\"AADSTS7000215\"}");

        var ex = await Should.ThrowAsync<UserFriendlyException>(() =>
            Create().PostJournalAsync(new PayrollArea(), TwoLines(), "H", Observer()));

        ex.Message.ShouldContain("AADSTS7000215");
        _events.ShouldBeEmpty();
    }

    /// <summary>Answers each URL with its queued replies, in order, and records what was sent.</summary>
    private class ScriptedHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, Queue<(HttpStatusCode Status, string Body)>> _replies = new();

        public List<(string Url, string Body)> Requests { get; } = new();

        public void Respond(string url, HttpStatusCode status, string body)
        {
            if (!_replies.TryGetValue(url, out var queue))
            {
                queue = new Queue<(HttpStatusCode, string)>();
                _replies[url] = queue;
            }

            queue.Enqueue((status, body));
        }

        public IEnumerable<string> Bodies(string url) => Requests.Where(r => r.Url == url).Select(r => r.Body);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri.ToString();
            var body = request.Content == null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((url, body));

            if (!_replies.TryGetValue(url, out var queue) || queue.Count == 0)
            {
                throw new InvalidOperationException("Unexpected request to " + url);
            }

            var (status, reply) = queue.Dequeue();
            return new HttpResponseMessage(status) { Content = new StringContent(reply, Encoding.UTF8, "application/json") };
        }
    }
}
