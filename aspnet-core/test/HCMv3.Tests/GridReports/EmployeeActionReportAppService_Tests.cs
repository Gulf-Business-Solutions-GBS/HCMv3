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
/// Tests for the Employee Action grid report (legacy <c>uscRptEmployeeAction.ascx</c>, <c>Table1()</c>).
///
/// This report is ATYPICAL twice over and both are pinned here: it has no employee-status filter, and
/// its date window applies to <c>StartDate</c> alone because the legacy <c>EmployeeAction.EndDate</c>
/// getter just returns <c>StartDate</c> (so HCMv3 stores no EndDate column).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeActionReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeActionReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeActionReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeActionReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, string countryName = null)
    {
        Nationality nationality = null;
        Country country = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;

            if (countryName != null)
            {
                country = context.Set<Country>().Add(new Country
                {
                    TenantId = TenantId,
                    Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                    NameEn = countryName,
                    NameAr = countryName + "-Ar"
                }).Entity;
            }
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
                CountryId = country?.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private (PersonalAction Action, PersonalActionReason Reason) CreatePersonalAction(string code)
    {
        PersonalAction action = null;
        PersonalActionReason reason = null;

        UsingDbContext(context =>
        {
            action = context.Set<PersonalAction>().Add(new PersonalAction
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar"
            }).Entity;

            context.SaveChanges();

            reason = context.Set<PersonalActionReason>().Add(new PersonalActionReason
            {
                TenantId = TenantId,
                Code = code + "-R",
                NameEn = code + "-Reason",
                NameAr = code + "-ReasonAr",
                PersonalActionId = action.Id
            }).Entity;
        });

        return (action, reason);
    }

    private void CreateAction(Employee employee, PersonalAction action, PersonalActionReason reason, DateTime start)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAction>().Add(new EmployeeAction
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                PersonalActionId = action.Id,
                ActionReasonId = reason.Id,
                StartDate = start
            });
        });
    }

    private static GetEmployeeActionReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        string personalActionCode = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeActionReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            PersonalActionCode = personalActionCode,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Actions_Inside_The_Requested_Window()
    {
        var (action, reason) = CreatePersonalAction("PA-OVL-" + Guid.NewGuid().ToString("N").Substring(0, 4));
        var inRange = CreateEmployee("NOVL-IN");
        var before = CreateEmployee("NOVL-BEFORE");
        var after = CreateEmployee("NOVL-AFTER");

        CreateAction(inRange, action, reason, new DateTime(2024, 6, 1));
        CreateAction(before, action, reason, new DateTime(2023, 6, 1));
        CreateAction(after, action, reason, new DateTime(2025, 6, 1));

        var result = await _appService.GetEmployeeActionReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("NOVL-IN");
        codes.ShouldNotContain("NOVL-BEFORE");
        codes.ShouldNotContain("NOVL-AFTER");
    }

    [Fact]
    public async Task Should_Include_Actions_On_The_Window_Boundary()
    {
        var (action, reason) = CreatePersonalAction("PA-EDGE-" + Guid.NewGuid().ToString("N").Substring(0, 4));
        var onFrom = CreateEmployee("NEDGE-FROM");
        var onTo = CreateEmployee("NEDGE-TO");

        CreateAction(onFrom, action, reason, new DateTime(2024, 1, 1));
        CreateAction(onTo, action, reason, new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeActionReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("NEDGE-FROM");
        codes.ShouldContain("NEDGE-TO");
    }

    [Fact]
    public async Task Should_Filter_By_Personal_Action_Code()
    {
        var suffix = Guid.NewGuid().ToString("N").Substring(0, 4);
        var (wanted, wantedReason) = CreatePersonalAction("PA-W-" + suffix);
        var (other, otherReason) = CreatePersonalAction("PA-O-" + suffix);

        var wantedEmployee = CreateEmployee("NPA-WANTED");
        var otherEmployee = CreateEmployee("NPA-OTHER");

        CreateAction(wantedEmployee, wanted, wantedReason, new DateTime(2024, 5, 1));
        CreateAction(otherEmployee, other, otherReason, new DateTime(2024, 5, 1));

        var result = await _appService.GetEmployeeActionReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), personalActionCode: wanted.Code));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("NPA-WANTED");
        codes.ShouldNotContain("NPA-OTHER");
    }

    [Fact]
    public async Task Should_Project_The_Legacy_Columns_With_EndDate_Mirroring_StartDate()
    {
        var (action, reason) = CreatePersonalAction("PA-PROJ-" + Guid.NewGuid().ToString("N").Substring(0, 4));
        var employee = CreateEmployee("NPROJ-1", "Kuwait");

        CreateAction(employee, action, reason, new DateTime(2024, 4, 15));

        var result = await _appService.GetEmployeeActionReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "NPROJ-1"));

        var row = result.Items.Single();
        row.EmployeeName.ShouldBe("Full NPROJ-1");
        row.PersonalActionName.ShouldBe(action.Code + "-En");
        row.ActionReasonName.ShouldBe(action.Code + "-Reason");
        // Legacy "Country" on THIS report is the employee's own country.
        row.CountryName.ShouldBe("Kuwait");
        row.StartDate.ShouldBe(new DateTime(2024, 4, 15));
        // Legacy EmployeeAction.EndDate is a getter returning StartDate.
        row.EndDate.ShouldBe(row.StartDate);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var (action, reason) = CreatePersonalAction("PA-EXP-" + Guid.NewGuid().ToString("N").Substring(0, 4));
        var first = CreateEmployee("NEXP-A");
        var second = CreateEmployee("NEXP-B");
        var third = CreateEmployee("NEXP-C");
        var excluded = CreateEmployee("NEXP-OUT");

        CreateAction(first, action, reason, new DateTime(2024, 4, 1));
        CreateAction(second, action, reason, new DateTime(2024, 4, 1));
        CreateAction(third, action, reason, new DateTime(2024, 4, 1));
        CreateAction(excluded, action, reason, new DateTime(2024, 4, 1));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            employeeCode: "NEXP-A,NEXP-B,NEXP-C",
            maxResultCount: 1);

        var file = await _appService.GetEmployeeActionReportToExcel(input);

        file.FileName.ShouldBe("EmployeeAction.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "NEXP-A", "NEXP-B", "NEXP-C" });
        exported.First().Keys.ShouldContain("Personal Action");
    }
}
