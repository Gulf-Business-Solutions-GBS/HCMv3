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
public class EmployeeHealthReimbursesAppService_Tests : AppTestBase
{
    private readonly IEmployeeHealthReimbursesAppService _appService;

    public EmployeeHealthReimbursesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeHealthReimbursesAppService>();
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
    public async Task Should_Create_EmployeeHealthReimburse()
    {
        //Arrange
        var employee = CreateTestEmployee("EHR100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeHealthReimburseDto
        {
            EmployeeId = employee.Id,
            PaymentDate = new DateTime(2024, 3, 15),
            Amount = 100
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeHealthReimbursesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Amount_Is_Zero()
    {
        //Arrange
        var employee = CreateTestEmployee("EHR200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeHealthReimburseDto
            {
                EmployeeId = employee.Id,
                PaymentDate = new DateTime(2024, 3, 15),
                Amount = 0
            }));
    }
}
