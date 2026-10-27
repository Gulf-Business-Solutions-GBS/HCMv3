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
public class EmployeeRelationshipsAppService_Tests : AppTestBase
{
    private readonly IEmployeeRelationshipsAppService _appService;

    public EmployeeRelationshipsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeRelationshipsAppService>();
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

    private RelationshipType CreateTestRelationshipType(string code)
    {
        RelationshipType relationshipType = null;
        UsingDbContext(context =>
        {
            relationshipType = context.Set<RelationshipType>().Add(new RelationshipType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return relationshipType;
    }

    [Fact]
    public async Task Should_Create_EmployeeRelationship()
    {
        //Arrange
        var employee = CreateTestEmployee("R100");
        var relatedEmployee = CreateTestEmployee("R101");
        var relationshipType = CreateTestRelationshipType("RT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRelationshipDto
        {
            EmployeeId = employee.Id,
            RelationshipTypeId = relationshipType.Id,
            RelatedEmployeeId = relatedEmployee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 6, 1)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeRelationshipsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].RelatedEmployeeId.ShouldBe(relatedEmployee.Id);
    }

    [Fact]
    public async Task Should_Get_EmployeeRelationship_For_Edit()
    {
        //Arrange
        var employee = CreateTestEmployee("R200");
        var relatedEmployee = CreateTestEmployee("R201");
        var relationshipType = CreateTestRelationshipType("RT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRelationshipDto
        {
            EmployeeId = employee.Id,
            RelationshipTypeId = relationshipType.Id,
            RelatedEmployeeId = relatedEmployee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 6, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeRelationshipsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        var result = await _appService.GetEmployeeRelationshipForEdit(new EntityDto(id));

        //Assert
        result.EmployeeRelationship.ShouldNotBeNull();
        result.EmployeeRelationship.RelationshipTypeId.ShouldBe(relationshipType.Id);
    }

    [Fact]
    public async Task Should_Update_EmployeeRelationship()
    {
        //Arrange
        var employee = CreateTestEmployee("R300");
        var relatedEmployee = CreateTestEmployee("R301");
        var relationshipType = CreateTestRelationshipType("RT300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRelationshipDto
        {
            EmployeeId = employee.Id,
            RelationshipTypeId = relationshipType.Id,
            RelatedEmployeeId = relatedEmployee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 6, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeRelationshipsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeRelationshipDto
        {
            Id = id,
            EmployeeId = employee.Id,
            RelationshipTypeId = relationshipType.Id,
            RelatedEmployeeId = relatedEmployee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31)
        });

        //Assert
        var result = await _appService.GetEmployeeRelationshipForEdit(new EntityDto(id));
        result.EmployeeRelationship.EndDate.ShouldBe(new DateTime(2020, 12, 31));
    }

    [Fact]
    public async Task Should_Delete_EmployeeRelationship()
    {
        //Arrange
        var employee = CreateTestEmployee("R400");
        var relatedEmployee = CreateTestEmployee("R401");
        var relationshipType = CreateTestRelationshipType("RT400");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRelationshipDto
        {
            EmployeeId = employee.Id,
            RelationshipTypeId = relationshipType.Id,
            RelatedEmployeeId = relatedEmployee.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 6, 1)
        });

        var created = await _appService.GetAll(new GetAllEmployeeRelationshipsInput { EmployeeId = employee.Id });
        var id = created.Items[0].Id;

        //Act
        await _appService.Delete(new EntityDto(id));

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeRelationshipsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Throw_On_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("R500");
        var relatedEmployee = CreateTestEmployee("R501");
        var relationshipType = CreateTestRelationshipType("RT500");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeRelationshipDto
            {
                EmployeeId = employee.Id,
                RelationshipTypeId = relationshipType.Id,
                RelatedEmployeeId = relatedEmployee.Id,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2020, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Relationship_For_Same_Employee_And_Type()
    {
        //Arrange
        var employee = CreateTestEmployee("R600");
        var relatedEmployee1 = CreateTestEmployee("R601");
        var relatedEmployee2 = CreateTestEmployee("R602");
        var relationshipType = CreateTestRelationshipType("RT600");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeRelationshipDto
        {
            EmployeeId = employee.Id,
            RelationshipTypeId = relationshipType.Id,
            RelatedEmployeeId = relatedEmployee1.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31)
        });

        //Act & Assert - overlapping period for same employee + relationship type
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeRelationshipDto
            {
                EmployeeId = employee.Id,
                RelationshipTypeId = relationshipType.Id,
                RelatedEmployeeId = relatedEmployee2.Id,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2021, 6, 1)
            }));
    }
}
