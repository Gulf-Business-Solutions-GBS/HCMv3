using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class PrePayrollReportAppService_Tests : AppTestBase
{
    private readonly IPrePayrollReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public PrePayrollReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IPrePayrollReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, string accessId = null)
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
                Status = EmployeeStatus.Active,
                AccessID = accessId
            }).Entity;
        });

        return employee;
    }

    private static GetPrePayrollReportInput Input(int month, int year, int maxResultCount = 100, int skipCount = 0)
    {
        return new GetPrePayrollReportInput { Month = month, Year = year, MaxResultCount = maxResultCount, SkipCount = skipCount };
    }

    [Fact]
    public async Task Should_Report_HasAccessId_True_When_AccessID_Is_Not_Blank()
    {
        CreateEmployee("PPR-ACC-YES", "SOME-ID");
        CreateEmployee("PPR-ACC-NO");

        var result = await _appService.GetPrePayrollReport(Input(6, 2024));

        result.Items.Single(r => r.EmployeeCode == "PPR-ACC-YES").HasAccessId.ShouldBeTrue();
        result.Items.Single(r => r.EmployeeCode == "PPR-ACC-NO").HasAccessId.ShouldBeFalse();
    }

    /// <summary>
    /// LEGACY QUIRK REPRODUCED: RunType/PayrollArea are logged but never applied as filters -- selecting
    /// either must not change the result set.
    /// </summary>
    [Fact]
    public async Task Should_Not_Filter_By_PayrollRunType_Or_PayrollArea_Reproducing_Legacy_Behavior()
    {
        CreateEmployee("PPR-NOFILTER");

        var withoutFilters = await _appService.GetPrePayrollReport(Input(6, 2024));
        var withRunTypeFilter = await _appService.GetPrePayrollReport(new GetPrePayrollReportInput
        {
            Month = 6,
            Year = 2024,
            PayrollRunType = PayrollRunType.OffCycle,
            PayrollAreaId = 999999
        });

        withoutFilters.Items.Select(i => i.EmployeeCode).ShouldContain("PPR-NOFILTER");
        withRunTypeFilter.Items.Select(i => i.EmployeeCode).ShouldContain("PPR-NOFILTER");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_And_Resolve_Every_Header()
    {
        CreateEmployee("PPR-EXP-A");
        CreateEmployee("PPR-EXP-B");

        var file = await _appService.GetPrePayrollReportToExcel(Input(6, 2024, maxResultCount: 1));

        file.FileName.ShouldBe("PrePayrollReport.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<System.Collections.Generic.IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
        exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ShouldBeEmpty();
    }
}
