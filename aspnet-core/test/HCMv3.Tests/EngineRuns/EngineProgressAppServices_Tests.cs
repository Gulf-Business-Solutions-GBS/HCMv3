using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Abp.Runtime.Session;
using Abp.Timing;
using HCMv3.EngineRuns;
using HCMv3.EngineRuns.Dto;
using HCMv3.PayrollManagement;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.EngineRuns;

/// <summary>
/// Exercises the progress endpoints through the CONTAINER-resolved app services, i.e. the real HTTP call
/// path with ABP's authorization and unit-of-work interceptors in place. The reader's arithmetic is covered
/// by <c>EngineRunProgressReader_Tests</c>; what is proved here is that the endpoints are correctly wired,
/// scoped to their own engine, and shaped so NSwag can generate usable client proxies.
/// </summary>
public class EngineProgressAppServices_Tests : AppTestBase
{
    private readonly ITimeEngineAppService _timeEngine;
    private readonly IPayrollEngineAppService _payrollEngine;

    public EngineProgressAppServices_Tests()
    {
        _timeEngine = Resolve<ITimeEngineAppService>();
        _payrollEngine = Resolve<IPayrollEngineAppService>();
    }

    [Fact]
    public async Task Should_Serve_An_Empty_Status_Before_Any_Run()
    {
        (await _timeEngine.GetTimeEngineStatus()).HasRun.ShouldBeFalse();
        (await _payrollEngine.GetPayrollEngineStatus()).HasRun.ShouldBeFalse();
    }

    /// <summary>
    /// Each engine's screen must only ever see its own run. Getting this wrong would show payroll figures on
    /// the time screen, which is the kind of bug that gets noticed only after someone acts on it.
    /// </summary>
    [Fact]
    public async Task Should_Keep_The_Two_Engines_Status_Separate()
    {
        await SeedRunAsync(EngineType.TimeEngine, total: 7);
        await SeedRunAsync(EngineType.PayrollEngine, total: 9);

        (await _timeEngine.GetTimeEngineStatus()).TotalEmployees.ShouldBe(7);
        (await _payrollEngine.GetPayrollEngineStatus()).TotalEmployees.ShouldBe(9);
    }

    [Fact]
    public async Task Should_Serve_The_Per_Employee_Grid_Through_Both_Engines()
    {
        var timeRunId = await SeedRunAsync(EngineType.TimeEngine, total: 2);
        await SeedEmployeeAsync(timeRunId, 1, "T001", succeeded: true, message: "Successfull");
        await SeedEmployeeAsync(timeRunId, 2, "T002", succeeded: false, message: "Employee Sub Group not found for Employee.");

        var payrollRunId = await SeedRunAsync(EngineType.PayrollEngine, total: 1);
        await SeedEmployeeAsync(payrollRunId, 1, "P001", succeeded: false, message: "Payroll Control Record is locked.");

        var timeRows = await _timeEngine.GetTimeEngineRunEmployees(new HCMv3.TimeManagement.Dto.GetTimeEngineRunEmployeesInput());
        timeRows.TotalCount.ShouldBe(2);
        timeRows.Items.Select(i => i.EmployeeCode).ShouldBe(new[] { "T001", "T002" });
        timeRows.Items[1].Message.ShouldBe("Employee Sub Group not found for Employee.");

        var payrollFailures = await _payrollEngine.GetPayrollEngineRunEmployees(
            new GetEngineRunEmployeesInput { OnlyFailed = true });
        payrollFailures.TotalCount.ShouldBe(1);
        payrollFailures.Items[0].EmployeeCode.ShouldBe("P001");
    }

