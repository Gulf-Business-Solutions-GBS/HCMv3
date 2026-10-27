using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeGratuitiesAppService_Tests : AppTestBase
{
    private readonly IEmployeeGratuitiesAppService _appService;

    public EmployeeGratuitiesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeGratuitiesAppService>();
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
    public async Task Should_Create_Gratuity()
    {
        //Arrange
        var employee = CreateTestEmployee("GR100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeGratuityDto
        {
            EmployeeId = employee.Id,
            GratuityPaidDate = new DateTime(2024, 6, 1),
            GratuityPaidDays = 10
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeGratuitiesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].GratuityPaidDays.ShouldBe(10);
    }

    [Fact]
    public async Task Should_Delete_Gratuity()
    {
        //Arrange
        var employee = CreateTestEmployee("GR200");
        await _appService.CreateOrEdit(new CreateOrEditEmployeeGratuityDto
        {
            EmployeeId = employee.Id,
            GratuityPaidDate = new DateTime(2024, 6, 1),
            GratuityPaidDays = 5
        });

        var created = await _appService.GetAll(new GetAllEmployeeGratuitiesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeGratuitiesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }
}
