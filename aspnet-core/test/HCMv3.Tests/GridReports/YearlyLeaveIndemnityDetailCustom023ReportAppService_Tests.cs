using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.QuotaProvision;
using HCMv3.GridReports.QuotaProvision.Dto;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Custom023 shares its query logic verbatim with Custom016 (see
/// <see cref="HCMv3.GridReports.QuotaProvision.YearlyLeaveIndemnityDetailReportQueryHelper"/>); this test
/// only pins that the Custom023 app service is wired up under its own permission/screen name.
/// </summary>
// ReSharper disable once InconsistentNaming
public class YearlyLeaveIndemnityDetailCustom023ReportAppService_Tests : AppTestBase
{
    private readonly IYearlyLeaveIndemnityDetailCustom023ReportAppService _appService;

    public YearlyLeaveIndemnityDetailCustom023ReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IYearlyLeaveIndemnityDetailCustom023ReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
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
                JoiningDate = new DateTime(2018, 1, 1),
                ProbationEndDate = new DateTime(2018, 4, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    [Fact]
    public async Task Should_Return_A_Row_Per_Requested_Employee_With_Zeroed_Balances_When_No_Leave_Or_Payroll_Data_Exists()
    {
        CreateEmployee("YLID23-1");

        var result = await _appService.GetYearlyLeaveIndemnityDetailCustom023Report(new GetYearlyLeaveIndemnityDetailReportInput
        {
            ToDate = new DateTime(2024, 12, 31),
            EmployeeCode = "YLID23-1",
            MaxResultCount = 100
        });

        var row = result.Items.Single();
        row.OpeningBalance.ShouldBe(0);
        row.ClosingBalance.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Export_Without_Throwing()
    {
        CreateEmployee("YLID23-2");

        var file = await _appService.GetYearlyLeaveIndemnityDetailCustom023ReportToExcel(new GetYearlyLeaveIndemnityDetailReportInput
        {
            ToDate = new DateTime(2024, 12, 31),
            EmployeeCode = "YLID23-2",
            MaxResultCount = 100
        });

        file.FileName.ShouldBe("YearlyLeaveIndemnityDetailCustom023.xlsx");
    }
}
