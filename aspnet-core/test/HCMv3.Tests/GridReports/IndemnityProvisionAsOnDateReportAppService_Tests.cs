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

// ReSharper disable once InconsistentNaming
public class IndemnityProvisionAsOnDateReportAppService_Tests : AppTestBase
{
    private readonly IIndemnityProvisionAsOnDateReportAppService _appService;

    public IndemnityProvisionAsOnDateReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IIndemnityProvisionAsOnDateReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, DateTime joiningDate)
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
                ProbationEndDate = joiningDate.AddMonths(3),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    [Fact]
    public async Task Should_Flag_Comment_When_As_On_Date_Predates_Joining()
    {
        var joiningDate = new DateTime(2024, 1, 1);
        CreateEmployee("IPAOD-1", joiningDate);

        var result = await _appService.GetIndemnityProvisionAsOnDateReport(new GetIndemnityProvisionAsOnDateReportInput
        {
            FromDate = new DateTime(2023, 6, 1),
            EmployeeCode = "IPAOD-1",
            MaxResultCount = 100
        });

        var row = result.Items.Single();
        row.Comment.ShouldNotBeNullOrEmpty();
        row.IndemnityQuota.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Leave_Indemnity_Quota_Zero_When_No_Gratuity_Quota_Row_Covers_As_On_Date()
    {
        var joiningDate = new DateTime(2018, 1, 1);
        CreateEmployee("IPAOD-2", joiningDate);

        var result = await _appService.GetIndemnityProvisionAsOnDateReport(new GetIndemnityProvisionAsOnDateReportInput
        {
            FromDate = new DateTime(2024, 6, 1),
            EmployeeCode = "IPAOD-2",
            MaxResultCount = 100
        });

        var row = result.Items.Single();
        row.IndemnityQuota.ShouldBe(0);
        row.DeductionStart.ShouldBeNull();
    }
}
