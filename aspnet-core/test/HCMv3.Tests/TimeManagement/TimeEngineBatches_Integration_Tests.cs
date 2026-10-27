using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.EngineRuns;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

/// <summary>
/// The Time Engine fixes proved against a real database, over employees Go-Live Setup loads with its working time
/// (planned time, 08:00-17:00, Friday off -- made-up people, no salaries involved):
/// <list type="bullet">
/// <item><description>SAVE IN BATCHES gives exactly the time registers one unit of work gives;</description></item>
/// <item><description>the job runs a whole area in batches on ONE progress row and closes it Completed;</description></item>
/// <item><description>the PAYROLL AREA FILTER: only the selected area's employees get registers;</description></item>
/// <item><description>NO AUTOMATIC RETRY: a refused run is recorded Failed and the job does not throw.</description></item>
/// </list>
/// These seed three reference packs, so each test takes a while -- run the class on its own.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeEngineBatches_Integration_Tests : AppTestBase
{
    private const string AreaA = "Time Area A";
    private const string AreaB = "Time Area B";
    private static readonly DateTime MonthStart = new(2026, 9, 1);
    private static readonly DateTime MonthEnd = new(2026, 9, 30);
    private static readonly DateTime Joined = new(2020, 1, 15);

    private readonly IGoLiveSetupAppService _goLive;

    public TimeEngineBatches_Integration_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>
    /// The proof the batches change nothing: the same month run in one unit of work and then in batches of two
    /// (three employees -- a full batch and a part one) gives every employee the same day rows.
    /// </summary>
    [Fact]
    public async Task Should_Write_The_Same_Time_Registers_In_Batches_As_In_One_Unit_Of_Work()
    {
        await GivenEmployeesInArea(AreaA, "Unit A", "T001", "T002", "T003");

        await RunEngineAsync(AreaA, commitBatchSize: 0);
        var inOne = Registers();

        await RunEngineAsync(AreaA, commitBatchSize: 2);
        var inBatches = Registers();

        inOne.Count.ShouldBe(90, "three employees, thirty days each");
        inBatches.ShouldBe(inOne);
    }

    [Fact]
    public async Task Should_Report_A_Batched_Run_On_One_Progress_Row_Completed_After_It_Is_Saved()
    {
        await GivenEmployeesInArea(AreaA, "Unit A", "T001", "T002", "T003");
        var runsBefore = TimeEngineRuns().Count;

        await RunEngineAsync(AreaA, commitBatchSize: 2);

        var runs = TimeEngineRuns();
        runs.Count.ShouldBe(runsBefore + 1);
        var run = runs.Last();
        run.Status.ShouldBe(EngineRunStatus.Completed);
        run.RunKey.ShouldBe(TimeEngineRunKeys.ForPayrollArea(AreaA));
        run.TotalEmployees.ShouldBe(3);
        run.SucceededEmployees.ShouldBe(3);
        run.CompletedAt.ShouldNotBeNull();
        Registers().Count.ShouldBe(90, "everything the run wrote is saved by the time it is marked Completed");
    }

    /// <summary>BUG: every active employee was run whatever area was selected.</summary>
    [Fact]
    public async Task Should_Run_Only_The_Selected_Payroll_Areas_Employees()
    {
        await GivenEmployeesInArea(AreaA, "Unit A", "T001", "T002");
        await GivenEmployeesInArea(AreaB, "Unit B", "T101");

        await RunEngineAsync(AreaA, commitBatchSize: 0);

        Registers().Select(r => r.Split('|')[0]).Distinct().OrderBy(c => c).ShouldBe(new[] { "T001", "T002" });
    }

    [Fact]
    public async Task Should_Run_A_Whole_Payroll_Area_Through_The_Job()
    {
        await GivenEmployeesInArea(AreaA, "Unit A", "T001", "T002");

        await RunJobAsync(AreaA, MonthEnd);

        var run = TimeEngineRuns().Last();
        run.Status.ShouldBe(EngineRunStatus.Completed);
        run.SucceededEmployees.ShouldBe(2);
        Registers().Count.ShouldBe(60);
    }

    /// <summary>
    /// A run the engine refuses before it begins -- here, no end date -- is not thrown back at the job manager (which
    /// would run it again) but written onto a progress row as Failed with the reason, where the screen shows it.
    /// </summary>
    [Fact]
    public async Task Should_Record_A_Refused_Run_As_Failed_And_Not_Throw()
    {
        await Should.NotThrowAsync(() => RunJobAsync(AreaA, default(DateTime)));

        var run = TimeEngineRuns().Last();
        run.Status.ShouldBe(EngineRunStatus.Failed);
        run.FailureMessage.ShouldContain("No End Date defined");
        run.RunKey.ShouldBe(TimeEngineRunKeys.ForPayrollArea(AreaA));
    }

    // ---------------------------------------------------------------- arrange / act / read back

    private async Task GivenEmployeesInArea(string payrollAreaName, string organizationUnitName, params string[] codes)
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }

        await _goLive.LoadEmployees(new GoLiveLoadEmployeesInput
        {
            OrganizationUnitName = organizationUnitName,
            PayrollAreaName = payrollAreaName,
            Employees = codes
                .Select(code => new GoLiveEmployeeRowDto
                {
                    Code = code,
                    FullName = "Employee " + code,
                    JoiningDate = Joined,
                    CivilId = "2900115" + code.Substring(1).PadLeft(5, '0'),
                    BasicSalary = 500,
                })
                .ToList(),
        });
    }

    private async Task RunEngineAsync(string payrollAreaName, int commitBatchSize)
    {
        await WithUnitOfWorkAsync(async () =>
        {
            await Resolve<ITimeEngineService>().CreateTimeRegisterAsync(new TimeEngineRunInput
            {
                PayrollAreaName = payrollAreaName,
                StartDate = MonthStart,
                EndDate = MonthEnd,
                CurrentUserName = "tester",
                CommitBatchSize = commitBatchSize
            });
        });
    }

    private async Task RunJobAsync(string payrollAreaName, DateTime endDate)
    {
        await Resolve<TimeEngineBackgroundJob>().ExecuteAsync(new TimeEngineJobArgs
        {
            TenantId = TenantId,
            UserId = AbpSession.UserId,
            PayrollAreaName = payrollAreaName,
            StartDate = MonthStart,
            EndDate = endDate,
            CurrentUserName = "tester"
        });
    }

    /// <summary>Every September time register, by employee code and day, with the figures payroll reads from it.</summary>
    private List<string> Registers() => UsingDbContext(context =>
        (from register in context.TimeRegisters
         join employee in context.Employees on register.EmployeeId equals employee.Id
         where register.TenantId == TenantId
               && !register.IsDeleted
               && register.DayDate >= MonthStart
               && register.DayDate <= MonthEnd
         select new
         {
             employee.Code,
             register.DayDate,
             register.DutyId,
             register.DutyPeriodId,
             register.TimePolicyId,
             register.PlannedWorkingDay,
             register.ActualWorkingDay,
             register.AbsentDay,
             register.WeeklyOff,
             register.RestDay,
             register.PublicHoliday,
             register.LeaveDay,
             register.AnnualLeaveAccrualPerDay,
             register.ActiveDay,
             register.InactiveDay,
             register.OvertimeMinutes,
             register.DedLateInEarlyOutMins,
             register.Status
         })
        .ToList()
        .Select(r => string.Join("|",
            r.Code, r.DayDate.ToString("yyyyMMdd"), r.DutyId, r.DutyPeriodId, r.TimePolicyId,
            r.PlannedWorkingDay.ToString("R"), r.ActualWorkingDay.ToString("R"), r.AbsentDay.ToString("R"),
            r.WeeklyOff.ToString("R"), r.RestDay.ToString("R"), r.PublicHoliday.ToString("R"), r.LeaveDay.ToString("R"),
            r.AnnualLeaveAccrualPerDay.ToString("R"), r.ActiveDay.ToString("R"), r.InactiveDay.ToString("R"),
            r.OvertimeMinutes.ToString("R"), r.DedLateInEarlyOutMins.ToString("R"), r.Status))
        .OrderBy(s => s, StringComparer.Ordinal)
        .ToList());

    private List<EngineRun> TimeEngineRuns() => UsingDbContext(context => context.EngineRuns
        .Where(r => r.TenantId == TenantId && r.EngineType == EngineType.TimeEngine)
        .OrderBy(r => r.Id)
        .ToList());
}
