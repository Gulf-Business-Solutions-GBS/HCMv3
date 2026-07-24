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
public class EmployeeCertificationsAppService_Tests : AppTestBase
{
    private readonly IEmployeeCertificationsAppService _appService;

    public EmployeeCertificationsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeCertificationsAppService>();
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

    private Institution CreateTestInstitution(string code)
    {
        Institution institution = null;
        UsingDbContext(context =>
        {
            institution = context.Set<Institution>().Add(new Institution
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return institution;
    }

    [Fact]
    public async Task Should_Create_EmployeeCertification()
    {
        //Arrange
        var employee = CreateTestEmployee("CE100");
        var institution = CreateTestInstitution("I100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeCertificationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 3, 1),
            Course = "PMP",
            InstitutionId = institution.Id
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeCertificationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Course.ShouldBe("PMP");
    }

    [Fact]
    public async Task Should_Get_EmployeeCertification_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("CE200");
        var institution = CreateTestInstitution("I200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeCertificationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 3, 1),
            Course = "Scrum Master",
            InstitutionId = institution.Id
        });

        var created = await _appService.GetAll(new GetAllEmployeeCertificationsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeCertificationForEdit(new EntityDto(id));

        //Assert
        result.EmployeeCertification.ShouldNotBeNull();
        result.EmployeeCertification.Course.ShouldBe("Scrum Master");
    }

    [Fact]
    public async Task Should_Update_EmployeeCertification()
    {
        //Arrange
        var employee = CreateTestEmployee("CE300");
        var institution = CreateTestInstitution("I300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeCertificationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 3, 1),
            Course = "Course A",
            InstitutionId = institution.Id
        });

        var created = await _appService.GetAll(new GetAllEmployeeCertificationsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeCertificationDto
        {
            Id = id,
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 3, 1),
            Course = "Course B",
            InstitutionId = institution.Id
        });

        //Assert
        var result = await _appService.GetEmployeeCertificationForEdit(new EntityDto(id));
        result.EmployeeCertification.Course.ShouldBe("Course B");
    }

    [Fact]
    public async Task Should_Delete_EmployeeCertification()
    {
        //Arrange
        var employee = CreateTestEmployee("CE400");
        var institution = CreateTestInstitution("I400");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeCertificationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 3, 1),
            Course = "Course C",
            InstitutionId = institution.Id
        });

        var created = await _appService.GetAll(new GetAllEmployeeCertificationsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeCertificationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_On_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("CE500");
        var institution = CreateTestInstitution("I500");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeCertificationDto
            {
                EmployeeId = employee.Id,
                StartDate = new DateTime(2020, 3, 1),
                EndDate = new DateTime(2020, 1, 1),
                Course = "Invalid",
                InstitutionId = institution.Id
            }));
    }

    [Fact]
    public async Task Should_Allow_Overlapping_Certification_Periods()
    {
        //Arrange - legacy overlap-validation for this entity is dead code, so overlaps are allowed
        var employee = CreateTestEmployee("CE600");
        var institution = CreateTestInstitution("I600");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeCertificationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31),
            Course = "Course D",
            InstitutionId = institution.Id
        });

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeCertificationDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2020, 6, 1),
            EndDate = new DateTime(2021, 6, 1),
            Course = "Course E",
            InstitutionId = institution.Id
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeCertificationsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(2);
    }
}
