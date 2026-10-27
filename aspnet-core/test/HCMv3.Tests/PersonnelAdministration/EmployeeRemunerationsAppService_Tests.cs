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
public class EmployeeRemunerationsAppService_Tests : AppTestBase
{
    private readonly IEmployeeRemunerationsAppService _appService;

    public EmployeeRemunerationsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeRemunerationsAppService>();
    }

    private Employee CreateTestEmployee(string code)
    {
        Nationality nationality = null;
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

        Employee employee = null;
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
    public async Task Should_Create_EmployeeRemuneration()
    {
        //Arrange
        var employee = CreateTestEmployee("ER100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRemunerationDto
        {
            EmployeeId = employee.Id,
            PaymentDate = new DateTime(2024, 3, 15),
            PaymentNumber = 3,
            PaymentUnit = PaymentUnit.Days
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeRemunerationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_PaymentNumber_Is_Zero()
    {
        //Arrange
        var employee = CreateTestEmployee("ER200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeRemunerationDto
            {
                EmployeeId = employee.Id,
                PaymentDate = new DateTime(2024, 3, 15),
                PaymentNumber = 0
            }));
    }
}
