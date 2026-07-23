using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeManagersAppService_Tests : AppTestBase
{
    private readonly IEmployeeManagersAppService _employeeManagersAppService;

    public EmployeeManagersAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _employeeManagersAppService = Resolve<IEmployeeManagersAppService>();
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
    public async Task Should_Auto_Chain_EndDate_When_New_Manager_Record_Starts()
    {
        //Arrange
        var employee = CreateTestEmployee("M100");
        var manager1 = CreateTestEmployee("M101");
        var manager2 = CreateTestEmployee("M102");

        await _employeeManagersAppService.CreateOrEdit(new CreateOrEditEmployeeManagerDto
        {
            EmployeeId = employee.Id,
            ManagerId = manager1.Id,
            StartDate = new DateTime(2020, 1, 1)
        });

        //Act - a later record starts, should chain the first record's EndDate
        await _employeeManagersAppService.CreateOrEdit(new CreateOrEditEmployeeManagerDto
        {
            EmployeeId = employee.Id,
            ManagerId = manager2.Id,
            StartDate = new DateTime(2021, 1, 1)
        });

        //Assert
        UsingDbContext(context =>
        {
            var records = context.Set<EmployeeManager>()
                .Where(m => m.EmployeeId == employee.Id)
                .OrderBy(m => m.StartDate)
                .ToList();

            records.Count.ShouldBe(2);
            records[0].EndDate.ShouldBe(new DateTime(2020, 12, 31));
            records[1].EndDate.ShouldBe(new DateTime(9999, 12, 31));
        });
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_StartDate_For_Same_Employee()
    {
        //Arrange
        var employee = CreateTestEmployee("M200");
        var manager1 = CreateTestEmployee("M201");
        var manager2 = CreateTestEmployee("M202");

        await _employeeManagersAppService.CreateOrEdit(new CreateOrEditEmployeeManagerDto
        {
            EmployeeId = employee.Id,
            ManagerId = manager1.Id,
            StartDate = new DateTime(2020, 1, 1)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _employeeManagersAppService.CreateOrEdit(new CreateOrEditEmployeeManagerDto
            {
                EmployeeId = employee.Id,
                ManagerId = manager2.Id,
                StartDate = new DateTime(2020, 1, 1)
            }));
    }
}
