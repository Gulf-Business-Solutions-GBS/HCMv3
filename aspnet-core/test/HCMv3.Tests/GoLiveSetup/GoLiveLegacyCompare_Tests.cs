using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.Threading;
using Abp.UI;
using HCMv3.GoLiveSetup.ReferenceData;
using HCMv3.GoLiveSetup;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.PayrollManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GoLiveSetup;

/// <summary>
/// Compare with legacy salary: the legacy payroll's registers for a month, exported by a read-only query,
/// set beside HCMv3's wage type by wage type, with the likely reason for each difference.
///
/// <para>Every "legacy" file here is made up from HCMv3's own results, changed where a test needs a
/// difference. No real salary is used.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class GoLiveLegacyCompare_Tests : AppTestBase
{
    private const string DefaultArea = "Default Payroll Area";
    private const string Header = "EmployeeCode,PayMonth,WageTypeCode,WageTypeName,BillingType,Rate,Number,Amount";
    private static readonly DateTime September = new(2026, 9, 30);

    private readonly IGoLiveSetupAppService _goLive;

    public GoLiveLegacyCompare_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _goLive = Resolve<IGoLiveSetupAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>
    /// A legacy month paid exactly as HCMv3 paid it matches line for line; rows for another month and rows with
    /// no codes are left out and counted.
    /// </summary>
    [Fact]
    public async Task Should_Match_Every_Line_Paid_The_Same_And_Leave_Out_Other_Months()
    {
        var lines = await GivenAPaidEmployee("E001");
        var csv = Csv(lines.Select(l => Row("E001", l.WageCode, l.Group, l.Rate, l.Number, l.Amount)))
                  + "\nE001,2026-08-31,1000,Basic Salary,Earning,1,1,1"
                  + "\n,2026-09-30,,,,,,";

        var result = await Compare(csv, "legacy.csv");

        result.Employees.ShouldBe(1);
        result.RowsForOtherMonths.ShouldBe(1);
        result.RowsUnreadable.ShouldBe(1);
        result.Different.ShouldBe(0);
        result.OnlyInLegacy.ShouldBe(0);
        result.OnlyInHcmv3.ShouldBe(0);
        result.Matched.ShouldBe(lines.Count);
    }

    /// <summary>
    /// Each kind of difference gets its own reason: a different salary record is data, different days are days,
    /// a small gap is rounding, net pay follows the lines it adds up, a wage type HCMv3 lacks is setup, and an
    /// employee HCMv3 lacks is data.
    /// </summary>
    [Fact]
    public async Task Should_Give_Each_Kind_Of_Difference_Its_Reason()
    {
        var lines = await GivenAPaidEmployee("E001");
        var timeType = lines.FirstOrDefault(l => l.Group == BillingType.TimeType && l.WageCode != "9000");
        var provision = lines.FirstOrDefault(l => l.Group == BillingType.Provision);
        timeType.ShouldNotBeNull("the month's register has no time type line to change");
        provision.ShouldNotBeNull("the month's register has no provision line to change");

        var rows = lines.Select(l => l.WageCode switch
        {
            "1000" => Row("E001", l.WageCode, l.Group, l.Rate + 100, l.Number, l.Amount + 100),
            "9000" => Row("E001", l.WageCode, l.Group, l.Rate, l.Number, l.Amount + 100),
            _ when l == timeType => Row("E001", l.WageCode, l.Group, l.Rate, l.Number + 2, l.Amount),
            _ when l == provision => Row("E001", l.WageCode, l.Group, l.Rate, l.Number, l.Amount + 0.005),
            _ => Row("E001", l.WageCode, l.Group, l.Rate, l.Number, l.Amount),
        }).ToList();
        rows.Add(Row("E001", "ZZ99", BillingType.Earning, 10, 1, 10));
        rows.Add(Row("X999", "1000", BillingType.Earning, 10, 30, 10));

        var result = await Compare(Csv(rows), "legacy.csv");

        Line(result, "E001", "1000").Cause.ShouldBe(GoLiveCompareCause.Data);
        Line(result, "E001", timeType.WageCode).Cause.ShouldBe(GoLiveCompareCause.Days);
        Line(result, "E001", provision.WageCode).Cause.ShouldBe(GoLiveCompareCause.Rounding);
        Line(result, "E001", "9000").Cause.ShouldBe(GoLiveCompareCause.FollowsOtherLines);
        Line(result, "E001", "ZZ99").Cause.ShouldBe(GoLiveCompareCause.Setup);
        Line(result, "E001", "ZZ99").Status.ShouldBe(GoLiveCompareStatus.OnlyInLegacy);
        Line(result, "X999", "1000").Cause.ShouldBe(GoLiveCompareCause.Data);
        Line(result, "X999", "1000").Reason.ShouldBe(Localized("GoLiveCompareEmployeeNotLoaded"));
    }

    /// <summary>
    /// Matched means rate, number and amount all agree. The same amount reached with a different rate is a
    /// difference, and each value says on its own whether it matched.
    /// </summary>
    [Fact]
    public async Task Should_Not_Call_A_Line_Matched_When_Only_The_Amount_Agrees()
    {
        var lines = await GivenAPaidEmployee("E001");
        var csv = Csv(lines.Select(l => l.WageCode == "1000"
            ? Row("E001", l.WageCode, l.Group, l.Rate + 1, l.Number, l.Amount)
            : Row("E001", l.WageCode, l.Group, l.Rate, l.Number, l.Amount)));

        var result = await Compare(csv, "legacy.csv");

        var basic = Line(result, "E001", "1000");
        basic.Status.ShouldBe(GoLiveCompareStatus.Different);
        basic.RateMatches.ShouldBeFalse();
        basic.NumberMatches.ShouldBeTrue();
        basic.AmountMatches.ShouldBeTrue();
        result.Matched.ShouldBe(lines.Count - 1);
    }

    /// <summary>A wage type HCMv3 paid that the legacy payroll did not is listed as only in HCMv3.</summary>
    [Fact]
    public async Task Should_List_What_Only_HCMv3_Paid()
    {
        var lines = await GivenAPaidEmployee("E001");
        var csv = Csv(lines.Where(l => l.WageCode != "1000").Select(l => Row("E001", l.WageCode, l.Group, l.Rate, l.Number, l.Amount)));

        var result = await Compare(csv, "legacy.csv");

        var basic = Line(result, "E001", "1000");
        basic.Status.ShouldBe(GoLiveCompareStatus.OnlyInHcmv3);
        basic.Cause.ShouldBe(GoLiveCompareCause.Data);
    }

    /// <summary>The query comes filled in with the month and the payroll area's employee codes.</summary>
    [Fact]
    public async Task Should_Fill_The_Export_Query_With_The_Month_And_The_Employees()
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load("E001", "E002"));

        var query = await _goLive.GetLegacySalaryExportQuery(
            new GoLivePayrollResultsInput { PayDate = new DateTime(2026, 9, 12), PayrollAreaName = DefaultArea });

        query.Sql.ShouldContain("DECLARE @PayMonth        date          = '2026-09-30';");
        query.Sql.ShouldContain("DECLARE @EmployeeCodes   nvarchar(max) = N'E001,E002';");
        query.Sql.ShouldNotContain("INSERT", Case.Insensitive);
        query.FileName.ShouldBe("Legacy Salary Register 2026-09.sql");
    }

    /// <summary>A file with nothing readable in it is refused in words, not compared as empty.</summary>
    [Fact]
    public async Task Should_Refuse_A_File_With_No_Rows()
    {
        var refusal = await Should.ThrowAsync<UserFriendlyException>(() => Compare("\n\n", "legacy.csv"));

        refusal.Message.ShouldBe(Localized("GoLiveCompareFileEmpty"));
    }

    // ---------------------------------------------------------------- arrange

    private async Task<List<GoLiveSalaryRegisterLineDto>> GivenAPaidEmployee(string code)
    {
        GivenTheReferenceData();
        await _goLive.LoadEmployees(Load(code));
        var run = await _goLive.RunPayroll(new GoLiveRunPayrollInput { PayDate = September, PayrollAreaName = DefaultArea });
        run.Succeeded.ShouldBeTrue(run.Message);

        var employeeId = UsingDbContext(context => context.Employees.Single(e => e.Code == code && !e.IsDeleted).Id);
        var register = await _goLive.GetSalaryRegister(new GoLiveSalaryRegisterInput { PayDate = September, EmployeeId = employeeId });
        return register.Lines;
    }

    private Task<GoLiveLegacyCompareOutput> Compare(string content, string fileName) =>
        _goLive.CompareWithLegacySalary(new GoLiveLegacyCompareInput
        {
            PayDate = September,
            PayrollAreaName = DefaultArea,
            FileName = fileName,
            Base64Content = Convert.ToBase64String(Encoding.UTF8.GetBytes(content)),
        });

    private static GoLiveLegacyCompareLineDto Line(GoLiveLegacyCompareOutput result, string employee, string wage) =>
        result.Lines.SingleOrDefault(l => l.EmployeeCode == employee && l.WageTypeCode == wage)
        ?? throw new ShouldAssertException($"no line for {employee} / {wage}");

    private static string Row(string employee, string wage, BillingType group, double rate, double number, double amount) =>
        string.Join(",",
            employee, "2026-09-30", wage, "Wage " + wage, group,
            rate.ToString("R", CultureInfo.InvariantCulture),
            number.ToString("R", CultureInfo.InvariantCulture),
            amount.ToString("R", CultureInfo.InvariantCulture));

    private static string Csv(IEnumerable<string> rows) => Header + "\n" + string.Join("\n", rows);

    private void GivenTheReferenceData()
    {
        var seeder = Resolve<IReferenceDataSeeder>();
        foreach (var pack in new[] { "universal", "kuwait", "payroll" })
        {
            AsyncHelper.RunSync(() => seeder.SeedPackAsync(pack, TenantId));
        }
    }

    private static GoLiveLoadEmployeesInput Load(params string[] codes) =>
        new()
        {
            OrganizationUnitName = "ABC Holding",
            PayrollAreaName = DefaultArea,
            Employees = codes
                .Select((code, i) => new GoLiveEmployeeRowDto
                {
                    Code = code,
                    FullName = "Employee " + code,
                    JoiningDate = new DateTime(2020, 1, 15),
                    CivilId = "29001150" + (1000 + i),
                    BasicSalary = 500,
                })
                .ToList(),
        };

    private string Localized(string key, params object[] args) =>
        string.Format(Resolve<ILocalizationManager>().GetString(HCMv3Consts.LocalizationSourceName, key), args);
}
