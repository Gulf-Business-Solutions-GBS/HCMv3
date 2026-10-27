using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Timing;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.TimeReport;
using HCMv3.GridReports.TimeReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using HCMv3.TimeManagement;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Time Register Approval grid report (legacy <c>uscTimeRegisterApproval.ascx</c>,
/// <c>Table1()</c> lines 409-505).
///
/// The screen is READ-ONLY despite its name -- legacy has no persistence path at all, it only shows the
/// raw and the APPROVED value of each deduction side by side. These tests therefore only pin reads.
/// </summary>
// ReSharper disable once InconsistentNaming
public class TimeRegisterApprovalReportAppService_Tests : AppTestBase
{
    private readonly ITimeRegisterApprovalReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public TimeRegisterApprovalReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ITimeRegisterApprovalReportAppService>();
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

    private AbsenceType CreateAbsenceType(string code)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                IsActive = true
            }).Entity;
        });

        return absenceType;
    }

    private void CreateTimeRegister(
        Employee employee,
        DateTime dayDate,
        double leaveDay = 0,
        int? leaveTypeId = null,
        double dedLateInEarlyOutMins = 0,
        double absentDay = 0,
        double approvedDedLateInEarlyOutMins = 0,
        double approvedAbsentDay = 0,
        TimeRegisterStatus status = TimeRegisterStatus.Opened,
        string remarks = null)
    {
        UsingDbContext(context =>
        {
            context.Set<TimeRegister>().Add(new TimeRegister
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                DayDate = dayDate,
                LeaveDay = leaveDay,
                LeaveTypeId = leaveTypeId,
                DedLateInEarlyOutMins = dedLateInEarlyOutMins,
                AbsentDay = absentDay,
                ApprovedDedLateInEarlyOutMins = approvedDedLateInEarlyOutMins,
                ApprovedAbsentDay = approvedAbsentDay,
                Status = status,
                Remarks = remarks
            });
        });
    }

    /// <summary>Creates a PayrollArea and an org assignment covering TODAY -- what legacy filters on.</summary>
    private void CreatePayrollAreaAssignment(Employee employee, string payrollAreaCode)
    {
        UsingDbContext(context =>
        {
            var currency = context.Set<Currency>().Add(new Currency
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 3),
                NameEn = "Cur",
                NameAr = "Cur"
            }).Entity;

            var payrollPeriod = context.Set<PayrollPeriod>().Add(new PayrollPeriod
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "P",
                NameAr = "P"
            }).Entity;

            context.SaveChanges();

            var payrollArea = context.Set<PayrollArea>().Add(new PayrollArea
            {
                TenantId = TenantId,
                Code = payrollAreaCode,
                Name = "PA-" + payrollAreaCode,
                CurrencyId = currency.Id,
                PayrollPeriodId = payrollPeriod.Id
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = Clock.Now.Date.AddYears(-1),
                EndDate = Clock.Now.Date.AddYears(1),
                PayrollAreaId = payrollArea.Id
            });
        });
    }

    private static GetTimeRegisterApprovalReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string payrollAreaCode = null,
        int maxResultCount = 100)
    {
        return new GetTimeRegisterApprovalReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            PayrollAreaCode = payrollAreaCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Include_The_Days_On_Both_Window_Boundaries_And_Exclude_The_Neighbours()
    {
        var employee = CreateEmployee("TRA-EDGE");

        // Legacy predicate is DayDate >= dtFrom && DayDate <= dtTo (lines 415-416) -- both ends inclusive.
        CreateTimeRegister(employee, new DateTime(2024, 5, 31));
        CreateTimeRegister(employee, new DateTime(2024, 6, 1));
        CreateTimeRegister(employee, new DateTime(2024, 6, 30));
        CreateTimeRegister(employee, new DateTime(2024, 7, 1));

        var result = await _appService.GetTimeRegisterApprovalReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TRA-EDGE"));

        var days = result.Items.Select(i => i.DayDate).OrderBy(d => d).ToList();
        days.ShouldBe(new List<DateTime> { new(2024, 6, 1), new(2024, 6, 30) });
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var first = CreateEmployee("TRA-CODE-A");
        var second = CreateEmployee("TRA-CODE-B");
        var third = CreateEmployee("TRA-CODE-C");

        var day = new DateTime(2024, 6, 10);
        CreateTimeRegister(first, day);
        CreateTimeRegister(second, day);
        CreateTimeRegister(third, day);

        var result = await _appService.GetTimeRegisterApprovalReport(
            Input(day, day, employeeCode: "TRA-CODE-A , TRA-CODE-C"));

        var codes = result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "TRA-CODE-A", "TRA-CODE-C" });
    }

    [Fact]
    public async Task Should_Filter_By_Payroll_Area_Using_Todays_Assignment()
    {
        var matching = CreateEmployee("TRA-PA-MATCH");
        var other = CreateEmployee("TRA-PA-OTHER");

        var day = new DateTime(2024, 6, 10);
        CreateTimeRegister(matching, day);
        CreateTimeRegister(other, day);

        // Legacy line 422 keys the payroll-area lookup on DateTime.Now, NOT on the report window.
        var payrollAreaCode = "PA-" + Guid.NewGuid().ToString("N").Substring(0, 6);
        CreatePayrollAreaAssignment(matching, payrollAreaCode);
        CreatePayrollAreaAssignment(other, payrollAreaCode + "-X");

        var result = await _appService.GetTimeRegisterApprovalReport(
            Input(day, day, payrollAreaCode: payrollAreaCode));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("TRA-PA-MATCH");
        codes.ShouldNotContain("TRA-PA-OTHER");
    }

    [Fact]
    public async Task Should_Project_The_Raw_And_Approved_Values_Side_By_Side()
    {
        var employee = CreateEmployee("TRA-PROJ");
        var absenceType = CreateAbsenceType("TRA-LT");

        var day = new DateTime(2024, 6, 12); // a Wednesday
        CreateTimeRegister(
            employee,
            day,
            leaveDay: 1,
            leaveTypeId: absenceType.Id,
            dedLateInEarlyOutMins: 45,
            absentDay: 1,
            approvedDedLateInEarlyOutMins: 30,
            approvedAbsentDay: 0.5,
            status: TimeRegisterStatus.Approved,
            remarks: "Reviewed");

        var result = await _appService.GetTimeRegisterApprovalReport(Input(day, day, employeeCode: "TRA-PROJ"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("TRA-PROJ");
        row.DayDate.ShouldBe(day);
        // Legacy line 438 wrote DayOfWeek.ToString(), i.e. the invariant English name.
        row.WeekDay.ShouldBe("Wednesday");
        row.LeaveDay.ShouldBe(1);
        // Legacy line 440 used LeaveType.NameEn -- English even under an Arabic UI culture.
        row.LeaveType.ShouldBe("TRA-LT-En");
        row.DedLateInEarlyOutMinutes.ShouldBe(45);
        row.AbsentDay.ShouldBe(1);
        row.ApprovedDedLateInEarlyOutMinutes.ShouldBe(30);
        row.ApprovedAbsentDay.ShouldBe(0.5);
        row.Status.ShouldBe(TimeRegisterStatus.Approved);
        row.Remarks.ShouldBe("Reviewed");
    }

    /// <summary>
    /// Legacy line 440 is <c>r.LeaveType != null ? r.LeaveType.NameEn : ""</c> -- an EMPTY STRING, not
    /// null, when the day carries no leave type.
    /// </summary>
    [Fact]
    public async Task Should_Return_Empty_String_Not_Null_When_The_Day_Has_No_Leave_Type()
    {
        var employee = CreateEmployee("TRA-NOLT");

        var day = new DateTime(2024, 6, 13);
        CreateTimeRegister(employee, day);

        var result = await _appService.GetTimeRegisterApprovalReport(Input(day, day, employeeCode: "TRA-NOLT"));

        result.Items.Single().LeaveType.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task Should_Page_Results()
    {
        var employee = CreateEmployee("TRA-PAGE");

        CreateTimeRegister(employee, new DateTime(2024, 6, 1));
        CreateTimeRegister(employee, new DateTime(2024, 6, 2));

        var page = await _appService.GetTimeRegisterApprovalReport(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TRA-PAGE", maxResultCount: 1));

        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(1);
        // Default sort is EmployeeCode ASC, DayDate ASC.
        page.Items.Single().DayDate.ShouldBe(new DateTime(2024, 6, 1));
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("TRA-EXP");

        CreateTimeRegister(employee, new DateTime(2024, 6, 1));
        CreateTimeRegister(employee, new DateTime(2024, 6, 2));
        CreateTimeRegister(employee, new DateTime(2024, 6, 3));
        // Outside the window -- must not reach the export.
        CreateTimeRegister(employee, new DateTime(2024, 7, 1));

        // Page size of 1 -- the export must ignore it and still return every filtered row.
        var file = await _appService.GetTimeRegisterApprovalReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TRA-EXP", maxResultCount: 1));

        file.FileName.ShouldBe("TimeRegisterApproval.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users, invisible to the compiler.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        var employee = CreateEmployee("TRA-HDR");
        CreateTimeRegister(employee, new DateTime(2024, 6, 5));

        var file = await _appService.GetTimeRegisterApprovalReportToExcel(
            Input(new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), employeeCode: "TRA-HDR"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
