using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.GeneralMasterInfo;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetupV2;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetupV2;

/// <summary>
/// Go-Live Setup V2 is built backwards from its goal: press Run Payroll, see what the engine refuses on, and
/// make each refusal the next step. These tests pin that chain one link at a time, in the order a brand-new
/// tenant meets them. Each new link is written here first, while it still fails.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2RunPayroll_Tests : AppTestBase
{
    private static readonly DateTime PayDate = new(2026, 9, 30);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2RunPayroll_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- link 1: nothing given

    [Fact]
    public async Task Should_Stop_On_The_Pay_Date_When_Nothing_Is_Given()
    {
        var result = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput());

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveV2StoppedAt.Input);
        result.Message.ShouldBe("No Pay Date defined for Payroll Calculation. Please select a Pay Date.");
    }

    // ---------------------------------------------------------------- link 2: a date, but no payroll area

    [Fact]
    public async Task Should_Stop_On_The_Payroll_Area_When_Only_A_Date_Is_Given()
    {
        var result = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = PayDate });

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveV2StoppedAt.Input);
        result.Message.ShouldBe("Payroll Area is required to run the Payroll Engine.");
    }

    // ---------------------------------------------------------------- link 3: the engine itself refuses

    [Fact]
    public async Task Should_Report_The_Engines_Own_Words_When_It_Refuses_To_Start()
    {
        // A payroll area the tenant does not have. The engine looks for its control record first.
        var result = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput
        {
            PayDate = PayDate,
            PayrollAreaName = "Nowhere",
        });

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveV2StoppedAt.Engine);
        result.Message.ShouldBe("Payroll Control Record not found, Cannot Run Payroll Engine.");
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

        var result = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput
        {
            PayDate = PayDate,
            PayrollAreaName = area.Name,
        });

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveV2StoppedAt.NoEmployees);
        result.Employees.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------- link n: the run starts, an employee fails

    /// <summary>
    /// Once the run itself is allowed, refusals move down to individual employees. The run does not stop for
    /// them -- each is logged and the next is tried -- so V2 has to report them per employee rather than as
    /// one message.
    /// </summary>
    [Fact]
    public async Task Should_Report_Each_Employee_The_Engine_Could_Not_Pay()
    {
        var area = GivenARunnablePayrollArea("Lab");
        GivenAnEmployeeInPayrollArea(area, "V2-001");

        var result = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput
        {
            PayDate = PayDate,
            PayrollAreaName = area.Name,
        });

        result.Succeeded.ShouldBeFalse();
        result.StoppedAt.ShouldBe(GoLiveV2StoppedAt.Employees);

        var employee = result.Employees.Single();
        employee.EmployeeCode.ShouldBe("V2-001");
        employee.Succeeded.ShouldBeFalse();
        employee.Message.ShouldContain("Employee Contract not found");
    }

    [Fact]
    public async Task Should_Normalise_The_Pay_Date_To_The_Month_End_The_Engine_Uses()
    {
        var area = GivenARunnablePayrollArea("Lab");
        GivenAnEmployeeInPayrollArea(area, "V2-001");

        var result = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput
        {
            PayDate = new DateTime(2026, 9, 12),
            PayrollAreaName = area.Name,
        });

        result.StoppedAt.ShouldBe(GoLiveV2StoppedAt.Employees);
        result.PayInDate.ShouldBe(PayDate);
    }

    // ---------------------------------------------------------------- arrange

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
