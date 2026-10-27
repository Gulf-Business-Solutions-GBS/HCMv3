using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Time Analysis By Department grid report (legacy <c>uscRptTimeAnalysisByDept.ascx</c>,
/// <c>Table1()</c>).
///
/// The report is a DEPARTMENT ROLL-UP with a deliberately inert measure: legacy built the row set from
/// the time registers in the window, then wrote <c>TotalValue = 0</c> and <c>Period = dtTo</c> on every
/// row. That is reproduced rather than "fixed", and pinned by
/// <see cref="Should_Report_Zero_Total_Value_And_The_To_Date_As_Period_Exactly_As_Legacy_Did"/>.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeAnalysisByDeptReportAppService_Tests : AppTestBase
{
    private readonly ITimeAnalysisByDeptReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public TimeAnalysisByDeptReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ITimeAnalysisByDeptReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });

        return employee;
    }

    private void CreateTimeRegister(Employee employee, DateTime dayDate)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DayDate = dayDate
            });
        });
    }

    private static GetTimeAnalysisByDeptReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetTimeAnalysisByDeptReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    /// <summary>
    /// Legacy lines 483-484 are <c>DayDate >= dtFrom &amp;&amp; DayDate &lt;= dtTo</c>, so an employee whose only
    /// register rows sit outside the window contributes nothing at all.
    /// </summary>
    [Fact]
    public async Task Should_Only_Count_Employees_With_A_Register_Row_Inside_The_Window()
    {
        var inside = CreateEmployee("TAD-IN");
        var outside = CreateEmployee("TAD-OUT");

        CreateTimeRegister(inside, new DateTime(2024, 6, 1));
        CreateTimeRegister(outside, new DateTime(2024, 7, 1));

        var insideOnly = await _appService.GetTimeAnalysisByDeptReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TAD-IN"));
        var outsideOnly = await _appService.GetTimeAnalysisByDeptReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TAD-OUT"));

        insideOnly.TotalCount.ShouldBe(1);
        outsideOnly.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Include_The_Days_On_Both_Window_Boundaries()
    {
        var onFromDate = CreateEmployee("TAD-EDGE-FROM");
        var onToDate = CreateEmployee("TAD-EDGE-TO");

        CreateTimeRegister(onFromDate, new DateTime(2024, 6, 1));
        CreateTimeRegister(onToDate, new DateTime(2024, 6, 30));

        var result = await _appService.GetTimeAnalysisByDeptReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TAD-EDGE-FROM,TAD-EDGE-TO"));

        // Both employees fall in the same (unclassified) department, so they roll up into one row.
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var active = CreateEmployee("TAD-ST-ACTIVE");
        var inactive = CreateEmployee("TAD-ST-INACTIVE", EmployeeStatus.Inactive);

        var day = new DateTime(2024, 6, 10);
        CreateTimeRegister(active, day);
        CreateTimeRegister(inactive, day);

        var activeOnly = await _appService.GetTimeAnalysisByDeptReport(
            Input(day, day, employeeCode: "TAD-ST-ACTIVE", status: EmployeeStatus.Inactive));

        activeOnly.TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// LEGACY BUG REPRODUCED. Legacy lines 520-537 wrote a constant <c>TotalValue = 0</c> on every
    /// department row and set <c>Period</c> to the report's TO date rather than to anything derived from
    /// the data. The measure column is therefore always zero. The standing rule for this wave is legacy
    /// fidelity, so it is reproduced and pinned here rather than silently "corrected".
    /// </summary>
    [Fact]
    public async Task Should_Report_Zero_Total_Value_And_The_To_Date_As_Period_Exactly_As_Legacy_Did()
    {
        var employee = CreateEmployee("TAD-CONST");
        CreateTimeRegister(employee, new DateTime(2024, 6, 10));

        var toDate = new DateTime(2024, 6, 30);
        var result = await _appService.GetTimeAnalysisByDeptReport(
            Input(new DateTime(2024, 6, 1), toDate, employeeCode: "TAD-CONST"));

        var row = result.Items.Single();
        row.TotalValue.ShouldBe(0);
        row.Period.ShouldBe(toDate);
    }

    /// <summary>
    /// Several employees sharing a department collapse to ONE row -- the row set is the DISTINCT set of
    /// department names, not one row per employee.
    /// </summary>
    [Fact]
    public async Task Should_Roll_Several_Employees_Up_Into_One_Department_Row()
    {
        var first = CreateEmployee("TAD-ROLL-A");
        var second = CreateEmployee("TAD-ROLL-B");

        var day = new DateTime(2024, 6, 11);
        CreateTimeRegister(first, day);
        CreateTimeRegister(second, day);

        var result = await _appService.GetTimeAnalysisByDeptReport(
            Input(day, day, employeeCode: "TAD-ROLL-A,TAD-ROLL-B"));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().DepartmentName.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("TAD-EXP");
        CreateTimeRegister(employee, new DateTime(2024, 6, 12));

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetTimeAnalysisByDeptReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TAD-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("TimeAnalysisByDept.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(1);
        exported.First().Keys.ShouldContain("Department");
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("TAD-HDR");
        CreateTimeRegister(employee, new DateTime(2024, 6, 13));

        var file = await _appService.GetTimeAnalysisByDeptReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TAD-HDR"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
