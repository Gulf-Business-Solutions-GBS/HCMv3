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
public class EmployeeEducationsAppService_Tests : AppTestBase
{
    private readonly IEmployeeEducationsAppService _appService;

    public EmployeeEducationsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeEducationsAppService>();
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

    private Education CreateTestEducation(string code)
    {
        Education education = null;
        UsingDbContext(context =>
        {
            education = context.Set<Education>().Add(new Education
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return education;
    }

    private EducationBranch CreateTestEducationBranch(string code)
    {
        EducationBranch branch = null;
        UsingDbContext(context =>
        {
            branch = context.Set<EducationBranch>().Add(new EducationBranch
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return branch;
    }

    [Fact]
    public async Task Should_Create_EmployeeEducation()
    {
        //Arrange
        var employee = CreateTestEmployee("EDU100");
        var education = CreateTestEducation("E100");
        var branch = CreateTestEducationBranch("EB100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeEducationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 9, 1),
            EndDate = new DateTime(2019, 6, 1),
            EducationId = education.Id,
            EducationBranchId = branch.Id,
            Grade = "A"
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeEducationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EducationId.ShouldBe(education.Id);
    }

    [Fact]
    public async Task Should_Default_EndDate_To_Infinite_When_Not_Provided()
    {
        //Arrange
        var employee = CreateTestEmployee("EDU150");
        var education = CreateTestEducation("E150");
        var branch = CreateTestEducationBranch("EB150");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeEducationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 9, 1),
            EducationId = education.Id,
            EducationBranchId = branch.Id
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeEducationsInput { EmployeeId = employee.Id });
        result.Items[0].EndDate.ShouldBe(new DateTime(9999, 12, 31));
    }

    [Fact]
    public async Task Should_Get_EmployeeEducation_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("EDU200");
        var education = CreateTestEducation("E200");
        var branch = CreateTestEducationBranch("EB200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeEducationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 9, 1),
            EndDate = new DateTime(2019, 6, 1),
            EducationId = education.Id,
            EducationBranchId = branch.Id
        });

        var created = await _appService.GetAll(new GetAllEmployeeEducationsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeEducationForEdit(new EntityDto(id));

        //Assert
        result.EmployeeEducation.ShouldNotBeNull();
        result.EmployeeEducation.EducationId.ShouldBe(education.Id);
    }

    [Fact]
    public async Task Should_Update_EmployeeEducation()
    {
        //Arrange
        var employee = CreateTestEmployee("EDU300");
        var education = CreateTestEducation("E300");
        var branch = CreateTestEducationBranch("EB300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeEducationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 9, 1),
            EndDate = new DateTime(2019, 6, 1),
            EducationId = education.Id,
            EducationBranchId = branch.Id,
            Grade = "B"
        });

        var created = await _appService.GetAll(new GetAllEmployeeEducationsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeEducationDto
        {
            Id = id,
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 9, 1),
            EndDate = new DateTime(2019, 6, 1),
            EducationId = education.Id,
            EducationBranchId = branch.Id,
            Grade = "A+"
        });

        //Assert
        var result = await _appService.GetEmployeeEducationForEdit(new EntityDto(id));
        result.EmployeeEducation.Grade.ShouldBe("A+");
    }

    [Fact]
    public async Task Should_Delete_EmployeeEducation()
    {
        //Arrange
        var employee = CreateTestEmployee("EDU400");
        var education = CreateTestEducation("E400");
        var branch = CreateTestEducationBranch("EB400");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeEducationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 9, 1),
            EndDate = new DateTime(2019, 6, 1),
            EducationId = education.Id,
            EducationBranchId = branch.Id
        });

        var created = await _appService.GetAll(new GetAllEmployeeEducationsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeEducationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Allow_Multiple_Concurrent_Education_Records_For_Same_Employee()
    {
        //Arrange
        var employee = CreateTestEmployee("EDU500");
        var education = CreateTestEducation("E500");
        var branch = CreateTestEducationBranch("EB500");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeEducationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2015, 9, 1),
            EndDate = new DateTime(2019, 6, 1),
            EducationId = education.Id,
            EducationBranchId = branch.Id
        });

        //Act - overlapping period is allowed for this entity (no overlap validation ported)
        await _appService.CreateOrEdit(new CreateOrEditEmployeeEducationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2016, 1, 1),
            EndDate = new DateTime(2018, 1, 1),
            EducationId = education.Id,
            EducationBranchId = branch.Id
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeEducationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(2);
    }
}
