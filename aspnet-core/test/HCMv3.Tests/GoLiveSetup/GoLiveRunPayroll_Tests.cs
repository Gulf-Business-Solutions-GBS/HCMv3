using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// Go-Live Setup is built backwards from its goal: press Run Payroll, see what the engine refuses on, and
/// make each refusal the next step. These tests pin that chain one link at a time, in the order a brand-new
/// tenant meets them. Each new link is written here first, while it still fails.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveRunPayroll_Tests : AppTestBase
{
    private static readonly DateTime PayDate = new(2026, 9, 30);

    private readonly IGoLiveSetupAppService _goLive;

    public GoLiveRunPayroll_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- link 1: nothing given

    [Fact]
    public async Task Should_Stop_On_The_Pay_Date_When_Nothing_Is_Given()
    {
        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput());

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveStoppedAt.Input);
        result.Message.ShouldBe("No Pay Date defined for Payroll Calculation. Please select a Pay Date.");
    }

    // ---------------------------------------------------------------- link 2: a date, but no payroll area

    [Fact]
    public async Task Should_Stop_On_The_Payroll_Area_When_Only_A_Date_Is_Given()
    {
        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = PayDate });

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveStoppedAt.Input);
        result.Message.ShouldBe("Payroll Area is required to run the Payroll Engine.");
    }

    // ---------------------------------------------------------------- link 3: the engine itself refuses

    /// <summary>
    /// Run payroll sets up a month that is missing its control record or calendar before the engine looks for
    /// them. On a company with no reference data it cannot -- there is no Kuwaiti Dinar to pay in -- and says
    /// so, rather than letting the engine refuse for want of a record Go-Live Setup could not make.
    /// </summary>
    [Fact]
    public async Task Should_Say_Why_The_Month_Could_Not_Be_Set_Up()
    {
        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput
        {
            PayDate = PayDate,
            PayrollAreaName = "Nowhere",
        });

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveStoppedAt.MonthSetUp);
        result.Message.ShouldContain("KWD");
    }

    // ---------------------------------------------------------------- link 4: the run starts, nobody to pay

    /// <summary>
    /// The engine finishes happily when the payroll area is empty -- nothing failed, because nothing was tried.
    /// Reporting that as success would tell a customer their payroll works when it has paid no one.
    /// </summary>
    [Fact]
    public async Task Should_Not_Call_A_Run_That_Paid_Nobody_A_Success()
    {
        var area = GivenARunnablePayrollArea("Lab");

        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput
        {
            PayDate = PayDate,
            PayrollAreaName = area.Name,
        });

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveStoppedAt.NoEmployees);
        result.Employees.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------- link n: the run starts, an employee fails

    /// <summary>
    /// Once the run itself is allowed, refusals move down to individual employees. The run does not stop for
    /// them -- each is logged and the next is tried -- so Go-Live Setup has to report them per employee rather than as
    /// one message.
    /// </summary>
    [Fact]
    public async Task Should_Report_Each_Employee_The_Engine_Could_Not_Pay()
    {
        // The time engine now runs first and meets the missing contract before the payroll engine does. The
        // refusal is the same fact, in the time engine's words.
        var area = GivenARunnablePayrollArea("Lab");
        GivenTheTimeEngineSettings();
        GivenAnEmployeeInPayrollArea(area, "V2-001");

        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput
        {
            PayDate = PayDate,
            PayrollAreaName = area.Name,
        });

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveStoppedAt.TimeEngine);

        var employee = result.Employees.Single();
        employee.EmployeeCode.ShouldBe("V2-001");
        employee.Succeeded.ShouldBeFalse();
        employee.Message.ShouldContain("Contract not found");
    }

    [Fact]
    public async Task Should_Normalise_The_Pay_Date_To_The_Month_End_The_Engine_Uses()
    {
        var area = GivenARunnablePayrollArea("Lab");
        GivenAnEmployeeInPayrollArea(area, "V2-001");

        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput
        {
            PayDate = new DateTime(2026, 9, 12),
            PayrollAreaName = area.Name,
        });

        result.StoppedAt.ShouldBe(GoLiveStoppedAt.TimeEngine);
        result.PayInDate.ShouldBe(PayDate);
    }

    /// <summary>
    /// The time engine reads four settings records and would crash on a missing one. A company set up by hand,
    /// without them, is told which -- rather than shown a null reference.
    /// </summary>
    [Fact]
    public async Task Should_Name_The_Settings_The_Time_Engine_Lacks()
    {
        var area = GivenARunnablePayrollArea("Lab");
        GivenAnEmployeeInPayrollArea(area, "V2-001");

        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = PayDate, PayrollAreaName = area.Name });

        result.StoppedAt.ShouldBe(GoLiveStoppedAt.TimeEngine);
        result.Message.ShouldContain("Time settings");
        result.Message.ShouldContain("Security settings");
    }

    // ---------------------------------------------------------------- link 3 answered: set up the month

    private const string DefaultArea = "Default Payroll Area";

    /// <summary>
    /// The engine refused a new company for want of a control record, and then a pay calendar -- links 3 and 4.
    /// Both are answered by setting up the month, which needs nothing the customer has not already given, so
    /// Run payroll now does it itself, first, and says what it did. The run goes on to the next link.
    /// </summary>
    [Fact]
    public async Task Should_Set_Up_The_Month_Itself_Before_Running()
    {
        GivenTheReferenceData();

        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        result.MonthSetUp.ShouldNotBeNull();
        result.MonthSetUp.PayrollAreaCreated.ShouldBeTrue();
        result.MonthSetUp.ControlRecordCreated.ShouldBeTrue();
        result.MonthSetUp.CalendarMonthsCreated.ShouldBe(12);
        result.StoppedAt.ShouldBe(GoLiveStoppedAt.NoEmployees);
    }

    [Fact]
    public async Task Should_Not_Set_Up_A_Month_That_Is_Already_Set_Up()
    {
        GivenTheReferenceData();
        await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        var again = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        again.MonthSetUp.ShouldBeNull();
        again.StoppedAt.ShouldBe(GoLiveStoppedAt.NoEmployees);
    }

    [Fact]
    public async Task Should_Not_Point_Anywhere_While_An_Input_Is_Still_Missing()
    {
        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput());

        result.NextStep.ShouldBe(GoLiveNextStep.None);
    }

    [Fact]
    public async Task Should_Set_Up_A_Payroll_Area_And_Its_Control_Record_For_The_Month()
    {
        GivenTheReferenceData();

        var setUp = await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput
        {
            PayDate = new DateTime(2026, 9, 30, 12, 0, 0),
            PayrollAreaName = DefaultArea,
        });

        setUp.PayrollAreaCreated.ShouldBeTrue();
        setUp.CurrencyCode.ShouldBe("KWD");
        setUp.PayYear.ShouldBe("Jan-Dec");
        setUp.CutOffDay.ShouldBe(31);
        setUp.ControlRecordCreated.ShouldBeTrue();
        setUp.MonthStart.ShouldBe(new DateTime(2026, 9, 1));
        setUp.MonthEnd.ShouldBe(PayDate);

        UsingDbContext(context =>
        {
            var area = context.PayrollAreas.Single(a => a.TenantId == TenantId && a.Name == DefaultArea);
            var record = context.PayrollControlRecords.Single(r => r.PayrollAreaId == area.Id);

            // What the engine checks before it will start a regular run.
            record.CalendarMonthEnd.ShouldBe(PayDate);
            record.PeriodLocked.ShouldBeTrue();
            record.PeriodClosed.ShouldBeFalse();
        });
    }

    /// <summary>
    /// The point of the step: the same press that was refused gets further. Set-up lays down the control record
    /// and the pay calendar, so the engine passes both and runs -- and finds nobody in the payroll area yet.
    /// </summary>
    [Fact]
    public async Task Should_Get_Past_The_Control_Record_And_The_Calendar_Once_The_Month_Is_Set_Up()
    {
        GivenTheReferenceData();
        await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        result.StoppedAt.ShouldBe(GoLiveStoppedAt.NoEmployees);
        result.NextStep.ShouldBe(GoLiveNextStep.LoadEmployees);
    }

    [Fact]
    public async Task Should_Use_An_Existing_Payroll_Area_Rather_Than_Create_Another()
    {
        GivenTheReferenceData();
        GivenAPayrollAreaWithoutAControlRecord(DefaultArea, "Jan-Dec");

        var setUp = await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        setUp.PayrollAreaCreated.ShouldBeFalse();
        setUp.ControlRecordCreated.ShouldBeTrue();
        UsingDbContext(context => context.PayrollAreas.Count(a => a.TenantId == TenantId && a.Name == DefaultArea).ShouldBe(1));
    }

    [Fact]
    public async Task Should_Leave_An_Existing_Control_Record_Alone()
    {
        // The engine allows one control record per payroll area. A second set-up must not try to add another.
        GivenTheReferenceData();
        await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        var again = await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        again.PayrollAreaCreated.ShouldBeFalse();
        again.ControlRecordCreated.ShouldBeFalse();
        UsingDbContext(context => context.PayrollControlRecords.Count(r => r.TenantId == TenantId).ShouldBe(1));
    }

    /// <summary>
    /// A control record's month is the month of the PAY YEAR, not of the calendar: for an April to March
    /// company, September is month 6. Assuming the calendar month would lock the wrong month.
    /// </summary>
    [Fact]
    public async Task Should_Work_Out_The_Pay_Year_Month_For_Any_Pay_Year()
    {
        GivenTheReferenceData();
        GivenAPayrollAreaWithoutAControlRecord(DefaultArea, "Apr-Mar");

        var setUp = await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        setUp.MonthEnd.ShouldBe(PayDate);
        UsingDbContext(context => context.PayrollControlRecords.Single(r => r.TenantId == TenantId).PayrollMonth.ShouldBe(6));
    }

    [Fact]
    public async Task Should_Refuse_To_Guess_A_Currency_The_Tenant_Does_Not_Have()
    {
        // No reference data: no Kuwaiti Dinar. Paying a whole company in some other currency would be a guess.
        var ex = await Should.ThrowAsync<UserFriendlyException>(() =>
            _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea }));

        ex.Message.ShouldContain("KWD");
    }

    // ---------------------------------------------------------------- link 4 answered: the pay calendar

    /// <summary>
    /// A tenant whose payroll area and control record exist -- set up by hand, or by an earlier Go-Live Setup -- but that
    /// has no calendar. Run payroll lays the calendar down itself and leaves the control record as it was.
    /// </summary>
    [Fact]
    public async Task Should_Lay_Down_A_Missing_Calendar_Itself_Before_Running()
    {
        GivenTheReferenceData();
        GivenAPayrollAreaWithAControlRecordButNoCalendar(DefaultArea);

        var result = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        result.MonthSetUp.ShouldNotBeNull();
        result.MonthSetUp.ControlRecordCreated.ShouldBeFalse();
        result.MonthSetUp.CalendarMonthsCreated.ShouldBe(12);
        result.StoppedAt.ShouldBe(GoLiveStoppedAt.NoEmployees);
    }

    /// <summary>
    /// No questions asked: the year is assumed to run January to December, so the calendar is the twelve
    /// calendar months of the year being paid.
    /// </summary>
    [Fact]
    public async Task Should_Lay_Down_A_January_To_December_Calendar_For_The_Year()
    {
        GivenTheReferenceData();

        var setUp = await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        setUp.CalendarYear.ShouldBe(2026);
        setUp.CalendarMonthsCreated.ShouldBe(12);

        UsingDbContext(context =>
        {
            var months = context.PayrollCalendars
                .Where(c => c.TenantId == TenantId && c.PayrollYear == 2026)
                .OrderBy(c => c.PayrollMonth)
                .ToList();

            months.Select(c => c.PayrollMonth).ShouldBe(Enumerable.Range(1, 12));
            months.First().StartDate.ShouldBe(new DateTime(2026, 1, 1));
            months.First().EndDate.ShouldBe(new DateTime(2026, 1, 31));
            months.Single(c => c.PayrollMonth == 2).EndDate.ShouldBe(new DateTime(2026, 2, 28));
            months.Last().EndDate.ShouldBe(new DateTime(2026, 12, 31));
        });
    }

    [Fact]
    public async Task Should_Only_Add_The_Calendar_Months_That_Are_Missing()
    {
        // September already there -- say, keyed in by hand. The rest of the year is added around it.
        GivenTheReferenceData();
        GivenAPayrollAreaWithAControlRecordButNoCalendar(DefaultArea);
        GivenACalendarMonth(DefaultArea, 9);

        var setUp = await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        setUp.CalendarMonthsCreated.ShouldBe(11);
        UsingDbContext(context => context.PayrollCalendars.Count(c => c.TenantId == TenantId && c.PayrollYear == 2026).ShouldBe(12));
    }

    [Fact]
    public async Task Should_Leave_A_Complete_Calendar_Alone()
    {
        GivenTheReferenceData();
        await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        var again = await _goLive.SetUpPayrollMonth(new GoLiveSetUpPayrollMonthInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        again.CalendarMonthsCreated.ShouldBe(0);
        UsingDbContext(context => context.PayrollCalendars.Count(c => c.TenantId == TenantId).ShouldBe(12));
    }

    // ---------------------------------------------------------------- arrange

    private void GivenTheReferenceData() =>
        AsyncHelper.RunSync(() => Resolve<IReferenceDataSeeder>().SeedPackAsync("universal", TenantId));

    /// <summary>The state IT4's set-up used to leave behind: an area and a locked control record, no calendar.</summary>
    private void GivenAPayrollAreaWithAControlRecordButNoCalendar(string name)
    {
        GivenAPayrollAreaWithoutAControlRecord(name, "Jan-Dec");

        UsingDbContext(context =>
        {
            var area = context.PayrollAreas.Single(a => a.TenantId == TenantId && a.Name == name);
            context.PayrollControlRecords.Add(new PayrollControlRecord
            {
                TenantId = TenantId,
                PayrollAreaId = area.Id,
                PayrollMonth = PayDate.Month,
                PayrollYear = PayDate.Year,
                CalendarMonthStart = new DateTime(PayDate.Year, PayDate.Month, 1),
                CalendarMonthEnd = PayDate,
                PeriodLocked = true,
            });
            context.SaveChanges();
        });
    }

    private void GivenACalendarMonth(string areaName, int month)
    {
        UsingDbContext(context =>
        {
            var area = context.PayrollAreas.Single(a => a.TenantId == TenantId && a.Name == areaName);
            context.PayrollCalendars.Add(new PayrollCalendar
            {
                TenantId = TenantId,
                PayrollAreaId = area.Id,
                PayrollMonth = month,
                PayrollYear = 2026,
                StartDate = new DateTime(2026, month, 1),
                EndDate = new DateTime(2026, month, DateTime.DaysInMonth(2026, month)),
            });
            context.SaveChanges();
        });
    }

    /// <summary>The four settings records the time engine reads, each left at its defaults.</summary>
    private void GivenTheTimeEngineSettings()
    {
        UsingDbContext(context =>
        {
            context.ManageTimeSettingses.Add(new ManageTimeSettings { TenantId = TenantId });
            context.ManagePayrollSettingses.Add(new ManagePayrollSettings { TenantId = TenantId });
            context.ManagePersonnelSettingses.Add(new ManagePersonnelSettings { TenantId = TenantId });
            context.ManageSecuritySettingses.Add(new ManageSecuritySettings { TenantId = TenantId });
            context.SaveChanges();
        });
    }

    /// <summary>A payroll area set up by hand on the payroll screens, before Go-Live Setup was ever opened.</summary>
    private void GivenAPayrollAreaWithoutAControlRecord(string name, string payYearCode)
    {
        UsingDbContext(context =>
        {
            context.PayrollAreas.Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = "90",
                Name = name,
                CurrencyId = context.Currencies.First(c => c.TenantId == TenantId && c.Code == "KWD").Id,
                PayrollPeriodId = context.PayrollPeriods.First(p => p.TenantId == TenantId && p.Code == payYearCode).Id,
                CutOffDay = 31,
            });
            context.SaveChanges();
        });
    }

    /// <summary>
    /// The three records the engine checks before it looks at any employee: the area, its control record
    /// (locked, for the month being paid) and a pay calendar row for that month.
    /// </summary>
    private PayrollArea GivenARunnablePayrollArea(string name)
    {
        AsyncHelper.RunSync(() => Resolve<IReferenceDataSeeder>().SeedPackAsync("universal", TenantId));

        return UsingDbContext(context =>
        {
            var area = new PayrollArea
            {
                TenantId = TenantId,
                Code = name.ToUpperInvariant(),
                Name = name,
                CurrencyId = context.Currencies.First(c => c.TenantId == TenantId && c.Code == "KWD").Id,
                PayrollPeriodId = context.PayrollPeriods.First(p => p.TenantId == TenantId).Id,
                CutOffDay = 31,
            };
            context.PayrollAreas.Add(area);
            context.SaveChanges();

            context.PayrollControlRecords.Add(new PayrollControlRecord
            {
                TenantId = TenantId,
                PayrollAreaId = area.Id,
                PayrollMonth = PayDate.Month,
                PayrollYear = PayDate.Year,
                CalendarMonthStart = new DateTime(PayDate.Year, PayDate.Month, 1),
                CalendarMonthEnd = PayDate,
                PeriodLocked = true,
            });

            context.PayrollCalendars.Add(new PayrollCalendar
            {
                TenantId = TenantId,
                PayrollAreaId = area.Id,
                PayrollMonth = PayDate.Month,
                PayrollYear = PayDate.Year,
                StartDate = new DateTime(PayDate.Year, PayDate.Month, 1),
                EndDate = PayDate,
            });

            context.SaveChanges();
            return area;
        });
    }

    /// <summary>An active employee assigned to the area -- and deliberately nothing else, so the engine refuses.</summary>
    private void GivenAnEmployeeInPayrollArea(PayrollArea area, string code)
    {
        UsingDbContext(context =>
        {
            var employee = new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "Ahmed",
                LastNameEn = "Ali",
                FirstNameAr = "أحمد",
                LastNameAr = "علي",
                NationalityId = context.Nationalities.First(n => n.TenantId == TenantId).Id,
                JoiningDate = new DateTime(2019, 3, 1),
                Status = EmployeeStatus.Active,
            };
            context.Employees.Add(employee);
            context.SaveChanges();

            context.EmployeeOrgAssignments.Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = employee.JoiningDate,
                EndDate = new DateTime(9999, 12, 31),
                PayrollAreaId = area.Id,
            });
            context.SaveChanges();
        });
    }
}
