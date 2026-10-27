using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PerformanceReport;
using HCMv3.GridReports.PerformanceReport.Dto;
using HCMv3.PerformanceManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the PA Employee Scorecard grid report (legacy customer variant
/// <c>uscRptPAEmployee_Custom033.ascx</c>, <c>Table1()</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class PAEmployeeScorecardReportAppService_Tests : AppTestBase
{
    private readonly IPAEmployeeScorecardReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PAEmployeeScorecardReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPAEmployeeScorecardReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
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
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private PAEmployee CreatePAEmployee(Employee employee, string code)
    {
        FormType formType = null;
        PACycle cycle = null;
        UsingDbContext(context =>
        {
            formType = context.Set<FormType>().Add(new FormType
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-Form",
                NameAr = code + "-Form-Ar"
            }).Entity;

            cycle = context.Set<PACycle>().Add(new PACycle
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-Cycle",
                NameAr = code + "-Cycle-Ar"
            }).Entity;
        });

        PAEmployee paEmployee = null;
        UsingDbContext(context =>
        {
            paEmployee = context.Set<PAEmployee>().Add(new PAEmployee
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                FormTypeId = formType.Id,
                PACycleId = cycle.Id
            }).Entity;
        });

        return paEmployee;
    }

    private static GetPAEmployeeScorecardReportInput Input(string employeeCode = null)
    {
        return new GetPAEmployeeScorecardReportInput
        {
            EmployeeCode = employeeCode,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Code()
    {
        var included = CreateEmployee("SC-INC");
        var excluded = CreateEmployee("SC-EXC");
        CreatePAEmployee(included, "SC-INC");
        CreatePAEmployee(excluded, "SC-EXC");

        var result = await _appService.GetPAEmployeeScorecardReport(Input(employeeCode: "SC-INC"));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("SC-INC");
        codes.ShouldNotContain("SC-EXC");
    }

    [Fact]
    public async Task Should_Default_Percentages_To_Zero_When_No_Ratings_Exist()
    {
        var employee = CreateEmployee("SC-NORATE");
        CreatePAEmployee(employee, "SC-NORATE");

        var result = await _appService.GetPAEmployeeScorecardReport(Input(employeeCode: "SC-NORATE"));

        var row = result.Items.Single();
        row.BehaviorPercentage.ShouldBe(0);
        row.TechnicalPercentage.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_With_OrgLevel_Headers()
    {
        var employee = CreateEmployee("SC-EXP");
        CreatePAEmployee(employee, "SC-EXP");

        var file = await _appService.GetPAEmployeeScorecardReportToExcel(Input(employeeCode: "SC-EXP"));

        file.FileName.ShouldBe("PAEmployeeScorecard.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<System.Collections.Generic.IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(1);
        for (var level = 1; level <= 7; level++)
        {
            exported.First().Keys.ShouldContain("Org Level " + level);
        }
    }
}
