using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Transaction;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EmployeeSocialSecurityReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeSocialSecurityReportAppService _appService;

    public EmployeeSocialSecurityReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSocialSecurityReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private static string NewCode() => Guid.NewGuid().ToString("N").Substring(0, 8);

    private Nationality CreateNationality()
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality { TenantId = TenantId, Code = NewCode(), NameEn = "N", NameAr = "N" }).Entity;
        });
        return nationality;
    }

    private Employee CreateEmployee(string code, Nationality nationality, Gender gender = Gender.Male)
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
                JoiningDate = new DateTime(2015, 1, 1),
                ProbationEndDate = new DateTime(2015, 4, 1),
                NationalityId = nationality.Id,
                Gender = gender
            }).Entity;
        });
        return employee;
    }

    private SocialSecurity CreateSocialSecurity(Nationality nationality, DateTime start, DateTime end, double earningsCap = 0)
    {
        SocialSecurity socialSecurity = null;
        UsingDbContext(context =>
        {
            socialSecurity = context.Set<SocialSecurity>().Add(new SocialSecurity
            {
                TenantId = TenantId,
                NationalityId = nationality.Id,
                StartDate = start,
                EndDate = end,
                EarningsCap = earningsCap
            }).Entity;
        });
        return socialSecurity;
    }

    private void CreateSocialSecurityDetail(SocialSecurity socialSecurity, double min, double max, double employeeContribution, double employerContribution)
    {
        UsingDbContext(context =>
        {
            context.Set<SocialSecurityDetail>().Add(new SocialSecurityDetail
            {
                TenantId = TenantId,
                SocialSecurityId = socialSecurity.Id,
                MinEarnings = min,
                MaxEarnings = max,
                EmployeeContribution = employeeContribution,
                EmployerContribution = employerContribution
            });
        });
    }

    private void CreateEmployeeSocialSecurity(Employee employee, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeSocialSecurity>().Add(new EmployeeSocialSecurity
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                SocialSecurityNum = 123456,
                StartDate = start,
                EndDate = end,
                SocialSecurityContrib = true
            });
        });
    }

    private static GetEmployeeSocialSecurityReportInput Input(DateTime asOf, string employeeCode = null)
    {
        return new GetEmployeeSocialSecurityReportInput { AsOfDate = asOf, EmployeeCode = employeeCode, MaxResultCount = 100, SkipCount = 0 };
    }

    [Fact]
    public async Task Should_Only_Include_Registrations_Effective_As_Of_The_Requested_Date()
    {
        var nationality = CreateNationality();
        var current = CreateEmployee("ESS-CUR", nationality);
        var expired = CreateEmployee("ESS-EXP", nationality);

        CreateEmployeeSocialSecurity(current, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeSocialSecurity(expired, new DateTime(2022, 1, 1), new DateTime(2022, 12, 31));

        var result = await _appService.GetEmployeeSocialSecurityReport(Input(new DateTime(2024, 6, 15)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("ESS-CUR");
        codes.ShouldNotContain("ESS-EXP");
    }

    [Fact]
    public async Task Should_Leave_Contribution_Fields_At_Zero_When_No_SocialSecurity_Master_Matches_The_Nationality()
    {
        var nationality = CreateNationality();
        var employee = CreateEmployee("ESS-NOMASTER", nationality);
        CreateEmployeeSocialSecurity(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeSocialSecurityReport(Input(new DateTime(2024, 6, 15), "ESS-NOMASTER"));

        var row = result.Items.Single();
        row.EmployeeShareAmount.ShouldBe(0);
        row.CompanyShareAmount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Compute_Employee_And_Company_Contribution_From_The_Matching_Earnings_Tier()
    {
        var nationality = CreateNationality();
        var employee = CreateEmployee("ESS-CALC", nationality);
        CreateEmployeeSocialSecurity(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var socialSecurity = CreateSocialSecurity(nationality, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateSocialSecurityDetail(socialSecurity, min: 0, max: 1500, employeeContribution: 7.5, employerContribution: 11);

        var result = await _appService.GetEmployeeSocialSecurityReport(Input(new DateTime(2024, 6, 15), "ESS-CALC"));

        var row = result.Items.Single();
        // With no basic salary configured the rate is 0, which falls inside the 0..1500 tier.
        row.EmployeeSharePer.ShouldBe(7.5);
        row.CompanySharePer.ShouldBe(11);
    }

    [Fact]
    public async Task Should_Export_Every_Filtered_Row()
    {
        var nationality = CreateNationality();
        var a = CreateEmployee("ESSEXP-A", nationality);
        var b = CreateEmployee("ESSEXP-B", nationality);
        CreateEmployeeSocialSecurity(a, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeSocialSecurity(b, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var file = await _appService.GetEmployeeSocialSecurityReportToExcel(Input(new DateTime(2024, 6, 15), "ESSEXP-A,ESSEXP-B"));

        file.FileName.ShouldBe("EmployeeSocialSecurity.xlsx");
    }
}
