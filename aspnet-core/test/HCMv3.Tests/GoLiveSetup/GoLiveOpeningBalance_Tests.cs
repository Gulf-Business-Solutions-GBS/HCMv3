using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// The annual leave opening balance column of Load Employee Data: kept as the Annual Leave Opening Balance time
/// transfer the engines read, and counted in the first payroll month's accrual so the leave provision starts from
/// the balance the employee brings. Made-up figures only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveOpeningBalance_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private const string Company = "ABC Holding";
    private static readonly DateTime September = new(2026, 9, 30);

    private readonly IGoLiveSetupAppService _goLive;

    public GoLiveOpeningBalance_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>A balance on the sheet becomes one type "1" time transfer, and the step reopens with it.</summary>
    [Fact]
    public async Task Should_Keep_The_Opening_Balance_As_An_Annual_Leave_Time_Transfer()
    {
        GivenTheReferenceData();

        await _goLive.LoadEmployees(Load(("E001", 12.5)));

        var transfers = Transfers("E001");
        transfers.ShouldHaveSingleItem();
        transfers[0].TransferCount.ShouldBe(12.5);
        transfers[0].TypeCode.ShouldBe("1");
        (await _goLive.GetLoadedEmployees(DefaultArea)).Employees.Single().AnnualLeaveOpeningBalance.ShouldBe(12.5);
    }

    /// <summary>Loading again corrects the balance to the sheet, and an emptied cell removes it.</summary>
    [Fact]
    public async Task Should_Correct_The_Balance_On_A_Reload_And_Remove_It_When_Emptied()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(("E001", 12.5)));

        await _goLive.LoadEmployees(Load(("E001", 10)));
        Transfers("E001").Single().TransferCount.ShouldBe(10);

        await _goLive.LoadEmployees(Load(("E001", null)));
        Transfers("E001").ShouldBeEmpty();
    }

    /// <summary>
    /// Run payroll dates the balance on the first day of the month, so the month's accrual carries it: the same
    /// employee with ten days brought forward accrues exactly ten days more than one with none.
    /// </summary>
    [Fact]
    public async Task Should_Count_The_Opening_Balance_In_The_First_Month_Accrual()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(("E001", 10), ("E002", null)));

        var run = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);

        Transfers("E001").Single().TransferDate.ShouldBe(new DateTime(2026, 9, 1));
        (await Days("E001", "4010") - await Days("E002", "4010")).ShouldBe(10, 0.001);
    }

    /// <summary>
    /// Re-running after the balance changes: the closing balance and the leave provisions follow the new
    /// accrual in the same run, not the one before it. They are worked out from 4010 after it is rewritten,
    /// so a stale read of 4010 left them one run behind.
    /// </summary>
    [Fact]
    public async Task Should_Follow_A_Changed_Opening_Balance_In_The_Same_Rerun()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(("E001", 8.88)));
        (await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea })).Succeeded.ShouldBeTrue();

        await _goLive.LoadEmployees(Load(("E001", 8.888)));
        var rerun = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        rerun.Succeeded.ShouldBeTrue(rerun.Message);

        var accrual = await Days("E001", "4010");
        (await Days("E001", "4022")).ShouldBe(accrual, 0.000001);
        (await Days("E001", "3001")).ShouldBe(accrual, 0.000001);
        (await Days("E001", "3004")).ShouldBe(accrual, 0.000001);
    }

    /// <summary>Clear takes the balance with the employee, so a fresh load does not find an old one.</summary>
    [Fact]
    public async Task Should_Remove_The_Balance_When_The_Company_Is_Cleared()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(("E001", 12.5)));

        await _goLive.ClearCompany(new GoLiveClearCompanyInput { PayrollAreaName = DefaultArea, ConfirmCompanyName = Company });

        UsingDbContext(context => context.TimeTransfers.Count(t => t.TenantId == TenantId && !t.IsDeleted)).ShouldBe(0);
    }

    // ---------------------------------------------------------------- arrange

    private async Task<double> Days(string code, string wageCode)
    {
        var employeeId = UsingDbContext(context => context.Employees.Single(e => e.Code == code && !e.IsDeleted).Id);
        var register = await _goLive.GetSalaryRegister(new GoLiveSalaryRegisterInput { PayDate = September, EmployeeId = employeeId });
        return register.Lines.Where(l => l.WageCode == wageCode).Sum(l => l.Number);
    }

    private (double TransferCount, string TypeCode, DateTime TransferDate)[] Transfers(string code) =>
        UsingDbContext(context => context.TimeTransfers
            .Where(t => t.Employee.Code == code && !t.IsDeleted && t.Comments == GoLiveOpeningBalances.Comment)
            .Select(t => new { t.TransferCount, t.TransferType.Code, t.TransferDate })
            .ToList()
            .Select(t => (t.TransferCount, t.Code, t.TransferDate))
            .ToArray());

    private void GivenTheReferenceData()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }
    }

    private static GoLiveLoadEmployeesInput Load(params (string Code, double? OpeningBalance)[] employees) =>
        new()
        {
            OrganizationUnitName = Company,
            PayrollAreaName = DefaultArea,
            Employees = employees
                .Select((e, i) => new GoLiveEmployeeRowDto
                {
                    Code = e.Code,
                    FullName = "Employee " + e.Code,
                    JoiningDate = new DateTime(2020, 1, 15),
                    CivilId = "29001150" + (1000 + i),
                    BasicSalary = 500,
                    AnnualLeaveOpeningBalance = e.OpeningBalance,
                })
                .ToList(),
        };
}
