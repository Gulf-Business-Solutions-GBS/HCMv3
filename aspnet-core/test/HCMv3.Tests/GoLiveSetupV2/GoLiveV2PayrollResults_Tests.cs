using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Threading;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetupV2;
using HCMv3.GoLiveSetupV2.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetupV2;

/// <summary>
/// Step 3, seeing what the first payroll paid: a line per employee with earnings, deductions and net pay; the
/// Salary Register of one employee, every wage type with rate, number and amount; and the same register lines
/// for everyone as an Excel file, to set beside the legacy payroll.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2PayrollResults_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private static readonly DateTime PayDate = new(2026, 9, 30);
    private static readonly DateTime Joined = new(2020, 1, 15);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2PayrollResults_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    [Fact]
    public async Task Should_List_Each_Employee_With_What_The_Payroll_Paid()
    {
        await GivenTheFirstPayrollHasRun();

        var results = await _goLive.GetPayrollResults(Input());

        results.PayInDate.ShouldBe(PayDate);
        results.PayrollAreaId.ShouldNotBeNull();

        var paid = results.Employees.Single(e => e.EmployeeCode == "E001");
        paid.EmployeeName.ShouldBe("Ahmed Ali");
        paid.HasRegister.ShouldBeTrue();
        paid.NetPay.ShouldBe(500, 0.001);
        paid.Earnings.ShouldBe(500, 0.001);
        paid.Deductions.ShouldBe(0, 0.001);

        results.TotalNetPay.ShouldBe(500, 0.001);
        results.EmployeesPaid.ShouldBe(1);
    }

    /// <summary>Someone in the payroll area without a register this month is listed too -- as not paid.</summary>
    [Fact]
    public async Task Should_List_An_Employee_The_Payroll_Did_Not_Reach_As_Not_Paid()
    {
        await GivenTheFirstPayrollHasRun();
        await _goLive.LoadEmployees(Load(Row("E002", "Sara Khalid", "290011501235")));

        var results = await _goLive.GetPayrollResults(Input());

        var notPaid = results.Employees.Single(e => e.EmployeeCode == "E002");
        notPaid.HasRegister.ShouldBeFalse();
        notPaid.NetPay.ShouldBe(0);
        results.EmployeesPaid.ShouldBe(1);
    }

    /// <summary>The Salary Register: every wage type the engine wrote, with rate, number and amount.</summary>
    [Fact]
    public async Task Should_Show_One_Employees_Salary_Register()
    {
        await GivenTheFirstPayrollHasRun();
        var employeeId = (await _goLive.GetPayrollResults(Input())).Employees.Single(e => e.EmployeeCode == "E001").EmployeeId;

        var register = await _goLive.GetSalaryRegister(new GoLiveV2SalaryRegisterInput { PayDate = PayDate, EmployeeId = employeeId });

        register.EmployeeCode.ShouldBe("E001");
        register.ForPeriod.ShouldBe(PayDate);
        register.InPeriod.ShouldBe(PayDate);

        var basic = register.Lines.Single(l => l.WageCode == "1000");
        basic.WageName.ShouldBe("Basic Salary");
        basic.Group.ShouldBe(BillingType.Earning);
        basic.Amount.ShouldBe(500, 0.001);
        register.Lines.Single(l => l.WageCode == "9000").Amount.ShouldBe(500, 0.001);

        // The time engine's day counters are there too, as the payroll engine used them.
        register.Lines.ShouldContain(l => l.Group == BillingType.TimeType);

        // The annual leave provision: September's accrued days (30/365 of a year's 30) at a 26th of the pay
        // that is paid during annual leave. Zero -- and so missing -- before the catalogue said which pay that is.
        var accrued = register.Lines.Single(l => l.WageCode == "4010").Number;
        accrued.ShouldBeGreaterThan(0);
        register.Lines.Single(l => l.WageCode == "3001").Amount.ShouldBe(500d / 26 * accrued, 0.01);
    }

    /// <summary>
    /// A company seeded before the catalogue carried them has no "paid during leave" links, so its annual leave
    /// provision was priced at nothing and dropped. Run payroll adds the legacy links first, and says so.
    /// </summary>
    [Fact]
    public async Task Should_Add_The_Leave_Pay_Links_A_Company_Lacks_Before_Running()
    {
        GivenTheReferenceData();
        UsingDbContext(context =>
        {
            context.WageTypeLeaves.RemoveRange(context.WageTypeLeaves.Where(l => l.TenantId == TenantId));
            context.SaveChanges();
        });
        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234")));

        var run = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        run.Succeeded.ShouldBeTrue(run.Message);
        // The catalogue's own links: legacy's 230, and the grid's salary wage types for annual leave.
        run.LeavePayLinksAdded.ShouldBe(Resolve<IReferenceDataSeeder>().GetPack("payroll").WageTypeLeaves.Count);

        var employeeId = (await _goLive.GetPayrollResults(Input())).Employees.Single().EmployeeId;
        var register = await _goLive.GetSalaryRegister(new GoLiveV2SalaryRegisterInput { PayDate = PayDate, EmployeeId = employeeId });
        register.Lines.Single(l => l.WageCode == "3001").Amount.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Not_Touch_The_Leave_Pay_Links_A_Company_Already_Has()
    {
        await GivenTheFirstPayrollHasRun();

        var again = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });

        again.LeavePayLinksAdded.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_The_Register_Lines_For_Everyone_To_Excel()
    {
        await GivenTheFirstPayrollHasRun();

        var file = await _goLive.GetSalaryRegisterExcel(Input());

        file.ShouldNotBeNull();
        file.FileName.ShouldEndWith(".xlsx");
        file.FileToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Should_Show_Nothing_For_A_Month_Not_Yet_Run()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234")));

        var results = await _goLive.GetPayrollResults(Input());

        results.EmployeesPaid.ShouldBe(0);
        results.Employees.ShouldAllBe(e => !e.HasRegister);
    }

    // ---------------------------------------------------------------- arrange

    /// <summary>One employee on a basic salary of 500, loaded and paid for September 2026.</summary>
    private async Task GivenTheFirstPayrollHasRun()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(Row("E001", "Ahmed Ali", "290011501234")));
        var run = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = PayDate, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);
    }

    private void GivenTheReferenceData()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }
    }

    private static GoLiveV2PayrollResultsInput Input() => new() { PayDate = PayDate, PayrollAreaName = DefaultArea };

    private static GoLiveV2LoadEmployeesInput Load(params GoLiveV2EmployeeRowDto[] rows) =>
        new() { OrganizationUnitName = "ABC Holding", PayrollAreaName = DefaultArea, Employees = new List<GoLiveV2EmployeeRowDto>(rows) };

    private static GoLiveV2EmployeeRowDto Row(string code, string name, string civilId) =>
        new() { Code = code, FullName = name, JoiningDate = Joined, CivilId = civilId, BasicSalary = 500 };
}
