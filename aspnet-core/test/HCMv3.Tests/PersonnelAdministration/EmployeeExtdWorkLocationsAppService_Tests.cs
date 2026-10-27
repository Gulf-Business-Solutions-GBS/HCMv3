using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeExtdWorkLocationsAppService_Tests : AppTestBase
{
    private readonly IEmployeeExtdWorkLocationsAppService _appService;

    public EmployeeExtdWorkLocationsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeExtdWorkLocationsAppService>();
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

    private WorkLocation CreateTestWorkLocation(string code)
    {
        WorkLocation location = null;
        UsingDbContext(context =>
        {
            location = context.Set<WorkLocation>().Add(new WorkLocation
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return location;
    }

    [Fact]
    public async Task Should_Create_EmployeeExtdWorkLocation()
    {
        //Arrange
        var employee = CreateTestEmployee("EEWL100");
        var location = CreateTestWorkLocation("WL100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeExtdWorkLocationDto
        {
            EmployeeId = employee.Id,
            WorkLocationId = location.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeExtdWorkLocationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Overlapping_WorkLocation()
    {
        //Arrange
        var employee = CreateTestEmployee("EEWL200");
        var location = CreateTestWorkLocation("WL200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeExtdWorkLocationDto
        {
            EmployeeId = employee.Id,
            WorkLocationId = location.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeExtdWorkLocationDto
            {
                EmployeeId = employee.Id,
                WorkLocationId = location.Id,
                StartDate = new DateTime(2024, 6, 1),
                EndDate = new DateTime(2024, 12, 31)
            }));
    }
}
