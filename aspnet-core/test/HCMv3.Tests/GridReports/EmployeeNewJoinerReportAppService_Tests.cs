using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Status;
using HCMv3.GridReports.Status.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee New Joiner grid report (legacy <c>uscRptEmployeeNewJoiner.ascx</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeNewJoinerReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeNewJoinerReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeNewJoinerReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeNewJoinerReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, DateTime joiningDate, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
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
                JoiningDate = joiningDate,
                NationalityId = nationality.Id,
                Status = status
            }).Entity;
        });
        return employee;
    }

    private static GetEmployeeNewJoinerReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100,
        int skipCount = 0)
    {
        return new GetEmployeeNewJoinerReportInput
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
    public async Task Should_Filter_By_JoiningDate_Window()
    {
        var inRange = CreateEmployee("NJ-IN", new DateTime(2024, 6, 1));
        var outOfRange = CreateEmployee("NJ-OUT", new DateTime(2023, 1, 1));

        var result = await _appService.GetEmployeeNewJoinerReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("NJ-IN");
        codes.ShouldNotContain("NJ-OUT");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var active = CreateEmployee("NJ-ACTIVE", new DateTime(2024, 6, 1));
        var inactive = CreateEmployee("NJ-INACTIVE", new DateTime(2024, 6, 1), EmployeeStatus.Inactive);

        var result = await _appService.GetEmployeeNewJoinerReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("NJ-INACTIVE");
        codes.ShouldNotContain("NJ-ACTIVE");
    }

    /// <summary>
    /// Every exported header must be a RESOLVED caption. A localization key with no XML entry renders as
    /// "[SomeKey]" in the xlsx -- visible to users but invisible to the compiler and to every other test.
    /// </summary>
    [Fact]
    public async Task Should_Resolve_Every_Exported_Column_Header()
    {
        CreateEmployee("NJ-HDR", new DateTime(2024, 6, 1));

        var file = await _appService.GetEmployeeNewJoinerReportToExcel(
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
