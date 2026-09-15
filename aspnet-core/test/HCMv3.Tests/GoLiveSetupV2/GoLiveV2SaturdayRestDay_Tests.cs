using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetupV2;
using HCMv3.GoLiveSetupV2.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetupV2;

/// <summary>
/// "Saturday is a rest day" next to the company: some companies give Saturday as a rest day and some do not, and
/// the time engine can only count Rest Days (4009) from a working week that says so. Made-up figures only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2SaturdayRestDay_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime September = new(2026, 9, 30);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2SaturdayRestDay_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>Ticked, every employee gets the working week with Saturday as a rest day, and the step reopens ticked.</summary>
    [Fact]
    public async Task Should_Give_The_Rest_Day_Week_When_Ticked()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Load(saturdayIsRestDay: true, "E001"));

        DutyPeriodOf("E001").ShouldBe(GoLiveV2WorkingTime.RestDayDutyPeriodName);
        (await _goLive.GetLoadedEmployees(DefaultArea)).SaturdayIsRestDay.ShouldBeTrue();
    }

    /// <summary>Loading again unticked puts the employees back on the week without it.</summary>
    [Fact]
    public async Task Should_Switch_The_Week_When_The_Choice_Changes()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(saturdayIsRestDay: true, "E001"));

        await _goLive.LoadEmployees(Load(saturdayIsRestDay: false, "E001"));

        DutyPeriodOf("E001").ShouldBe(GoLiveV2WorkingTime.DutyPeriodName);
        (await _goLive.GetLoadedEmployees(DefaultArea)).SaturdayIsRestDay.ShouldBeFalse();
    }

    /// <summary>
    /// September 2026 has four Fridays and four Saturdays: ticked, the month counts four weekly offs (4008) and
    /// four rest days (4009).
    /// </summary>
    [Fact]
    public async Task Should_Count_Saturdays_As_Rest_Days()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(saturdayIsRestDay: true, "E001"));

        var run = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);

        (await Days("E001", "4009")).ShouldBe(4);
        (await Days("E001", "4008")).ShouldBe(4);
    }

    // ---------------------------------------------------------------- arrange

    private string DutyPeriodOf(string code) =>
        UsingDbContext(context => context.EmployeeDutyPeriods
            .Where(p => p.Employee.Code == code && !p.IsDeleted)
            .Select(p => p.DutyPeriod.NameEn)
            .Single());

    private async Task<double> Days(string code, string wageCode)
    {
        var employeeId = UsingDbContext(context => context.Employees.Single(e => e.Code == code && !e.IsDeleted).Id);
        var register = await _goLive.GetSalaryRegister(new GoLiveV2SalaryRegisterInput { PayDate = September, EmployeeId = employeeId });
        return register.Lines.Where(l => l.WageCode == wageCode).Sum(l => l.Number);
    }

    private void GivenTheReferenceData()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }
    }

    private static GoLiveV2LoadEmployeesInput Load(bool saturdayIsRestDay, params string[] codes) =>
        new()
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = DefaultArea,
            SaturdayIsRestDay = saturdayIsRestDay,
            Employees = codes
                .Select((code, i) => new GoLiveV2EmployeeRowDto
                {
                    Code = code,
                    FullName = "Employee " + code,
                    JoiningDate = new DateTime(2020, 1, 15),
                    CivilId = "29001150" + (1000 + i),
                    BasicSalary = 500,
                })
                .ToList(),
        };
}
