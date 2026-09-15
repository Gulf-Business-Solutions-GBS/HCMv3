using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
/// From a comparison line the legacy payroll paid and HCMv3 did not, to a payment on the Payment screen that the
/// next run pays -- for one-off earnings and deductions only, never a salary wage type. Made-up figures only.
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveV2LegacyPayments_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private const string Header = "EmployeeCode,PayMonth,WageTypeCode,WageTypeName,BillingType,Rate,Number,Amount";
    private static readonly DateTime September = new(2026, 9, 30);

    private readonly IGoLiveV2AppService _goLive;

    public GoLiveV2LegacyPayments_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveV2AppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>
    /// A missing bonus can be added as a payment and a missing housing allowance cannot -- it belongs on the salary
    /// record; an employee code HCMv3 does not have is listed on its own.
    /// </summary>
    [Fact]
    public async Task Should_Offer_A_Payment_For_A_Missing_Bonus_But_Not_For_A_Salary_Wage_Type()
    {
        var csv = await GivenAPaidEmployeeAndALegacyFile();

        var result = await Compare(csv);

        Line(result, "E001", "1053").CanAddPayment.ShouldBeTrue();
        Line(result, "E001", "1001").CanAddPayment.ShouldBeFalse();
        result.LegacyEmployeesNotInHcmv3.ShouldBe(new[] { "X999" });
    }

    /// <summary>
    /// Added, the bonus is on the Payment screen for the month; the next run pays it, so the line matches; adding it
    /// again changes nothing. The housing allowance is refused and nothing is made for it.
    /// </summary>
    [Fact]
    public async Task Should_Add_The_Payment_So_The_Next_Run_Pays_It()
    {
        var csv = await GivenAPaidEmployeeAndALegacyFile();

        var added = await _goLive.AddLegacyPayments(new GoLiveV2LegacyPaymentsInput
        {
            PayDate = September,
            Lines = new List<GoLiveV2LegacyPaymentLineDto>
            {
                new() { EmployeeCode = "E001", WageTypeCode = "1053", Amount = 100 },
                new() { EmployeeCode = "E001", WageTypeCode = "1001", Amount = 50 },
            },
        });

        added.Added.ShouldBe(1);
        added.Refused.ShouldHaveSingleItem().WageTypeCode.ShouldBe("1001");
        UsingDbContext(context => context.EmployeePayments.Count(p => p.TenantId == TenantId && !p.IsDeleted && p.Comments == GoLiveV2LegacyPayments.Comment))
            .ShouldBe(1);

        var run = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);

        Line(await Compare(csv), "E001", "1053").Status.ShouldBe(GoLiveV2CompareStatus.Matched);

        var again = await _goLive.AddLegacyPayments(new GoLiveV2LegacyPaymentsInput
        {
            PayDate = September,
            Lines = new List<GoLiveV2LegacyPaymentLineDto> { new() { EmployeeCode = "E001", WageTypeCode = "1053", Amount = 100 } },
        });
        again.AlreadyThere.ShouldBe(1);
        again.Added.ShouldBe(0);
    }

    // ---------------------------------------------------------------- arrange

    /// <summary>
    /// One employee paid for September, and a legacy file of exactly what HCMv3 paid, plus a bonus and a housing
    /// allowance HCMv3 did not pay, and an employee HCMv3 does not have.
    /// </summary>
    private async Task<string> GivenAPaidEmployeeAndALegacyFile()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(new GoLiveV2LoadEmployeesInput
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = DefaultArea,
            Employees = new List<GoLiveV2EmployeeRowDto>
            {
                new()
                {
                    Code = "E001",
                    FullName = "Employee E001",
                    JoiningDate = new DateTime(2020, 1, 15),
                    CivilId = "290011501000",
                    BasicSalary = 500,
                },
            },
        });

        var run = await _goLive.RunPayroll(new GoLiveV2RunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);

        var employeeId = UsingDbContext(context => context.Employees.Single(e => e.Code == "E001" && !e.IsDeleted).Id);
        var register = await _goLive.GetSalaryRegister(new GoLiveV2SalaryRegisterInput { PayDate = September, EmployeeId = employeeId });

        var rows = register.Lines.Select(l => Row("E001", l.WageCode, l.Group, l.Rate, l.Number, l.Amount)).ToList();
        rows.Add(Row("E001", "1053", BillingType.Earning, 0, 0, 100));
        rows.Add(Row("E001", "1001", BillingType.Earning, 50, 30, 50));
        rows.Add(Row("X999", "1053", BillingType.Earning, 0, 0, 10));
        return Header + "\n" + string.Join("\n", rows);
    }

    private Task<GoLiveV2LegacyCompareOutput> Compare(string csv) =>
        _goLive.CompareWithLegacySalary(new GoLiveV2LegacyCompareInput
        {
            PayDate = September,
            PayrollAreaName = DefaultArea,
            FileName = "legacy.csv",
            Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(csv)),
        });

    private static GoLiveV2LegacyCompareLineDto Line(GoLiveV2LegacyCompareOutput result, string employee, string wage) =>
        result.Lines.SingleOrDefault(l => l.EmployeeCode == employee && l.WageTypeCode == wage)
        ?? throw new ShouldAssertException($"no line for {employee} / {wage}");

    private static string Row(string employee, string wage, BillingType group, double rate, double number, double amount) =>
        string.Join(",",
            employee, "2026-09-30", wage, "Wage " + wage, group,
            rate.ToString("R", CultureInfo.InvariantCulture),
            number.ToString("R", CultureInfo.InvariantCulture),
            amount.ToString("R", CultureInfo.InvariantCulture));

    private void GivenTheReferenceData()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }
    }
}
