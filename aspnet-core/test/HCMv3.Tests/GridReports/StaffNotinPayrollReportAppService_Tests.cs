using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.PayrollReport;
using HCMv3.GridReports.PayrollReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class StaffNotinPayrollReportAppService_Tests : AppTestBase
{
    private readonly IStaffNotinPayrollReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public StaffNotinPayrollReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IStaffNotinPayrollReportAppService>();
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

    private void CreateOnHold(Employee employee, DateTime start, DateTime end, bool onHold)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOnHold>().Add(new EmployeeOnHold
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                OnHold = onHold
            });
        });
    }

    private static GetStaffNotinPayrollReportInput Input(int month, int year, int maxResultCount = 100, int skipCount = 0)
    {
        return new GetStaffNotinPayrollReportInput { Month = month, Year = year, MaxResultCount = maxResultCount, SkipCount = skipCount };
    }

    [Fact]
    public async Task Should_Only_Include_OnHold_Records_Overlapping_The_Reported_Month()
    {
        var inRange = CreateEmployee("SNP-IN");
        var notOnHold = CreateEmployee("SNP-NOTHOLD");
        var outOfRange = CreateEmployee("SNP-OUT");

        CreateOnHold(inRange, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), true);
        CreateOnHold(notOnHold, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), false);
        CreateOnHold(outOfRange, new DateTime(2024, 1, 1), new DateTime(2024, 1, 31), true);

        var result = await _appService.GetStaffNotinPayrollReport(Input(6, 2024));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("SNP-IN");
        codes.ShouldNotContain("SNP-NOTHOLD");
        codes.ShouldNotContain("SNP-OUT");
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_And_Resolve_Every_Header()
    {
        var a = CreateEmployee("SNP-EXP-A");
        var b = CreateEmployee("SNP-EXP-B");
        CreateOnHold(a, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), true);
        CreateOnHold(b, new DateTime(2024, 6, 1), new DateTime(2024, 6, 30), true);

        var file = await _appService.GetStaffNotinPayrollReportToExcel(Input(6, 2024, maxResultCount: 1));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<System.Collections.Generic.IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
        exported.First().Keys.Where(k => k.StartsWith("[") && k.EndsWith("]")).ShouldBeEmpty();
    }
}
