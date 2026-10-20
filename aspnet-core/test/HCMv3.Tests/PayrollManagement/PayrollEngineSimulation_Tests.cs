using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using Abp.UI;
using HCMv3.EngineRuns;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using HCMv3.PayrollManagement.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// SIMULATION (test) payroll run on the everyday Payroll Engine screen -- wave P7, the port of legacy
/// <c>HCM10.PayrollEngine/SimulationPayrollHelper.cs</c>.
///
/// <para>Built on the real engine and every real wage rule, over a company Go-Live Setup has loaded and paid for
/// September 2026 (one employee per code, basic salary 500, made-up figures). The runs go through
/// <see cref="PayrollEngineBackgroundJob"/> -- the one path production uses.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollEngineSimulation_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime September = new(2026, 9, 30);
    private static readonly DateTime Joined = new(2020, 1, 15);

    private readonly IGoLiveSetupAppService _goLive;

    public PayrollEngineSimulation_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- the simulation tables only

    [Fact]
    public async Task Should_Write_Only_The_Simulation_Registers_And_Leave_The_Live_Ones_As_They_Were()
    {
        await GivenEmployeesPaidForSeptember("E001", "E002");
        GivenSimulationIsAllowed(true);
        var liveBefore = LiveRegisterRows();
        var statusBefore = PayrollStatusRows();
        var paymentsBefore = EmployeePaymentRows();

        await RunJobAsync(PayrollRegisterTarget.Simulation);

        LiveRegisterRows().ShouldBe(liveBefore, "a simulation must not add, change or delete a live register, line or day row");
        PayrollStatusRows().ShouldBe(statusBefore, "a simulation never resets AccountedTo / EarliestMasterDataChange");
        EmployeePaymentRows().ShouldBe(paymentsBefore, "a simulation raises and clears no 2005/2011 payment");

        var simulation = SimulationRegisters();
        simulation.Count.ShouldBe(2);
        simulation.ShouldAllBe(r => r.ForPeriod == September && r.InPeriod == September
                                    && r.PayrollRunType == PayrollRunType.Regular
                                    && r.PayrollResultType == PayrollResultType.Current
                                    && r.PayrollReasonTypeId == null);
        SimulationLines().ShouldNotBeEmpty();

        LatestEngineRun().Status.ShouldBe(EngineRunStatus.Completed);
    }

    /// <summary>A test run shows what the real run would pay: the same live rules, written elsewhere.</summary>
    [Fact]
    public async Task Should_Calculate_The_Same_Lines_As_A_Live_Run_Of_The_Same_Month()
    {
        await GivenEmployeesPaidForSeptember("E001", "E002");
        GivenSimulationIsAllowed(true);
        await RunJobAsync(PayrollRegisterTarget.Live);
        var live = LiveLines();

        await RunJobAsync(PayrollRegisterTarget.Simulation);

        live.ShouldNotBeEmpty();
        SimulationLines().ShouldBe(live);
    }

    /// <summary>Legacy's log row of a test run leads with "Simulation Process Ran" (line 409).</summary>
    [Fact]
    public async Task Should_Say_Simulation_On_Its_Execution_Log_Row()
    {
        await GivenEmployeesPaidForSeptember("E001");
        GivenSimulationIsAllowed(true);

        await RunJobAsync(PayrollRegisterTarget.Simulation);

        var parameters = UsingDbContext(context => context.ExecutionLogs
            .Where(l => l.Module == HCMv3.SystemSecurity.ExecutionLogModule.PayrollEngine)
            .OrderByDescending(l => l.Id)
            .First()
            .Parameters);
        parameters.ShouldStartWith("Simulation Process Ran | Payroll Area : " + DefaultArea);
    }

    // ---------------------------------------------------------------- what a simulation clears first

    /// <summary>
    /// Legacy <c>DeleteAllPreviousSimulationResiters</c>: every earlier simulation register of the run's
    /// employees goes first -- whatever its month -- so the table only ever holds the latest test run.
    /// </summary>
    [Fact]
    public async Task Should_Replace_Every_Earlier_Simulation_Of_The_Same_Employees()
    {
        await GivenEmployeesPaidForSeptember("E001", "E002");
        GivenSimulationIsAllowed(true);
        var august = GivenAnOldSimulationRegister("E001", new DateTime(2026, 8, 31));
        await RunJobAsync(PayrollRegisterTarget.Simulation);
        var firstRun = SimulationRegisters().Select(r => r.Id).ToList();

        await RunJobAsync(PayrollRegisterTarget.Simulation);

        var secondRun = SimulationRegisters();
        secondRun.Count.ShouldBe(2);
        secondRun.ShouldNotContain(r => r.Id == august, "the August test run of E001 is cleared too");
        secondRun.ShouldNotContain(r => firstRun.Contains(r.Id), "the first test run is replaced, not added to");
        UsingDbContext(context => context.SimulationSalaryRegisterDetails
                .Count(d => !d.IsDeleted && !context.SimulationSalaryRegisters.Any(r => r.Id == d.SimulationSalaryRegisterId && !r.IsDeleted)))
            .ShouldBe(0, "no line is left behind without its register");
    }

    /// <summary>Only the employees the run picks are cleared: a test run of E001 leaves E002's alone.</summary>
    [Fact]
    public async Task Should_Leave_The_Simulations_Of_Other_Employees_Alone()
    {
        await GivenEmployeesPaidForSeptember("E001", "E002");
        GivenSimulationIsAllowed(true);
        await RunJobAsync(PayrollRegisterTarget.Simulation);
        var e002 = SimulationRegistersOf("E002").Single().Id;

        await RunJobAsync(PayrollRegisterTarget.Simulation, employeeCodes: "E001");

        SimulationRegistersOf("E002").Single().Id.ShouldBe(e002);
        SimulationRegistersOf("E001").ShouldHaveSingleItem();
    }

    // ---------------------------------------------------------------- what a simulation does not refuse

    /// <summary>
    /// Legacy pins: SimulationPayrollHelper.Main lines 263-273 comment out the "control record locked and
    /// current" check, so a test run can be made while the month is still open. A live run of it is refused.
    /// </summary>
    [Fact]
    public async Task Should_Run_A_Regular_Simulation_For_A_Month_That_Is_Not_Locked()
    {
        await GivenEmployeesPaidForSeptember("E001");
        GivenSimulationIsAllowed(true);
        UsingDbContext(context =>
        {
            context.PayrollControlRecords.Single(r => r.TenantId == TenantId && !r.IsDeleted).PeriodLocked = false;
            context.SaveChanges();
        });

        await RunJobAsync(PayrollRegisterTarget.Live);
        var liveRun = LatestEngineRun();

        await RunJobAsync(PayrollRegisterTarget.Simulation);

        liveRun.Status.ShouldBe(EngineRunStatus.Failed);
        liveRun.FailureMessage.ShouldContain("not locked");
        LatestEngineRun().Status.ShouldBe(EngineRunStatus.Completed);
        SimulationRegisters().ShouldHaveSingleItem();
    }

    // ---------------------------------------------------------------- the setting

    [Fact]
    public async Task Should_Refuse_A_Simulation_When_The_Setting_Is_Off_And_Not_Throw_Out_Of_The_Job()
    {
        await GivenEmployeesPaidForSeptember("E001");
        GivenSimulationIsAllowed(false);
        var liveBefore = LiveRegisterRows();

        await Should.NotThrowAsync(() => RunJobAsync(PayrollRegisterTarget.Simulation));

        var run = LatestEngineRun();
        run.Status.ShouldBe(EngineRunStatus.Failed);
        run.FailureMessage.ShouldContain("Simulation run is not allowed");
        SimulationRegisters().ShouldBeEmpty();
        LiveRegisterRows().ShouldBe(liveBefore);
    }

    [Fact]
    public async Task Should_Refuse_To_Queue_A_Simulation_When_The_Setting_Is_Off()
    {
        await GivenEmployeesPaidForSeptember("E001");
        GivenSimulationIsAllowed(false);
        var jobsBefore = QueuedPayrollJobs().Count;

        await Should.ThrowAsync<UserFriendlyException>(() => Resolve<IPayrollEngineAppService>().RunPayrollEngine(
            new RunPayrollEngineInput { PayInDate = September, PayrollAreaName = DefaultArea, IsSimulation = true }));

        QueuedPayrollJobs().Count.ShouldBe(jobsBefore);
    }

    // ---------------------------------------------------------------- current and other period

    [Fact]
    public async Task Should_Queue_A_Current_Period_Simulation_With_The_Pay_Date_Given()
    {
        await GivenEmployeesPaidForSeptember("E001");
        GivenSimulationIsAllowed(true);

        await Resolve<IPayrollEngineAppService>().RunPayrollEngine(
            new RunPayrollEngineInput { PayInDate = September, PayrollAreaName = DefaultArea, IsSimulation = true });

        var args = QueuedPayrollJobs().Last();
        args.RegisterTarget.ShouldBe(PayrollRegisterTarget.Simulation);
        args.PayInDate.ShouldBe(September);
        args.UserId.ShouldBe(AbpSession.UserId);
    }

    /// <summary>
    /// Legacy <c>uscPayrollEngine.SetDates</c>: the "Other" period is a payroll month/year of the area's payroll
    /// year, turned into its calendar month end. Under "Apr-Mar", payroll month 10 of 2026 is January 2027.
    /// </summary>
    [Fact]
    public async Task Should_Work_Out_The_Pay_Date_Of_The_Other_Period_From_The_Payroll_Year()
    {
        GivenAnAreaWithAControlRecord("Sim Area", "Apr-Mar");
        GivenSimulationIsAllowed(true);

        await Resolve<IPayrollEngineAppService>().RunPayrollEngine(new RunPayrollEngineInput
        {
            PayInDate = September,
            PayrollAreaName = "Sim Area",
            IsSimulation = true,
            SimulationOtherPeriod = true,
            SimulationMonth = 10,
            SimulationYear = 2026
        });

        var args = QueuedPayrollJobs().Last();
        args.PayInDate.ShouldBe(new DateTime(2027, 1, 31));
        args.RegisterTarget.ShouldBe(PayrollRegisterTarget.Simulation);
    }

    [Fact]
    public async Task Should_Use_The_Calendar_Month_Itself_Under_A_January_To_December_Year()
    {
        GivenAnAreaWithAControlRecord("Sim Area", "Jan-Dec");
        GivenSimulationIsAllowed(true);

        await Resolve<IPayrollEngineAppService>().RunPayrollEngine(new RunPayrollEngineInput
        {
            PayInDate = September,
            PayrollAreaName = "Sim Area",
            IsSimulation = true,
            SimulationOtherPeriod = true,
            SimulationMonth = 2,
            SimulationYear = 2028
        });

        QueuedPayrollJobs().Last().PayInDate.ShouldBe(new DateTime(2028, 2, 29));
    }

    [Fact]
    public async Task Should_Ask_For_The_Month_And_Year_Of_The_Other_Period()
    {
        GivenAnAreaWithAControlRecord("Sim Area", "Jan-Dec");
        GivenSimulationIsAllowed(true);

        await Should.ThrowAsync<UserFriendlyException>(() => Resolve<IPayrollEngineAppService>().RunPayrollEngine(
            new RunPayrollEngineInput
            {
                PayInDate = September,
                PayrollAreaName = "Sim Area",
                IsSimulation = true,
                SimulationOtherPeriod = true,
                SimulationMonth = 3
            }));

        QueuedPayrollJobs().ShouldBeEmpty();
    }

    /// <summary>
    /// Legacy works the other period out only for an area with a control record, and is left with no pay date
    /// otherwise -- refused as "no pay date", as the engine refused it.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_The_Other_Period_Of_An_Area_Without_A_Control_Record()
    {
        GivenAnAreaWithAControlRecord("Sim Area", "Jan-Dec", withControlRecord: false);
        GivenSimulationIsAllowed(true);

        await Should.ThrowAsync<UserFriendlyException>(() => Resolve<IPayrollEngineAppService>().RunPayrollEngine(
            new RunPayrollEngineInput
            {
                PayInDate = September,
                PayrollAreaName = "Sim Area",
                IsSimulation = true,
                SimulationOtherPeriod = true,
                SimulationMonth = 3,
                SimulationYear = 2026
            }));

        QueuedPayrollJobs().ShouldBeEmpty();
    }

    /// <summary>A live run ignores the simulation fields entirely.</summary>
    [Fact]
    public async Task Should_Queue_A_Live_Run_With_Its_Own_Pay_Date_When_Not_A_Simulation()
    {
        await GivenEmployeesPaidForSeptember("E001");

        await Resolve<IPayrollEngineAppService>().RunPayrollEngine(new RunPayrollEngineInput
        {
            PayInDate = September,
            PayrollAreaName = DefaultArea,
            SimulationOtherPeriod = true,
            SimulationMonth = 1,
            SimulationYear = 2020
        });

        var args = QueuedPayrollJobs().Last();
        args.RegisterTarget.ShouldBe(PayrollRegisterTarget.Live);
        args.PayInDate.ShouldBe(September);
    }

    // ---------------------------------------------------------------- arrange

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
                    BasicSalary = 500,
                })
                .ToList(),
        });

        var run = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);
    }

    private void GivenSimulationIsAllowed(bool allowed)
    {
        UsingDbContext(context =>
        {
            var settings = context.ManagePayrollSettingses.FirstOrDefault(s => s.TenantId == TenantId);
            if (settings == null)
            {
                settings = new HCMv3.SystemSettings.ManagePayrollSettings { TenantId = TenantId };
                context.ManagePayrollSettingses.Add(settings);
            }

            settings.AllowSimulationRun = allowed;
            context.SaveChanges();
        });
    }

    private void GivenAnAreaWithAControlRecord(string name, string payYearCode, bool withControlRecord = true)
    {
        AsyncHelper.RunSync(() => Resolve<IReferenceDataSeeder>().SeedPackAsync("universal", TenantId));

        UsingDbContext(context =>
        {
            var area = new PayrollArea
            {
                TenantId = TenantId,
                Code = "SIM",
                Name = name,
                CurrencyId = context.Currencies.First(c => c.TenantId == TenantId && c.Code == "KWD").Id,
                PayrollPeriodId = context.PayrollPeriods.First(p => p.TenantId == TenantId && p.Code == payYearCode).Id,
                CutOffDay = 31,
            };
            context.PayrollAreas.Add(area);
            context.SaveChanges();

            if (withControlRecord)
            {
                context.PayrollControlRecords.Add(new PayrollControlRecord
                {
                    TenantId = TenantId,
                    PayrollAreaId = area.Id,
                    PayrollMonth = September.Month,
                    PayrollYear = September.Year,
                    CalendarMonthStart = new DateTime(2026, 9, 1),
                    CalendarMonthEnd = September,
                    PeriodLocked = true,
                });
                context.SaveChanges();
            }
        });
    }

    private int GivenAnOldSimulationRegister(string employeeCode, DateTime monthEnd)
    {
        return UsingDbContext(context =>
        {
            var register = new SimulationSalaryRegister
            {
                TenantId = TenantId,
                EmployeeId = context.Employees.Single(e => e.TenantId == TenantId && e.Code == employeeCode).Id,
                ForPeriod = monthEnd,
                InPeriod = monthEnd,
                PayrollRunType = PayrollRunType.Regular,
                PayrollResultType = PayrollResultType.Current,
            };
            context.SimulationSalaryRegisters.Add(register);
            context.SaveChanges();

            context.SimulationSalaryRegisterDetails.Add(new SimulationSalaryRegisterDetail
            {
                TenantId = TenantId,
                SimulationSalaryRegisterId = register.Id,
                WageTypeId = context.WageTypes.First(w => w.TenantId == TenantId && w.Code == "9000").Id,
                Amount = 123,
            });
            context.SaveChanges();
            return register.Id;
        });
    }

    // ---------------------------------------------------------------- act

    private async Task RunJobAsync(PayrollRegisterTarget target, string employeeCodes = null)
    {
        await Resolve<PayrollEngineBackgroundJob>().ExecuteAsync(new PayrollEngineJobArgs
        {
            TenantId = TenantId,
            UserId = AbpSession.UserId,
            PayrollRunType = PayrollRunType.Regular,
            PayInDate = September,
            PayrollAreaName = DefaultArea,
            EmployeeCodes = employeeCodes,
            RegisterTarget = target,
        });
    }

    // ---------------------------------------------------------------- read back

    /// <summary>Every live register, line and day row -- deleted ones too -- with every value and stamp that a write would change.</summary>
    private List<string> LiveRegisterRows() => UsingDbContext(context =>
    {
        var registers = context.SalaryRegisters.IgnoreQueryFilters().Where(r => r.TenantId == TenantId).ToList()
            .Select(r => $"R|{r.Id}|{r.EmployeeId}|{r.ForPeriod:yyyyMMdd}|{r.InPeriod:yyyyMMdd}|{r.PayrollRunType}|{r.PayrollResultType}|{r.PayrollReasonTypeId}|{r.IsDeleted}|{r.LastModificationTime:O}");
        var lines = context.SalaryRegisterDetails.IgnoreQueryFilters().Where(d => d.TenantId == TenantId).ToList()
            .Select(d => $"L|{d.Id}|{d.SalaryRegisterId}|{d.WageTypeId}|{d.Rate:R}|{d.Number:R}|{d.Amount:R}|{d.IsDeleted}|{d.LastModificationTime:O}");
        var days = context.SalaryRegisterDaysDetails.IgnoreQueryFilters().Where(d => d.TenantId == TenantId).ToList()
            .Select(d => $"D|{d.Id}|{d.SalaryRegisterDetailId}|{d.Period:yyyyMMdd}|{d.Rate:R}|{d.Number:R}|{d.Amount:R}|{d.IsDeleted}");
        return registers.Concat(lines).Concat(days).OrderBy(s => s, StringComparer.Ordinal).ToList();
    });

    private List<string> PayrollStatusRows() => UsingDbContext(context => context.EmployeePayrollStatuses
        .Where(s => s.TenantId == TenantId).ToList()
        .Select(s => $"{s.Id}|{s.AccountedTo:O}|{s.EarliestMasterDataChange:O}|{s.LastModificationTime:O}")
        .OrderBy(s => s, StringComparer.Ordinal).ToList());

    private List<string> EmployeePaymentRows() => UsingDbContext(context => context.EmployeePayments
        .IgnoreQueryFilters().Where(p => p.TenantId == TenantId).ToList()
        .Select(p => $"{p.Id}|{p.WageTypeId}|{p.Amount:R}|{p.IsDeleted}")
        .OrderBy(s => s, StringComparer.Ordinal).ToList());

    /// <summary>September's own live lines, by employee code and wage code.</summary>
    private List<string> LiveLines() => UsingDbContext(context =>
        (from line in context.SalaryRegisterDetails
         join register in context.SalaryRegisters on line.SalaryRegisterId equals register.Id
         join employee in context.Employees on register.EmployeeId equals employee.Id
         join wageType in context.WageTypes on line.WageTypeId equals wageType.Id
         where register.TenantId == TenantId && !register.IsDeleted && !line.IsDeleted && register.ForPeriod == September && register.InPeriod == September
               && register.PayrollRunType == PayrollRunType.Regular
         select new { employee.Code, WageCode = wageType.Code, line.Rate, line.Number, line.Amount })
        .ToList()
        .Select(l => $"{l.Code}|{l.WageCode}|{l.Rate:R}|{l.Number:R}|{l.Amount:R}")
        .OrderBy(s => s, StringComparer.Ordinal)
        .ToList());

    /// <summary>The same, from the simulation registers.</summary>
    private List<string> SimulationLines() => UsingDbContext(context =>
        (from line in context.SimulationSalaryRegisterDetails
         join register in context.SimulationSalaryRegisters on line.SimulationSalaryRegisterId equals register.Id
         join employee in context.Employees on register.EmployeeId equals employee.Id
         join wageType in context.WageTypes on line.WageTypeId equals wageType.Id
         where register.TenantId == TenantId && !register.IsDeleted && !line.IsDeleted && register.ForPeriod == September && register.InPeriod == September
               && register.PayrollRunType == PayrollRunType.Regular
         select new { employee.Code, WageCode = wageType.Code, line.Rate, line.Number, line.Amount })
        .ToList()
        .Select(l => $"{l.Code}|{l.WageCode}|{l.Rate:R}|{l.Number:R}|{l.Amount:R}")
        .OrderBy(s => s, StringComparer.Ordinal)
        .ToList());

    private List<SimulationSalaryRegister> SimulationRegisters() => UsingDbContext(context =>
        context.SimulationSalaryRegisters.Where(r => r.TenantId == TenantId && !r.IsDeleted).ToList());

    private List<SimulationSalaryRegister> SimulationRegistersOf(string employeeCode) => UsingDbContext(context =>
    {
        var employeeId = context.Employees.Single(e => e.TenantId == TenantId && e.Code == employeeCode).Id;
        return context.SimulationSalaryRegisters.Where(r => r.EmployeeId == employeeId && !r.IsDeleted).ToList();
    });

    private EngineRun LatestEngineRun() => UsingDbContext(context => context.EngineRuns
        .Where(r => r.TenantId == TenantId && r.EngineType == EngineType.PayrollEngine)
        .OrderByDescending(r => r.Id)
        .First());

    private List<PayrollEngineJobArgs> QueuedPayrollJobs() => UsingDbContext(context => context.BackgroundJobs
        .Where(j => j.JobType.Contains(nameof(PayrollEngineBackgroundJob)))
        .OrderBy(j => j.Id)
        .ToList()
        .Select(j => JsonConvert.DeserializeObject<PayrollEngineJobArgs>(j.JobArgs))
        .ToList());
}
