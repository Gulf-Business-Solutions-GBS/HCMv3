using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.UI;
using HCMv3.Integrations.Services;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Costing;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// <see cref="PostJvRunner"/>: legacy <c>CreatePostJV</c> (<c>uscCostingFile.ascx.cs</c> lines 765-1078) as a
/// background run -- one journal per employee for an off-cycle run, the Regular salary/provision split, the
/// server-side checks before the first journal, partial postings recorded rather than undone, and no second
/// posting when the job is executed again.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PostJvRunner_Tests : FinancePostingTestBase
{
    private readonly IJournalVoucherBuilder _builder = Substitute.For<IJournalVoucherBuilder>();
    private readonly IFinancePostingClient _client = Substitute.For<IFinancePostingClient>();
    private readonly List<(string Header, List<JournalVoucherLine> Lines)> _posted = new();
    private int _batchCounter;

    private FinanceRunLog _runLog;

    private PostJvRunner CreateRunner(int[] areaEmployeeIds, bool switchOn = true, bool separateProvision = false)
    {
        var postingSwitch = Substitute.For<ID365FinancePostingSwitch>();
        postingSwitch.IsPostingAllowedAsync().Returns(switchOn);

        var localization = LocalIocManager.Resolve<ILocalizationManager>();

        var preconditions = new PostJvPreconditions(Repo<PayrollArea>(), _builder, postingSwitch)
        {
            LocalizationManager = localization
        };

        var settings = Substitute.For<ISysSettingsAccessor>();
        settings.GetFIPostingSettingsAsync().Returns(new ManageFIPostingSettings { SeparateProvisionPosting = separateProvision });

        _runLog = CreateRunLog();

        return new PostJvRunner(
            preconditions,
            _builder,
            _client,
            settings,
            Repo<CostRegister>(),
            Repo<HCMv3.PersonnelAdministration.Employee>(),
            ResolverFor(AreaName, areaEmployeeIds),
            _runLog)
        {
            LocalizationManager = localization
        };
    }

    /// <summary>A client that "posts" successfully, reporting the header row as the real client does.</summary>
    private void ClientPostsSuccessfully(string failOnHeaderContaining = null)
    {
        _client.PostJournalAsync(Arg.Any<PayrollArea>(), Arg.Any<List<JournalVoucherLine>>(), Arg.Any<string>(),
                                 Arg.Any<IFinancePostingObserver>())
            .Returns(async call =>
            {
                var header = call.ArgAt<string>(2);
                var observer = call.ArgAt<IFinancePostingObserver>(3);

                if (failOnHeaderContaining != null && header.Contains(failOnHeaderContaining))
                {
                    await observer.OnPostingEventAsync(new FinancePostingEvent
                    {
                        Succeeded = false,
                        Message = "RESPONSE Status Code : 400 (BadRequest) -- D365 refused the header"
                    });
                    throw new UserFriendlyException("Finance journal header creation failed for '" + header + "'.");
                }

                var batch = "JV-" + (++_batchCounter).ToString("000");
                _posted.Add((header, call.ArgAt<List<JournalVoucherLine>>(1)));

                await observer.OnPostingEventAsync(new FinancePostingEvent
                {
                    Succeeded = true,
                    JournalBatchNumber = batch,
                    Message = "Successfull : Journal Header Created (" + header + ") : JournalBatchNumber : " + batch
                });

                return new FinanceJournalPostResult { JournalBatchNumber = batch, BatchCount = 1 };
            });
    }

    private void BuilderReturns(JournalVoucherResult areaVoucher, Dictionary<string, JournalVoucherResult> perEmployee = null)
    {
        _builder.BuildAsync(Arg.Is<CostingRunInput>(i => string.IsNullOrEmpty(i.EmployeeCode))).Returns(areaVoucher);

        foreach (var pair in perEmployee ?? new Dictionary<string, JournalVoucherResult>())
        {
            var code = pair.Key;
            _builder.BuildAsync(Arg.Is<CostingRunInput>(i => i.EmployeeCode == code)).Returns(pair.Value);
        }
    }

    private async Task<FinanceRunJobArgs> OpenRunAsync(CostingRunInput input, JournalVoucherResult previewed)
    {
        var id = await _runLog.OpenAsync(FinanceRunProcess.PostJv, input, "admin");
        return FinanceRunJobArgs.From(input, TenantId, 2, id, JournalVoucherFingerprint.Compute(input, previewed));
    }

    private ExecutionLog RunRecord() => Context.ExecutionLogs.Single();

    // ---------------------------------------------------------------- OffCycle: one journal per employee

    [Fact]
    public async Task Should_Post_One_Journal_Per_Employee_For_An_OffCycle_Run()
    {
        SeedArea();
        var e1 = AddEmployee("E1");
        var e2 = AddEmployee("E2");
        AddCostRegister(e1, PayrollRunType.OffCycle);
        AddCostRegister(e2, PayrollRunType.OffCycle);

        var e1Voucher = Voucher(Line("E1", 100, 0), Line("", 0, 100, "Earning", "Net Pay"));
        var e2Voucher = Voucher(Line("E2", 250, 0), Line("", 0, 250, "Earning", "Net Pay"));
        var area = Voucher(Line("E1", 100, 0), Line("E2", 250, 0), Line("", 0, 350, "Earning", "Net Pay"));
        BuilderReturns(area, new Dictionary<string, JournalVoucherResult> { ["E1"] = e1Voucher, ["E2"] = e2Voucher });
        ClientPostsSuccessfully();

        var runner = CreateRunner(new[] { e1, e2 });
        var input = Input(PayrollRunType.OffCycle);
        await runner.RunAsync(await OpenRunAsync(input, area));

        // Legacy line 977, verbatim header, one per employee, each with only that employee's JV.
        _posted.Select(p => p.Header).ShouldBe(new[]
        {
            "Employee : E1 - Payroll/Salary (OffCycle) For Sep-2026",
            "Employee : E2 - Payroll/Salary (OffCycle) For Sep-2026"
        });
        _posted[0].Lines.ShouldBe(e1Voucher.Lines);
        _posted[1].Lines.ShouldBe(e2Voucher.Lines);

        // Legacy lines 1004-1017: one "Successfull" row per employee, carrying the employee count.
        var employeeRows = LogRows().Where(l => l.EmployeeId != null).ToList();
        employeeRows.Select(r => r.EmployeeId).ShouldBe(new int?[] { e1, e2 });
        employeeRows.ShouldAllBe(r => r.Status == 0 && r.Message == CostEngineService.SuccessMessage && r.NoOfEmployee == 2);

        // The client's header rows are in the same log, employee-less, with the batch numbers.
        LogRows().Count(l => l.EmployeeId == null && l.Message.Contains("JournalBatchNumber : JV-")).ShouldBe(2);

        RunRecord().ExecutionStatus.ShouldBe(ExecutionLogStatus.SUCCESS);
    }

    [Fact]
    public async Task Should_Only_Post_Employees_With_An_OffCycle_Cost_Register_In_The_Area()
    {
        SeedArea();
        var e1 = AddEmployee("E1");
        var regularOnly = AddEmployee("E2");
        var otherArea = AddEmployee("E3");
        AddCostRegister(e1, PayrollRunType.OffCycle);
        AddCostRegister(regularOnly, PayrollRunType.Regular);
        AddCostRegister(otherArea, PayrollRunType.OffCycle);

        var e1Voucher = Voucher(Line("E1", 100, 0), Line("", 0, 100));
        BuilderReturns(e1Voucher, new Dictionary<string, JournalVoucherResult> { ["E1"] = e1Voucher });
        ClientPostsSuccessfully();

        var runner = CreateRunner(new[] { e1, regularOnly });
        var input = Input(PayrollRunType.OffCycle);
        await runner.RunAsync(await OpenRunAsync(input, e1Voucher));

        _posted.Count.ShouldBe(1);
        _posted[0].Header.ShouldContain("E1");
    }

    /// <summary>Legacy's off-cycle branch never looks at SeparateProvisionPosting: provision lines ride in the employee's one journal.</summary>
    [Fact]
    public async Task Should_Not_Split_Provision_For_OffCycle_Even_When_Separate_Provision_Posting_Is_On()
    {
        SeedArea();
        var e1 = AddEmployee("E1");
        AddCostRegister(e1, PayrollRunType.OffCycle);

        var e1Voucher = Voucher(Line("E1", 100, 0), Line("E1", 30, 0, "Provision"), Line("", 0, 30, "Provision"),
                                Line("", 0, 100));
        BuilderReturns(e1Voucher, new Dictionary<string, JournalVoucherResult> { ["E1"] = e1Voucher });
        ClientPostsSuccessfully();

        var runner = CreateRunner(new[] { e1 }, separateProvision: true);
        var input = Input(PayrollRunType.OffCycle);
        await runner.RunAsync(await OpenRunAsync(input, e1Voucher));

        _posted.Count.ShouldBe(1);
        _posted[0].Lines.Count.ShouldBe(4);
    }

    [Fact]
    public async Task Should_Log_A_Failed_OffCycle_Employee_And_Carry_On_With_The_Next()
    {
        SeedArea();
        var e1 = AddEmployee("E1");
        var e2 = AddEmployee("E2");
        AddCostRegister(e1, PayrollRunType.OffCycle);
        AddCostRegister(e2, PayrollRunType.OffCycle);

        var e1Voucher = Voucher(Line("E1", 100, 0), Line("", 0, 100));
        var e2Voucher = Voucher(Line("E2", 50, 0), Line("", 0, 50));
        var area = Voucher(Line("E1", 100, 0), Line("E2", 50, 0), Line("", 0, 150));
        BuilderReturns(area, new Dictionary<string, JournalVoucherResult> { ["E1"] = e1Voucher, ["E2"] = e2Voucher });
        ClientPostsSuccessfully(failOnHeaderContaining: "E1 -");

        var runner = CreateRunner(new[] { e1, e2 });
        var input = Input(PayrollRunType.OffCycle);
        await runner.RunAsync(await OpenRunAsync(input, area));

        _posted.Single().Header.ShouldContain("E2");

        var e1Row = LogRows().Single(l => l.EmployeeId == e1);
        e1Row.Status.ShouldBe(1);
        e1Row.Message.ShouldContain("Finance journal header creation failed");

        LogRows().Single(l => l.EmployeeId == e2).Status.ShouldBe(0);

        // The header failure row carries D365's own reply.
        LogRows().ShouldContain(l => l.EmployeeId == null && l.Status == 1 && l.Message.Contains("D365 refused the header"));

        RunRecord().ExecutionStatus.ShouldBe(ExecutionLogStatus.PARTIAL);
    }

    [Fact]
    public async Task Should_Stop_Before_The_Next_OffCycle_Employee()
    {
        SeedArea();
        var e1 = AddEmployee("E1");
        var e2 = AddEmployee("E2");
        AddCostRegister(e1, PayrollRunType.OffCycle);
        AddCostRegister(e2, PayrollRunType.OffCycle);

        var e1Voucher = Voucher(Line("E1", 100, 0), Line("", 0, 100));
        var e2Voucher = Voucher(Line("E2", 50, 0), Line("", 0, 50));
        var area = Voucher(Line("E1", 100, 0), Line("E2", 50, 0), Line("", 0, 150));
        BuilderReturns(area, new Dictionary<string, JournalVoucherResult> { ["E1"] = e1Voucher, ["E2"] = e2Voucher });
        ClientPostsSuccessfully();

        var runner = CreateRunner(new[] { e1, e2 });

        // The operator presses Stop while the first employee is being posted.
        _builder.When(b => b.BuildAsync(Arg.Is<CostingRunInput>(i => i.EmployeeCode == "E1")))
                .Do(_ => _runLog.RequestStopAsync().GetAwaiter().GetResult());

        var input = Input(PayrollRunType.OffCycle);
        await runner.RunAsync(await OpenRunAsync(input, area));

        _posted.Count.ShouldBe(1);
        LogRows().ShouldNotContain(l => l.EmployeeId == e2);
        RunRecord().ExecutionStatus.ShouldBe(ExecutionLogStatus.ABORTED);
    }

    // ---------------------------------------------------------------- Regular

    [Fact]
    public async Task Should_Post_A_Regular_Area_As_One_Salary_Journal_With_The_Legacy_Header()
    {
        SeedArea();
        var area = Voucher(Line("E1", 100, 0), Line("E1", 30, 0, "Provision"), Line("", 0, 30, "Provision"),
                           Line("", 0, 100));
        BuilderReturns(area);
        ClientPostsSuccessfully();

        var runner = CreateRunner(Array.Empty<int>());
        var input = Input();
        await runner.RunAsync(await OpenRunAsync(input, area));

        _posted.Single().Header.ShouldBe("Employee Payroll/Salary For Sep-2026");
        _posted.Single().Lines.Count.ShouldBe(4);

        // Legacy lines 848-857: an employee-less success row carrying the line count.
        var closing = LogRows().Last();
        closing.EmployeeId.ShouldBeNull();
        closing.Status.ShouldBe(0);
        closing.NoOfEmployee.ShouldBe(4);
        closing.Message.ShouldContain(Text("PayrollSalaryJvPostedSuccessfully"));
        closing.Message.ShouldContain("JV-001");
    }

    [Fact]
    public async Task Should_Split_Salary_And_Provision_Journals_When_Separate_Provision_Posting_Is_On()
    {
        SeedArea();
        var area = Voucher(Line("E1", 100, 0), Line("E1", 30, 0, "Provision"), Line("", 0, 30, "Provision"),
                           Line("", 0, 100));
        BuilderReturns(area);
        ClientPostsSuccessfully();

        var runner = CreateRunner(Array.Empty<int>(), separateProvision: true);
        var input = Input();
        await runner.RunAsync(await OpenRunAsync(input, area));

        _posted.Select(p => p.Header).ShouldBe(new[]
        {
            "Employee Payroll/Salary For Sep-2026",
            "Employee Provision For Sep-2026"
        });
        _posted[0].Lines.Count.ShouldBe(2);
        _posted[1].Lines.ShouldAllBe(l => l.BillingType == JournalVoucherBuilder.ProvisionBillingTypeLabel);
    }

    /// <summary>
    /// The salary journal reached D365, then the provision journal failed. Legacy left the salary journal in
    /// D365 and logged the failure; nothing is reversed here either, and the failure row names what is in D365.
    /// </summary>
    [Fact]
    public async Task Should_Record_The_Journals_Already_Posted_When_The_Provision_Journal_Fails()
    {
        SeedArea();
        var area = Voucher(Line("E1", 100, 0), Line("E1", 30, 0, "Provision"), Line("", 0, 30, "Provision"),
                           Line("", 0, 100));
        BuilderReturns(area);
        ClientPostsSuccessfully(failOnHeaderContaining: "Provision");

        var runner = CreateRunner(Array.Empty<int>(), separateProvision: true);
        var input = Input();
        await runner.RunAsync(await OpenRunAsync(input, area));

        _posted.Single().Header.ShouldBe("Employee Payroll/Salary For Sep-2026");

        // The salary header row is on record with its batch number...
        LogRows().ShouldContain(l => l.Status == 0 && l.Message.Contains("Employee Payroll/Salary") && l.Message.Contains("JV-001"));

        // ...and the failure row says which journal had already reached D365.
        var failure = LogRows().Last();
        failure.Status.ShouldBe(1);
        failure.Message.ShouldContain("Finance journal header creation failed");
        failure.Message.ShouldContain(Text("FinanceJournalsAlreadyPosted"));
        failure.Message.ShouldContain("JV-001");

        RunRecord().ExecutionStatus.ShouldBe(ExecutionLogStatus.PARTIAL);
    }

    // ---------------------------------------------------------------- Checks before the first journal

    [Fact]
    public async Task Should_Not_Post_When_Dynamics_365_Is_Switched_Off_In_The_Integration_Center()
    {
        SeedArea();
        var area = Voucher(Line("E1", 100, 0), Line("", 0, 100));
        BuilderReturns(area);
        ClientPostsSuccessfully();

        var runner = CreateRunner(Array.Empty<int>(), switchOn: false);
        var input = Input();
        await runner.RunAsync(await OpenRunAsync(input, area));

        _posted.ShouldBeEmpty();
        var row = LogRows().Single();
        row.Status.ShouldBe(1);
        row.Message.ShouldBe(Text("D365FinancePostingSwitchedOff"));
        RunRecord().ExecutionStatus.ShouldBe(ExecutionLogStatus.FAILED);
    }

    /// <summary>The cost register changed between the preview and the run: the JV is no longer the one that was checked.</summary>
    [Fact]
    public async Task Should_Not_Post_When_The_Jv_Changed_Since_The_Preview()
    {
        SeedArea();
        var previewed = Voucher(Line("E1", 100, 0), Line("", 0, 100));
        var now = Voucher(Line("E1", 120, 0), Line("", 0, 120));
        BuilderReturns(now);
        ClientPostsSuccessfully();

        var runner = CreateRunner(Array.Empty<int>());
        var input = Input();
        await runner.RunAsync(await OpenRunAsync(input, previewed));

        _posted.ShouldBeEmpty();
        LogRows().Single().Message.ShouldBe(Text("JournalVoucherPreviewChanged"));
    }

    [Fact]
    public async Task Should_Not_Post_An_Unbalanced_Jv()
    {
        SeedArea();
        var area = Voucher(Line("E1", 100.6, 0), Line("", 0, 100.4));
        BuilderReturns(area);
        ClientPostsSuccessfully();

        var runner = CreateRunner(Array.Empty<int>());
        var input = Input();
        await runner.RunAsync(await OpenRunAsync(input, area));

        _posted.ShouldBeEmpty();
        LogRows().Single().Message.ShouldBe(Text("DebitCreditNotMatched"));
    }

    /// <summary>
    /// ABP executes a job again when a restart left it in its store. For Post JV that would be a second set of
    /// journals in D365, so the second execution posts nothing and closes the run as interrupted.
    /// </summary>
    [Fact]
    public async Task Should_Not_Post_Again_When_The_Job_Is_Executed_A_Second_Time()
    {
        SeedArea();
        var area = Voucher(Line("E1", 100, 0), Line("", 0, 100));
        BuilderReturns(area);
        ClientPostsSuccessfully();

        var runner = CreateRunner(Array.Empty<int>());
        var input = Input();
        var args = await OpenRunAsync(input, area);

        // First execution claims the run and dies before closing it (a restart): simulated by claiming only.
        (await _runLog.TryClaimAsync(args.ExecutionLogId)).ShouldBeTrue();

        await runner.RunAsync(args);

        _posted.ShouldBeEmpty();
        LogRows().Single().Message.ShouldBe(Text("FinanceRunInterrupted"));
        RunRecord().ExecutionStatus.ShouldBe(ExecutionLogStatus.FAILED);
    }

    [Fact]
    public async Task Should_Never_Throw_Even_When_The_Builder_Fails()
    {
        SeedArea();
        _builder.BuildAsync(Arg.Any<CostingRunInput>()).ThrowsAsync(new InvalidOperationException("db down"));

        var runner = CreateRunner(Array.Empty<int>());
        var input = Input();
        var id = await _runLog.OpenAsync(FinanceRunProcess.PostJv, input, "admin");

        await Should.NotThrowAsync(() => runner.RunAsync(FinanceRunJobArgs.From(input, TenantId, 2, id, "abc")));

        LogRows().Single().Message.ShouldBe("db down");
    }

    [Fact]
    public void Should_Build_The_Legacy_Journal_Headers()
    {
        PostJvRunner.SalaryJournalHeader(new DateTime(2026, 1, 31)).ShouldBe("Employee Payroll/Salary For Jan-2026");
        PostJvRunner.ProvisionJournalHeader(new DateTime(2026, 1, 31)).ShouldBe("Employee Provision For Jan-2026");
        PostJvRunner.OffCycleJournalHeader("100891", PayrollRunType.OffCycle, new DateTime(2026, 1, 31))
            .ShouldBe("Employee : 100891 - Payroll/Salary (OffCycle) For Jan-2026");
    }
}
