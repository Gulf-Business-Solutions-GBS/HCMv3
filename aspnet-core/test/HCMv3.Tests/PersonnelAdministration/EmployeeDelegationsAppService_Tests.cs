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
public class EmployeeDelegationsAppService_Tests : AppTestBase
{
    private readonly IEmployeeDelegationsAppService _appService;

    public EmployeeDelegationsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDelegationsAppService>();
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
    public async Task Should_Create_EmployeeDelegation()
    {
        //Arrange
        var employee = CreateTestEmployee("DL100");
        var delegateTo = CreateTestEmployee("DL101");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDelegationDto
        {
            EmployeeId = employee.Id,
            DelegateToId = delegateTo.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 6, 1)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDelegationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].DelegateToId.ShouldBe(delegateTo.Id);
    }

    [Fact]
    public async Task Should_Get_EmployeeDelegation_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("DL200");
        var delegateTo = CreateTestEmployee("DL201");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDelegationDto
        {
            EmployeeId = employee.Id,
            DelegateToId = delegateTo.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 6, 1),
            Comments = "Test"
        });

        var created = await _appService.GetAll(new GetAllEmployeeDelegationsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeDelegationForEdit(new EntityDto(id));

        //Assert
        result.EmployeeDelegation.ShouldNotBeNull();
        result.EmployeeDelegation.Comments.ShouldBe("Test");
    }

    [Fact]
    public async Task Should_Update_EmployeeDelegation()
    {
        //Arrange
        var employee = CreateTestEmployee("DL300");
        var delegateTo = CreateTestEmployee("DL301");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDelegationDto
        {
            EmployeeId = employee.Id,
            DelegateToId = delegateTo.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 6, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeDelegationsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeDelegationDto
        {
            Id = id,
            EmployeeId = employee.Id,
            DelegateToId = delegateTo.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31),
            Comments = "Updated"
        });

        //Assert
        var result = await _appService.GetEmployeeDelegationForEdit(new EntityDto(id));
        result.EmployeeDelegation.EndDate.ShouldBe(new DateTime(2020, 12, 31));
        result.EmployeeDelegation.Comments.ShouldBe("Updated");
    }

    [Fact]
    public async Task Should_Delete_EmployeeDelegation()
    {
        //Arrange
        var employee = CreateTestEmployee("DL400");
        var delegateTo = CreateTestEmployee("DL401");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDelegationDto
        {
            EmployeeId = employee.Id,
            DelegateToId = delegateTo.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 6, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeDelegationsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeDelegationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_On_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("DL500");
        var delegateTo = CreateTestEmployee("DL501");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeDelegationDto
            {
                EmployeeId = employee.Id,
                DelegateToId = delegateTo.Id,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2020, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Delegation_Period()
    {
        //Arrange
        var employee = CreateTestEmployee("DL600");
        var delegateTo1 = CreateTestEmployee("DL601");
        var delegateTo2 = CreateTestEmployee("DL602");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDelegationDto
        {
            EmployeeId = employee.Id,
            DelegateToId = delegateTo1.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31)
        });

        //Act & Assert - overlapping period for the same employee
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeDelegationDto
            {
                EmployeeId = employee.Id,
                DelegateToId = delegateTo2.Id,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2021, 6, 1)
            }));
    }
}
