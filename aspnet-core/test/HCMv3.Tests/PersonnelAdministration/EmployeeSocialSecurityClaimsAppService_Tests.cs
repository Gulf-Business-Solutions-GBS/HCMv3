using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeSocialSecurityClaimsAppService_Tests : AppTestBase
{
    private readonly IEmployeeSocialSecurityClaimsAppService _appService;

    public EmployeeSocialSecurityClaimsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSocialSecurityClaimsAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
        Employee employee = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                FirstNameEn = "F",
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "F L",
                FullNameAr = "ف ل",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });
        return employee;
    }

    [Fact]
    public async Task Should_Create_Claim()
    {
        //Arrange
        var employee = CreateTestEmployee("SSC100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeSocialSecurityClaimDto
        {
            EmployeeId = employee.Id,
            ClaimDate = new DateTime(2024, 3, 1),
            EmployeeContributionAmount = 100,
            EmployerContributionAmount = 200
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeSocialSecurityClaimsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployerContributionAmount.ShouldBe(200);
    }

    [Fact]
    public async Task Should_Throw_When_Both_Contributions_Are_Zero()
    {
        //Arrange
        var employee = CreateTestEmployee("SSC200");

        //Act & Assert
        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeSocialSecurityClaimDto
            {
                EmployeeId = employee.Id,
                ClaimDate = new DateTime(2024, 3, 1),
                EmployeeContributionAmount = 0,
                EmployerContributionAmount = 0
            }));
    }
}
