using System;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeOnHoldsAppService_Tests : AppTestBase
{
    private readonly IEmployeeOnHoldsAppService _appService;

    public EmployeeOnHoldsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeOnHoldsAppService>();
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
    public async Task Should_Create_OnHold()
    {
        //Arrange
        var employee = CreateTestEmployee("OH100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOnHoldDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31),
            OnHold = true,
            OnHoldBasicPay = true
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeOnHoldsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].OnHoldBasicPay.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("OH200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeOnHoldDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2024, 2, 1),
                EndDate = new DateTime(2024, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Hold_Period()
    {
        //Arrange
        var employee = CreateTestEmployee("OH300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeOnHoldDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 2, 28)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeOnHoldDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2024, 2, 15),
                EndDate = new DateTime(2024, 3, 15)
            }));
    }

    [Fact]
    public async Task Should_Delete_OnHold()
    {
        //Arrange
        var employee = CreateTestEmployee("OH400");
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOnHoldDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 31)
        });

        var created = await _appService.GetAll(new GetAllEmployeeOnHoldsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeOnHoldsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }
}
