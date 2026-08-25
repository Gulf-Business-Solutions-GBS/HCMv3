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
public class ProvisionChangeReportAppService_Tests : AppTestBase
{
    private readonly IProvisionChangeReportAppService _appService;

    public ProvisionChangeReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IProvisionChangeReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
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
                Status = status
            }).Entity;
        });

        return employee;
    }

    [Fact]
    public async Task Should_Return_Zero_Difference_When_Employee_Has_No_Basic_Pay_Change()
    {
        var employee = CreateEmployee("PC-1");

        var result = await _appService.GetProvisionChangeReport(new GetProvisionChangeReportInput
        {
            Month = 6,
            Year = 2024,
            EmployeeCode = "PC-1",
            MaxResultCount = 100
        });

        var row = result.Items.Single();
        row.TotalAlDifferenceAmount.ShouldBe(0);
        row.TotalIndemnityDifferenceAmount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Exclude_Inactive_Employees_Without_FnF_Payment_By_Default()
    {
        CreateEmployee("PC-INACTIVE", EmployeeStatus.Inactive);

        var result = await _appService.GetProvisionChangeReport(new GetProvisionChangeReportInput
        {
            Month = 6,
            Year = 2024,
            EmployeeCode = "PC-INACTIVE",
            MaxResultCount = 100
        });

        result.Items.ShouldBeEmpty();
    }
}
