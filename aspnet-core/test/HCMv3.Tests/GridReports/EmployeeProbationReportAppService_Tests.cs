using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GridReports.Status;
using HCMv3.GridReports.Status.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Probation grid report (legacy <c>uscRptEmployeeProbation.ascx</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeProbationReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeProbationReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeProbationReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeProbationReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(
        string code,
        DateTime probationEndDate,
        EmployeeStatus status = EmployeeStatus.Active)
    {
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
                ProbationDays = 90,
                ProbationEndDate = probationEndDate,
                Status = status
            }).Entity;
        });
        return employee;
    }

    private static GetEmployeeProbationReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeeProbationReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = skipCount
        };
    }

    [Fact]
    public async Task Should_Filter_By_ProbationEndDate_Window()
    {
        var inRange = CreateEmployee("PR-IN", new DateTime(2024, 6, 1));
        var outOfRange = CreateEmployee("PR-OUT", new DateTime(2023, 1, 1));

        var result = await _appService.GetEmployeeProbationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PR-IN");
        codes.ShouldNotContain("PR-OUT");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var active = CreateEmployee("PR-ACTIVE", new DateTime(2024, 6, 1));
        var inactive = CreateEmployee("PR-INACTIVE", new DateTime(2024, 6, 1), EmployeeStatus.Inactive);

        var result = await _appService.GetEmployeeProbationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("PR-INACTIVE");
        codes.ShouldNotContain("PR-ACTIVE");
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users but invisible to the compiler and to every other test.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        CreateEmployee("PR-HDR", new DateTime(2024, 6, 1));

        var file = await _appService.GetEmployeeProbationReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        var unresolved = exported.First().Keys
            .Where(k => k.StartsWith("[") && k.EndsWith("]"))
            .ToList();

        unresolved.ShouldBeEmpty();
    }
}
