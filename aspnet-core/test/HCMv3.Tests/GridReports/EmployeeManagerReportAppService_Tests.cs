using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Manager grid-report port (legacy <c>uscRptEmployeeManager.ascx</c>,
/// <c>Table1()</c>). Each test pins one legacy filter/projection semantic.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeManagerReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeManagerReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeManagerReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeManagerReportAppService>();
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

    private void CreateManagerLink(Employee employee, Employee manager, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                ManagerId = manager.Id,
                StartDate = start,
                EndDate = end
            });
        });
    }

    private static GetEmployeeManagerReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeManagerReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Rows_Overlapping_The_Requested_Window()
    {
        var manager = CreateEmployee("MGR-BOSS");
        var inRange = CreateEmployee("MGR-IN");
        var before = CreateEmployee("MGR-BEFORE");
        var after = CreateEmployee("MGR-AFTER");

        CreateManagerLink(inRange, manager, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateManagerLink(before, manager, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));
        CreateManagerLink(after, manager, new DateTime(2025, 6, 1), new DateTime(2025, 12, 31));

        var result = await _appService.GetEmployeeManagerReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("MGR-IN");
        codes.ShouldNotContain("MGR-BEFORE");
        codes.ShouldNotContain("MGR-AFTER");
    }

    [Fact]
    public async Task Should_Include_Rows_Touching_The_Window_Boundary()
    {
        var manager = CreateEmployee("MGREDGE-BOSS");
        var startsOnToDate = CreateEmployee("MGREDGE-START");
        var endsOnFromDate = CreateEmployee("MGREDGE-END");

        // Legacy predicate is NOT(StartDate > To || EndDate < From), so both boundary rows are included.
        CreateManagerLink(startsOnToDate, manager, new DateTime(2024, 12, 31), new DateTime(2025, 12, 31));
        CreateManagerLink(endsOnFromDate, manager, new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        var result = await _appService.GetEmployeeManagerReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("MGREDGE-START");
        codes.ShouldContain("MGREDGE-END");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var manager = CreateEmployee("MGRST-BOSS");
        var active = CreateEmployee("MGRST-ACTIVE");
        var inactive = CreateEmployee("MGRST-INACTIVE", EmployeeStatus.Inactive);

        CreateManagerLink(active, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateManagerLink(inactive, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeManagerReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("MGRST-INACTIVE");
        codes.ShouldNotContain("MGRST-ACTIVE");
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var manager = CreateEmployee("MGRCODE-BOSS");
        var first = CreateEmployee("MGRCODE-A");
        var second = CreateEmployee("MGRCODE-B");
        var third = CreateEmployee("MGRCODE-C");

        CreateManagerLink(first, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateManagerLink(second, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateManagerLink(third, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeManagerReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
                employeeCode: "MGRCODE-A , MGRCODE-C"));

        var codes = result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "MGRCODE-A", "MGRCODE-C" });
    }

    [Fact]
    public async Task Should_Project_Manager_As_Code_Space_FullName()
    {
        var manager = CreateEmployee("MGRPROJ-BOSS");
        var employee = CreateEmployee("MGRPROJ-1");

        CreateManagerLink(employee, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeManagerReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "MGRPROJ-1"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("MGRPROJ-1");
        row.EmployeeName.ShouldBe("Full MGRPROJ-1");
        // Legacy CodeWithFullNameEn is Concat(Code, ' ', FullNameEn).
        row.ManagerName.ShouldBe("MGRPROJ-BOSS Full MGRPROJ-BOSS");
        row.StartDate.ShouldBe(new DateTime(2024, 1, 1));
        row.EndDate.ShouldBe(new DateTime(2024, 12, 31));
    }

    [Fact]
    public async Task Should_Page_Results()
    {
        var manager = CreateEmployee("MGRPAGE-BOSS");
        var first = CreateEmployee("MGRPAGE-A");
        var second = CreateEmployee("MGRPAGE-B");

        CreateManagerLink(first, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateManagerLink(second, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var page = await _appService.GetEmployeeManagerReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
                employeeCode: "MGRPAGE-A,MGRPAGE-B", maxResultCount: 1));

        page.TotalCount.ShouldBe(2);
        page.Items.Count.ShouldBe(1);
        page.Items.Single().EmployeeCode.ShouldBe("MGRPAGE-A"); // default sort is EmployeeCode ASC
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var manager = CreateEmployee("MGREXP-BOSS");
        var first = CreateEmployee("MGREXP-A");
        var second = CreateEmployee("MGREXP-B");
        var third = CreateEmployee("MGREXP-C");
        var excluded = CreateEmployee("MGREXP-OUT");

        CreateManagerLink(first, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateManagerLink(second, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateManagerLink(third, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateManagerLink(excluded, manager, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31),
            employeeCode: "MGREXP-A,MGREXP-B,MGREXP-C", maxResultCount: 1);

        var file = await _appService.GetEmployeeManagerReportToExcel(input);

        file.FileName.ShouldBe("EmployeeManager.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "MGREXP-A", "MGREXP-B", "MGREXP-C" });
    }
}
