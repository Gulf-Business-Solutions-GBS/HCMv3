using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.EngineRuns;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// SAVE IN BATCHES and NO AUTOMATIC RETRY on the everyday Payroll Engine run -- Go-Live Setup's IT11 fix
/// applied to <see cref="PayrollEngineBackgroundJob"/>: the employees of a run go through ten at a time, each
/// batch saved on its own, the run is still ONE progress row, and a failure is recorded on that row instead of
/// being thrown back at ABP's job manager (which would run the payroll a second time).
///
/// <para>Over a company Go-Live Setup has loaded and paid for September 2026 (made-up figures).</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollEngineBatches_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime September = new(2026, 9, 30);
    private static readonly DateTime Joined = new(2020, 1, 15);

    private readonly IGoLiveSetupAppService _goLive;

    public PayrollEngineBatches_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- batches

    /// <summary>
    /// The proof the batches change nothing: the same month run in one unit of work and then in batches of two
    /// (three employees -- a full batch and a part one) gives every employee the same lines.
    /// </summary>
    [Fact]
    public async Task Should_Calculate_The_Same_Registers_In_Batches_As_In_One_Unit_Of_Work()
    {
        await GivenEmployeesPaidForSeptember("E001", "E002", "E003");

        var single = await RunEngineAsync(commitBatchSize: 0);
        var inOne = RegisterLines();

        var batched = await RunEngineAsync(commitBatchSize: 2);
        var inBatches = RegisterLines();

        single.EmployeeResults.ShouldAllBe(r => r.Succeeded);
        batched.EmployeeResults.ShouldAllBe(r => r.Succeeded);
        batched.EmployeeResults.Select(r => r.EmployeeCode).ShouldBe(single.EmployeeResults.Select(r => r.EmployeeCode));
        inOne.ShouldNotBeEmpty();
        inBatches.ShouldBe(inOne);
    }

    /// <summary>However many batches, the screen follows one run from start to finish.</summary>
    [Fact]
    public async Task Should_Report_A_Batched_Run_On_One_Progress_Row()
    {
        await GivenEmployeesPaidForSeptember("E001", "E002", "E003");
        var runsBefore = PayrollEngineRuns().Count;

        await RunEngineAsync(commitBatchSize: 2);

        var runs = PayrollEngineRuns();
        runs.Count.ShouldBe(runsBefore + 1);
        var run = runs.Last();
        run.Status.ShouldBe(EngineRunStatus.Completed);
        run.TotalEmployees.ShouldBe(3);
        run.SucceededEmployees.ShouldBe(3);
    }

    /// <summary>The everyday job saves in batches of ten -- the Go-Live number.</summary>
    [Fact]
    public void Should_Use_The_Go_Live_Batch_Size()
    {
        PayrollEngineBatches.Size.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Run_A_Whole_Payroll_Area_Through_The_Job()
    {
        await GivenEmployeesPaidForSeptember("E001", "E002");
        var before = RegisterLines();

        await RunJobAsync(DefaultArea);

        var run = PayrollEngineRuns().Last();
        run.Status.ShouldBe(EngineRunStatus.Completed);
        run.SucceededEmployees.ShouldBe(2);
        RegisterLines().ShouldBe(before, "a re-run of the same month pays the same");
    }

    // ---------------------------------------------------------------- no automatic retry

    /// <summary>
    /// A run the engine refuses before it begins -- here, a payroll area with no control record -- is not thrown
    /// back at the job manager (which would retry it as a second payroll) but written onto a progress row as
    /// Failed with the reason, where the screen shows it.
    /// </summary>
    [Fact]
    public async Task Should_Record_A_Refused_Run_As_Failed_And_Not_Throw()
    {
        await GivenEmployeesPaidForSeptember("E001");

        await Should.NotThrowAsync(() => RunJobAsync("Nowhere"));

        var run = PayrollEngineRuns().Last();
        run.Status.ShouldBe(EngineRunStatus.Failed);
        run.FailureMessage.ShouldContain("Payroll Control Record not found");
        run.RunKey.ShouldBe(PayrollEngineService.BuildRunKey("Nowhere"));
    }

    // ---------------------------------------------------------------- arrange / act / read back

    private async Task GivenEmployeesPaidForSeptember(params string[] codes)
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }

        await _goLive.LoadEmployees(new GoLiveLoadEmployeesInput
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = DefaultArea,
            Employees = codes
                .Select((code, i) => new GoLiveEmployeeRowDto
                {
                    Code = code,
                    FullName = "Employee " + code,
                    JoiningDate = Joined,
                    CivilId = "29001150" + (1000 + i),
                    BasicSalary = 500 + (i * 125),
                })
                .ToList(),
        });

        var run = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);
    }

    private async Task<PayrollEngineRunResult> RunEngineAsync(int commitBatchSize)
    {
        PayrollEngineRunResult result = null;

        await WithUnitOfWorkAsync(async () =>
        {
            result = await Resolve<IPayrollEngineService>().RunAsync(new PayrollEngineRunInput
            {
                PayrollRunType = PayrollRunType.Regular,
                PayInDate = September,
                PayrollAreaName = DefaultArea,
                RegisterTarget = PayrollRegisterTarget.Live,
                CommitBatchSize = commitBatchSize
            });
        });

        return result;
    }

    private async Task RunJobAsync(string payrollAreaName)
    {
        await Resolve<PayrollEngineBackgroundJob>().ExecuteAsync(new PayrollEngineJobArgs
        {
            TenantId = TenantId,
            UserId = AbpSession.UserId,
            PayrollRunType = PayrollRunType.Regular,
            PayInDate = September,
            PayrollAreaName = payrollAreaName,
            RegisterTarget = PayrollRegisterTarget.Live,
        });
    }

    /// <summary>Every current live line of September, by employee code and wage code, with its day rows.</summary>
    private List<string> RegisterLines() => UsingDbContext(context =>
    {
        var lines = (from line in context.SalaryRegisterDetails
                     join register in context.SalaryRegisters on line.SalaryRegisterId equals register.Id
                     join employee in context.Employees on register.EmployeeId equals employee.Id
                     join wageType in context.WageTypes on line.WageTypeId equals wageType.Id
                     where register.TenantId == TenantId && !register.IsDeleted && !line.IsDeleted && register.InPeriod == September
                     select new
                     {
                         line.Id,
                         employee.Code,
                         register.ForPeriod,
                         register.PayrollResultType,
                         WageCode = wageType.Code,
                         line.Rate,
                         line.Number,
                         line.Amount
                     })
            .ToList();

        var lineIds = lines.Select(l => l.Id).ToList();
        var days = context.SalaryRegisterDaysDetails
            .Where(d => !d.IsDeleted && lineIds.Contains(d.SalaryRegisterDetailId))
            .ToList();

        return lines
            .Select(l => $"{l.Code}|{l.ForPeriod:yyyyMMdd}|{l.PayrollResultType}|{l.WageCode}|{l.Rate:R}|{l.Number:R}|{l.Amount:R}|"
                         + string.Join(";", days.Where(d => d.SalaryRegisterDetailId == l.Id)
                             .OrderBy(d => d.Period)
                             .Select(d => $"{d.Period:yyyyMMdd}:{d.Rate:R}:{d.Number:R}:{d.Amount:R}")))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToList();
    });

    private List<EngineRun> PayrollEngineRuns() => UsingDbContext(context => context.EngineRuns
        .Where(r => r.TenantId == TenantId && r.EngineType == EngineType.PayrollEngine)
        .OrderBy(r => r.Id)
        .ToList());
}
