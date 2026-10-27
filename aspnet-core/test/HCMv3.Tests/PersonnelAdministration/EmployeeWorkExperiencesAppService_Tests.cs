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
public class EmployeeWorkExperiencesAppService_Tests : AppTestBase
{
    private readonly IEmployeeWorkExperiencesAppService _appService;

    public EmployeeWorkExperiencesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeWorkExperiencesAppService>();
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
    public async Task Should_Create_EmployeeWorkExperience()
    {
        //Arrange
        var employee = CreateTestEmployee("WE100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeWorkExperienceDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 1, 1),
            EndDate = new DateTime(2018, 1, 1),
            Company = "Acme Corp",
            Position = "Developer"
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeWorkExperiencesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Company.ShouldBe("Acme Corp");
    }

    [Fact]
    public async Task Should_Get_EmployeeWorkExperience_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("WE200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeWorkExperienceDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 1, 1),
            EndDate = new DateTime(2018, 1, 1),
            Company = "Beta Inc",
            Position = "Senior Developer"
        });

        var created = await _appService.GetAll(new GetAllEmployeeWorkExperiencesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeWorkExperienceForEdit(new EntityDto(id));

        //Assert
        result.EmployeeWorkExperience.ShouldNotBeNull();
        result.EmployeeWorkExperience.Company.ShouldBe("Beta Inc");
    }

    [Fact]
    public async Task Should_Update_EmployeeWorkExperience()
    {
        //Arrange
        var employee = CreateTestEmployee("WE300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeWorkExperienceDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 1, 1),
            EndDate = new DateTime(2018, 1, 1),
            Company = "Gamma LLC",
            Position = "Developer"
        });

        var created = await _appService.GetAll(new GetAllEmployeeWorkExperiencesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeWorkExperienceDto
        {
            Id = id,
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 1, 1),
            EndDate = new DateTime(2018, 1, 1),
            Company = "Gamma LLC",
            Position = "Lead Developer"
        });

        //Assert
        var result = await _appService.GetEmployeeWorkExperienceForEdit(new EntityDto(id));
        result.EmployeeWorkExperience.Position.ShouldBe("Lead Developer");
    }

    [Fact]
    public async Task Should_Delete_EmployeeWorkExperience()
    {
        //Arrange
        var employee = CreateTestEmployee("WE400");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeWorkExperienceDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 1, 1),
            EndDate = new DateTime(2018, 1, 1),
            Company = "Delta Ltd",
            Position = "Developer"
        });

        var created = await _appService.GetAll(new GetAllEmployeeWorkExperiencesInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeWorkExperiencesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_On_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("WE500");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeWorkExperienceDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2018, 1, 1),
                EndDate = new DateTime(2015, 1, 1),
                Company = "Invalid Co",
                Position = "Developer"
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_WorkExperience_Period()
    {
        //Arrange
        var employee = CreateTestEmployee("WE600");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeWorkExperienceDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 1, 1),
            EndDate = new DateTime(2018, 1, 1),
            Company = "Epsilon Co",
            Position = "Developer"
        });

        //Act & Assert - overlapping period for the same employee
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeWorkExperienceDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2017, 1, 1),
                EndDate = new DateTime(2019, 1, 1),
                Company = "Zeta Co",
                Position = "Developer"
            }));
    }
}
