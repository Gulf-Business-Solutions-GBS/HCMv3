using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeSocialSecuritiesAppService_Tests : AppTestBase
{
    private readonly IEmployeeSocialSecuritiesAppService _appService;

    public EmployeeSocialSecuritiesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeSocialSecuritiesAppService>();
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
    public async Task Should_Create_SocialSecurity()
    {
        //Arrange
        var employee = CreateTestEmployee("ESS100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeSocialSecurityDto
        {
            EmployeeId = employee.Id,
            SocialSecurityNum = 123456789012,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            SocialSecurityContrib = true
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeSocialSecuritiesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].SocialSecurityNum.ShouldBe(123456789012);
    }
}