    [Fact]
    public async Task Should_Request_Stop_Through_Both_Engines()
    {
        var timeRunId = await SeedRunAsync(EngineType.TimeEngine);
        var payrollRunId = await SeedRunAsync(EngineType.PayrollEngine);

        await _timeEngine.StopTimeEngine();

        await UsingDbContextAsync(async context =>
        {
            (await context.EngineRuns.FirstAsync(r => r.Id == timeRunId)).StopRequested.ShouldBeTrue();
            (await context.EngineRuns.FirstAsync(r => r.Id == payrollRunId)).StopRequested.ShouldBeFalse(
                "Stopping one engine must never stop the other.");
        });

        await _payrollEngine.StopPayrollEngine();

        (await _payrollEngine.GetPayrollEngineStatus()).StopRequested.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Not_Throw_When_Stopping_With_Nothing_Running()
    {
        await _timeEngine.StopTimeEngine();
        await _payrollEngine.StopPayrollEngine();
    }

    [Fact]
    public async Task Should_Clear_Progress_Through_Both_Engines()
    {
        await SeedRunAsync(EngineType.TimeEngine, status: EngineRunStatus.Completed);
        await SeedRunAsync(EngineType.PayrollEngine, status: EngineRunStatus.Completed);

        await _timeEngine.ClearTimeEngineProgress();

        (await _timeEngine.GetTimeEngineStatus()).HasRun.ShouldBeFalse();
        (await _payrollEngine.GetPayrollEngineStatus()).HasRun.ShouldBeTrue(
            "Clearing one engine's history must not touch the other engine's.");

        await _payrollEngine.ClearPayrollEngineProgress();
        (await _payrollEngine.GetPayrollEngineStatus()).HasRun.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Age_Out_Killed_Runs_Through_Both_Engines()
    {
        await SeedRunAsync(
            EngineType.TimeEngine,
            lastHeartbeatAt: Clock.Now.AddSeconds(-(EngineRunConsts.StaleHeartbeatSeconds + 1)));

        (await _timeEngine.AbandonStaleTimeEngineRuns()).ShouldBe(1);
        (await _payrollEngine.AbandonStalePayrollEngineRuns()).ShouldBe(0);

        (await _timeEngine.GetTimeEngineStatus()).Status.ShouldBe(EngineRunStatus.Abandoned);
    }

    /// <summary>
    /// The repo's NSwag rule, asserted rather than trusted: a <c>Get*</c> method taking a CUSTOM input DTO
    /// must be <c>[HttpPost]</c>, or ABP routes it as GET, NSwag flattens the DTO into query-string
    /// parameters, and <c>GetEngineRunEmployeesInput</c> is never emitted into <c>service-proxies.ts</c>.
    /// This has bitten the project repeatedly.
    /// </summary>
    [Theory]
    [InlineData(typeof(TimeEngineAppService), nameof(ITimeEngineAppService.GetTimeEngineRunEmployees))]
    [InlineData(typeof(PayrollEngineAppService), nameof(IPayrollEngineAppService.GetPayrollEngineRunEmployees))]
    public void Should_Post_Grid_Endpoints_That_Take_A_Custom_Input_Dto(System.Type serviceType, string methodName)
    {
        serviceType.GetMethod(methodName)
            .GetCustomAttribute<HttpPostAttribute>()
            .ShouldNotBeNull();
    }

    /// <summary>
    /// The status poll must stay a GET (no custom input DTO), matching <c>GetProjectCostEngineStatus</c> --
    /// the idiom the existing Angular polling component is already built around. The Time Engine's takes the
    /// selected payroll area as a plain optional string, which keeps it a GET (a query-string argument).
    /// </summary>
    [Theory]
    [InlineData(typeof(TimeEngineAppService), nameof(ITimeEngineAppService.GetTimeEngineStatus))]
    [InlineData(typeof(PayrollEngineAppService), nameof(IPayrollEngineAppService.GetPayrollEngineStatus))]
    public void Should_Leave_Status_Polls_As_Http_Get(System.Type serviceType, string methodName)
    {
        var method = serviceType.GetMethod(methodName);

        method.GetParameters().ShouldAllBe(p => p.ParameterType == typeof(string) && p.IsOptional);
        method.GetCustomAttribute<HttpPostAttribute>().ShouldBeNull();
    }

    #region << helpers >>

    private Task<long> SeedRunAsync(
        EngineType engineType,
        EngineRunStatus status = EngineRunStatus.Running,
        int total = 0,
        System.DateTime? lastHeartbeatAt = null)
    {
        return UsingDbContextAsync(async context =>
        {
            var run = new EngineRun
            {
                TenantId = AbpSession.GetTenantId(),
                EngineType = engineType,
                RunKey = System.Guid.NewGuid().ToString("N"),
                Status = status,
                TotalEmployees = total,
                StartedAt = Clock.Now,
                LastHeartbeatAt = lastHeartbeatAt ?? Clock.Now
            };

            context.EngineRuns.Add(run);
            await context.SaveChangesAsync();
            return run.Id;
        });
    }

    private Task SeedEmployeeAsync(long runId, int sequenceNo, string code, bool succeeded, string message)
    {
        return UsingDbContextAsync(async context =>
        {
            context.EngineRunEmployees.Add(new EngineRunEmployee
            {
                TenantId = AbpSession.GetTenantId(),
                EngineRunId = runId,
                SequenceNo = sequenceNo,
                EmployeeCode = code,
                EmployeeName = code + " Name",
                Succeeded = succeeded,
                Message = message,
                StartedAt = Clock.Now,
                CompletedAt = Clock.Now,
                ElapsedMilliseconds = 1500
            });

            await context.SaveChangesAsync();
        });
    }

    #endregion
}
