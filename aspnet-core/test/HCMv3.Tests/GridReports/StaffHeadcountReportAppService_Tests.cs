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
/// Tests for the Staff Headcount grid-report port (legacy <c>uscRptStaffHeadcount.ascx</c>,
/// <c>Table1()</c> lines 474-524 and <c>Table2()</c> lines 526-579).
///
/// This report is a DRILL-DOWN, so these tests cover both ported levels and specifically pin that the
/// level-2 drill re-applies the level-1 filters (legacy repeats the filter block verbatim in every
/// TableN). The "export ignores page size" test is kept, on both levels.
/// </summary>
// ReSharper disable once InconsistentNaming
public class StaffHeadcountReportAppService_Tests : AppTestBase
{
    private readonly IStaffHeadcountReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public StaffHeadcountReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IStaffHeadcountReportAppService>();
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

    private long CreateOrganizationUnit(string displayName)
    {
        long id = 0;
        UsingDbContext(context =>
        {
            var unit = context.OrganizationUnits.Add(
                new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null)
                {
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8)
                }).Entity;
            context.SaveChanges();
            id = unit.Id;
        });

        return id;
    }

    /// <summary>Legacy's "CurrentDepartment" is the org unit of the assignment covering TODAY.</summary>
    private void AssignToday(Employee employee, long organizationUnitId)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = DateTime.Now.Date.AddDays(-1),
                EndDate = DateTime.Now.Date.AddDays(1),
                OrganizationUnitId = organizationUnitId
            });
        });
    }

    private static GetStaffHeadcountReportInput Input(
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetStaffHeadcountReportInput
        {
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    private static GetStaffHeadcountEmployeeReportInput EmployeeInput(
        long organizationUnitId,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetStaffHeadcountEmployeeReportInput
        {
            OrganizationUnitId = organizationUnitId,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Count_Employees_Per_Department()
    {
        var unit = CreateOrganizationUnit("HC Department A");

        AssignToday(CreateEmployee("HC-A1"), unit);
        AssignToday(CreateEmployee("HC-A2"), unit);

        var result = await _appService.GetStaffHeadcountReport(Input(employeeCode: "HC-A1,HC-A2"));

        var row = result.Items.Single(r => r.DepartmentName == "HC Department A");
        row.EmployeeCount.ShouldBe(2);
        row.Id.ShouldBe(unit);
    }

    [Fact]
    public async Task Should_Exclude_Employees_Without_A_Current_Assignment()
    {
        var unit = CreateOrganizationUnit("HC Department B");

        AssignToday(CreateEmployee("HC-B1"), unit);
        // No assignment at all -> legacy's "r.CurrentDepartment != null" drops it from the headcount.
        CreateEmployee("HC-B2");

        var result = await _appService.GetStaffHeadcountReport(Input(employeeCode: "HC-B1,HC-B2"));

        result.Items.Single(r => r.DepartmentName == "HC Department B").EmployeeCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Filter_Headcount_By_Employee_Status()
    {
        var unit = CreateOrganizationUnit("HC Department C");

        AssignToday(CreateEmployee("HC-C1"), unit);
        AssignToday(CreateEmployee("HC-C2", EmployeeStatus.Inactive), unit);

        var result = await _appService.GetStaffHeadcountReport(
            Input(employeeCode: "HC-C1,HC-C2", status: EmployeeStatus.Inactive));

        result.Items.Single(r => r.DepartmentName == "HC Department C").EmployeeCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_List_The_Employees_Of_The_Drilled_Into_Department()
    {
        var unit = CreateOrganizationUnit("HC Department D");
        var otherUnit = CreateOrganizationUnit("HC Department D2");

        AssignToday(CreateEmployee("HC-D1"), unit);
        AssignToday(CreateEmployee("HC-D2"), unit);
        AssignToday(CreateEmployee("HC-D3"), otherUnit);

        var result = await _appService.GetStaffHeadcountEmployeeReport(EmployeeInput(unit));

        var codes = result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "HC-D1", "HC-D2" });
        result.Items.First().EmployeeName.ShouldBe("Full HC-D1");
    }

    [Fact]
    public async Task Should_Reapply_The_Level1_Filters_When_Drilling_Down()
    {
        var unit = CreateOrganizationUnit("HC Department E");

        AssignToday(CreateEmployee("HC-E1"), unit);
        AssignToday(CreateEmployee("HC-E2", EmployeeStatus.Inactive), unit);

        // Legacy repeats the identical filter block in Table2, so a drill-down never widens the set.
        var result = await _appService.GetStaffHeadcountEmployeeReport(
            EmployeeInput(unit, status: EmployeeStatus.Inactive));

        result.Items.Single().EmployeeCode.ShouldBe("HC-E2");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Departments_Not_Just_The_Current_Page()
    {
        var first = CreateOrganizationUnit("HC Export Dept 1");
        var second = CreateOrganizationUnit("HC Export Dept 2");
        var third = CreateOrganizationUnit("HC Export Dept 3");

        AssignToday(CreateEmployee("HC-X1"), first);
        AssignToday(CreateEmployee("HC-X2"), second);
        AssignToday(CreateEmployee("HC-X3"), third);

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var file = await _appService.GetStaffHeadcountReportToExcel(
            Input(employeeCode: "HC-X1,HC-X2,HC-X3", maxResultCount: 1));

        file.FileName.ShouldBe("StaffHeadcount.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var departments = exported.Select(r => r["Department"]?.ToString()).OrderBy(d => d).ToList();
        departments.ShouldBe(new List<string> { "HC Export Dept 1", "HC Export Dept 2", "HC Export Dept 3" });
        exported.First().Keys.ShouldContain("Employees");
    }

    [Fact]
    public async Task Should_Export_All_Employees_Of_The_Drilled_Into_Department()
    {
        var unit = CreateOrganizationUnit("HC Export Dept 4");

        AssignToday(CreateEmployee("HC-Y1"), unit);
        AssignToday(CreateEmployee("HC-Y2"), unit);
        AssignToday(CreateEmployee("HC-Y3"), unit);

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var file = await _appService.GetStaffHeadcountEmployeeReportToExcel(
            EmployeeInput(unit, maxResultCount: 1));

        file.FileName.ShouldBe("StaffHeadcountEmployees.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var codes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "HC-Y1", "HC-Y2", "HC-Y3" });
    }
}
