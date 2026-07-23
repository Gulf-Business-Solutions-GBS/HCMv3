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
public class EmployeeDottedLineManagersAppService_Tests : AppTestBase
{
    private readonly IEmployeeDottedLineManagersAppService _appService;

    public EmployeeDottedLineManagersAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeDottedLineManagersAppService>();
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
    public async Task Should_Throw_On_Overlapping_DottedLineManager_Period()
    {
        //Arrange
        var employee = CreateTestEmployee("D100");
        var manager1 = CreateTestEmployee("D101");
        var manager2 = CreateTestEmployee("D102");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeDottedLineManagerDto
        {
            EmployeeId = employee.Id,
            ManagerId = manager1.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31)
        });

        //Act & Assert - overlapping period for the same employee
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeDottedLineManagerDto
            {
                EmployeeId = employee.Id,
                ManagerId = manager2.Id,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2021, 6, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("D200");
        var manager1 = CreateTestEmployee("D201");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeDottedLineManagerDto
            {
                EmployeeId = employee.Id,
                ManagerId = manager1.Id,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2020, 1, 1)
            }));
    }
}
