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
using HCMv3.Workflow;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Workflow Role grid-report port (legacy <c>uscRptEmployeeWorkflowRole.ascx</c>,
/// <c>Table1()</c> lines 494-570). Modelled on the reference report's tests: each test pins one legacy
/// filter/projection semantic, and the boundary-date and "export ignores page size" tests are kept.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeWorkflowRoleReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeWorkflowRoleReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeWorkflowRoleReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeWorkflowRoleReportAppService>();
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

    private WorkflowRole CreateWorkflowRole(string code)
    {
        WorkflowRole role = null;
        UsingDbContext(context =>
        {
            role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;
        });

        return role;
    }

    private void CreateEmployeeWorkflowRole(Employee employee, WorkflowRole role, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                WorkflowRoleId = role.Id,
                StartDate = start,
                EndDate = end
            });
        });
    }

    private static GetEmployeeWorkflowRoleReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeWorkflowRoleReportInput
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
        var role = CreateWorkflowRole("WR-OVL");
        var inRange = CreateEmployee("WFR-IN");
        var before = CreateEmployee("WFR-BEFORE");
        var after = CreateEmployee("WFR-AFTER");

        CreateEmployeeWorkflowRole(inRange, role, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateEmployeeWorkflowRole(before, role, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));
        CreateEmployeeWorkflowRole(after, role, new DateTime(2025, 6, 1), new DateTime(2025, 12, 31));

        var result = await _appService.GetEmployeeWorkflowRoleReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("WFR-IN");
        codes.ShouldNotContain("WFR-BEFORE");
        codes.ShouldNotContain("WFR-AFTER");
    }

    [Fact]
    public async Task Should_Include_Rows_Touching_The_Window_Boundary()
    {
        var role = CreateWorkflowRole("WR-EDGE");
        var startsOnToDate = CreateEmployee("WFR-EDGE-START");
        var endsOnFromDate = CreateEmployee("WFR-EDGE-END");

        // Legacy predicate is NOT(StartDate > To || EndDate < From), so both boundary rows are included.
        CreateEmployeeWorkflowRole(startsOnToDate, role, new DateTime(2024, 12, 31), new DateTime(2025, 12, 31));
        CreateEmployeeWorkflowRole(endsOnFromDate, role, new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        var result = await _appService.GetEmployeeWorkflowRoleReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("WFR-EDGE-START");
        codes.ShouldContain("WFR-EDGE-END");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var role = CreateWorkflowRole("WR-ST");
        var active = CreateEmployee("WFR-ST-ACTIVE");
        var inactive = CreateEmployee("WFR-ST-INACTIVE", EmployeeStatus.Inactive);

        CreateEmployeeWorkflowRole(active, role, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeWorkflowRole(inactive, role, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeWorkflowRoleReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("WFR-ST-INACTIVE");
        codes.ShouldNotContain("WFR-ST-ACTIVE");
    }

    [Fact]
    public async Task Should_Project_Legacy_Columns()
    {
        var role = CreateWorkflowRole("WR-PROJ");
        var employee = CreateEmployee("WFR-PROJ");

        CreateEmployeeWorkflowRole(employee, role, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeWorkflowRoleReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "WFR-PROJ"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("WFR-PROJ");
        row.EmployeeName.ShouldBe("Full WFR-PROJ");
        row.WorkflowRoleName.ShouldBe("WR-PROJ-En");
        row.StartDate.ShouldBe(new DateTime(2024, 1, 1));
        row.EndDate.ShouldBe(new DateTime(2024, 12, 31));
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var role = CreateWorkflowRole("WR-EXP");
        var first = CreateEmployee("WFR-EXP-A");
        var second = CreateEmployee("WFR-EXP-B");
        var third = CreateEmployee("WFR-EXP-C");
        var excluded = CreateEmployee("WFR-EXP-OUT");

        CreateEmployeeWorkflowRole(first, role, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeWorkflowRole(second, role, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeWorkflowRole(third, role, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeWorkflowRole(excluded, role, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            employeeCode: "WFR-EXP-A,WFR-EXP-B,WFR-EXP-C",
            maxResultCount: 1);

        var file = await _appService.GetEmployeeWorkflowRoleReportToExcel(input);

        file.FileName.ShouldBe("EmployeeWorkflowRole.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "WFR-EXP-A", "WFR-EXP-B", "WFR-EXP-C" });
        // The exporter writes LOCALIZED header cells, not the raw localization key.
        exported.First().Keys.ShouldContain("Workflow Role");
    }
}
