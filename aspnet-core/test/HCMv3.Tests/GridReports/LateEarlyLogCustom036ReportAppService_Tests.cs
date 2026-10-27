using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.Storage;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Late/Early Log (Custom 036) grid report -- legacy
/// <c>uscRptLateEarlyLog_Custom036.ascx.cs</c>, <c>Table1()</c>.
///
/// Custom036 IS Custom033 with the two access-unit columns removed, and the two share one implementation
/// (<c>LateEarlyLogGeoVariantReportAppServiceBase</c>). The behavioural defects that implementation
/// reproduces are pinned once, in <see cref="LateEarlyLogCustom033ReportAppService_Tests"/>. What is
/// tested HERE is what is specific to this report: its own filter/paging/export path, and the fact that
/// its column set really is the smaller one.
/// </summary>
// ReSharper disable once InconsistentNaming
public class LateEarlyLogCustom036ReportAppService_Tests : LateEarlyLogCustomReportTestBase
{
    private readonly ILateEarlyLogCustom036ReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public LateEarlyLogCustom036ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ILateEarlyLogCustom036ReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private static GetLateEarlyLogCustom036ReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string payrollAreaCode = null,
        int maxResultCount = 1000)
    {
        return new GetLateEarlyLogCustom036ReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            PayrollAreaCode = payrollAreaCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    /// <summary>
    /// The window is INCLUSIVE at both ends and produces exactly one row per covered day; days either
    /// side of it must not appear.
    /// </summary>
    [Fact]
    public async Task Should_Include_Both_Boundary_Days_And_Nothing_Outside_The_Window()
    {
        var employee = CreateEmployee("C036-BOUND");
        var from = new DateTime(2024, 6, 10);
        var to = new DateTime(2024, 6, 12);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        foreach (var day in new[] { from.AddDays(-1), from, to, to.AddDays(1) })
        {
            CreatePunch(employee, day, TimeSpan.FromHours(8), PunchInCode);
            CreatePunch(employee, day, TimeSpan.FromHours(16), PunchOutCode);
        }

        var result = await _appService.GetLateEarlyLogCustom036Report(Input(from, to, "C036-BOUND"));

        result.Items.Count.ShouldBe(3);
        result.Items.Select(i => i.Date.Date).ShouldBe(new[] { from, from.AddDays(1), to }, ignoreOrder: true);
    }

    /// <summary>
    /// The shared algorithm still applies: the earliest punch in the flexi window is the Sign In, the
    /// latest one that is not at the same instant is the Sign Out, and Total Hours is their difference,
    /// written even when zero.
    /// </summary>
    [Fact]
    public async Task Should_Report_Sign_In_Sign_Out_And_Total_Hours_From_The_Flexi_Window()
    {
        var employee = CreateEmployee("C036-BASIC");
        var day = new DateTime(2024, 6, 10);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        CreatePunch(employee, day, TimeSpan.FromHours(9), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(15), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom036Report(Input(day, day, "C036-BASIC"));

        var row = result.Items.ShouldHaveSingleItem();
        row.SignIn.ShouldBe(TimeSpan.FromHours(9));
        row.SignOut.ShouldBe(TimeSpan.FromHours(15));
        row.TotalHours.ShouldBe(TimeSpan.FromHours(6));
        row.Status.ShouldBe("");
    }

    /// <summary>
    /// A week-off duty reports the Week Off status. Unlike Custom023 the punches are still selected, since
    /// this variant's punch block is not gated on the week-off flag.
    /// </summary>
    [Fact]
    public async Task Should_Report_Week_Off_And_Still_Select_Punches()
    {
        var employee = CreateEmployee("C036-WO");
        var day = new DateTime(2024, 6, 10);

        var dutyId = CreateDuty(TimeSpan.FromHours(8), TimeSpan.FromHours(16), isWeeklyOff: true);
        AssignDutyPeriod(employee, CreateDutyPeriod(dutyId), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        CreatePunch(employee, day, TimeSpan.FromHours(9), PunchInCode);
        CreatePunch(employee, day, TimeSpan.FromHours(15), PunchOutCode);

        var result = await _appService.GetLateEarlyLogCustom036Report(Input(day, day, "C036-WO"));

        var row = result.Items.ShouldHaveSingleItem();
        row.Status.ShouldBe("Week Off");
        row.SignIn.ShouldBe(TimeSpan.FromHours(9));
        row.TotalHours.ShouldBe(TimeSpan.FromHours(6));
    }

    /// <summary>
    /// An employee with no duty-period assignment at all contributes NO rows, whatever their punches say
    /// -- legacy's <c>dsCount &gt; 0</c> guard.
    /// </summary>
    [Fact]
    public async Task Should_Skip_An_Employee_With_No_Duty_Period_At_All()
    {
        var employee = CreateEmployee("C036-NODP");
        var day = new DateTime(2024, 6, 10);

        AssignTimePolicy(employee, CreateTimePolicy(), new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));
        CreatePunch(employee, day, TimeSpan.FromHours(9), PunchInCode);

        var result = await _appService.GetLateEarlyLogCustom036Report(Input(day, day, "C036-NODP"));

        result.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// The export must honour the filters and return EVERY matching row, ignoring the page size.
    /// </summary>
    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("C036-EXP");
        var from = new DateTime(2024, 6, 1);
        var to = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom036ReportToExcel(
            Input(from, to, "C036-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("LateEarlyLogCustom036.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        CountExportedRows(bytes).ShouldBe(5);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler and to a source grep.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("C036-HDR");
        var day = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom036ReportToExcel(Input(day, day, "C036-HDR"));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.ShouldNotBeEmpty();
        headers.Where(h => h.StartsWith("[") && h.EndsWith("]")).ShouldBeEmpty();
    }

    /// <summary>
    /// THE ONE DIFFERENCE FROM CUSTOM033, pinned: twenty columns rather than twenty-two, with the two
    /// access-unit columns absent. If a future edit made the two reports share a column list as well as an
    /// algorithm, this is the test that would catch it.
    /// </summary>
    [Fact]
    public async Task Should_Export_Two_Fewer_Columns_Than_Custom033_By_Dropping_The_Access_Units()
    {
        var employee = CreateEmployee("C036-COLS");
        var day = new DateTime(2024, 6, 5);

        GiveStandardDayShift(employee, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30));

        var file = await _appService.GetLateEarlyLogCustom036ReportToExcel(Input(day, day, "C036-COLS"));
        var headers = ReadExportedHeaders(_tempFileCacheManager.GetFile(file.FileToken));

        headers.Count.ShouldBe(20);

        // Sanity, so the ShouldNotContain assertions cannot pass vacuously.
        headers.ShouldContain("Code");
        headers.ShouldContain("Access Id");
        headers.ShouldContain("Total Hours");

        headers.ShouldNotContain("Punch In Access Unit");
        headers.ShouldNotContain("Punch Out Access Unit");
    }
}
