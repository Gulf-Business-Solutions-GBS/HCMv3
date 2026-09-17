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
/// A joining date mistyped on the sheet, paid, then corrected and loaded again: every record the load dated from
/// the old joining date moves with it -- including the duty period and time policy the time engine needs for each
/// day -- so the next run pays the corrected month. Made-up figures only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2JoiningDateCorrection_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime September = new(2026, 9, 30);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2JoiningDateCorrection_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    [Fact]
    public async Task Should_Pay_The_Whole_Month_After_A_Late_Joining_Date_Is_Corrected()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Load(new DateTime(2026, 9, 15)));
        var first = await _goLive.RunPayroll(Run());
        first.Succeeded.ShouldBeTrue(first.Message);
        (await Days("4011")).ShouldBe(16);

        await _goLive.LoadEmployees(Load(new DateTime(2026, 9, 1)));
        var second = await _goLive.RunPayroll(Run());

        second.Succeeded.ShouldBeTrue(second.Message);
        (await Days("4011")).ShouldBe(30);

        UsingDbContext(context => context.EmployeeDutyPeriods
                .Where(p => p.Employee.Code == "E001" && !p.IsDeleted)
                .Select(p => p.StartDate)
                .ToList())
            .ShouldBe(new[] { new DateTime(2026, 9, 1) });
        UsingDbContext(context => context.EmployeeTimePolicies
                .Where(p => p.Employee.Code == "E001" && !p.IsDeleted)
                .Select(p => p.StartDate)
                .ToList())
            .ShouldBe(new[] { new DateTime(2026, 9, 1) });
    }

    // ---------------------------------------------------------------- arrange

    private static GoLiveV2RunPayrollInput Run() =>
        new() { PayDate = September, PayrollAreaName = DefaultArea, EmployeeCodes = new() { "E001" } };

    private async Task<double> Days(string wageCode)
    {
        var employeeId = UsingDbContext(context => context.Employees.Single(e => e.Code == "E001" && !e.IsDeleted).Id);
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

    private static GoLiveV2LoadEmployeesInput Load(DateTime joined) =>
        new()
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = DefaultArea,
            Employees = new()
            {
                new GoLiveV2EmployeeRowDto
                {
                    Code = "E001",
                    FullName = "Employee E001",
                    JoiningDate = joined,
                    CivilId = "290011501000",
                    BasicSalary = 500,
                },
            },
        };
}
