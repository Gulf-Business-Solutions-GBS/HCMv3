using System;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeAccountAssignmentsAppService_Tests : AppTestBase
{
    private readonly IEmployeeAccountAssignmentsAppService _appService;

    public EmployeeAccountAssignmentsAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAccountAssignmentsAppService>();
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

    private CostElement CreateTestCostElement(string code)
    {
        CostElement costElement = null;
        UsingDbContext(context =>
        {
            costElement = context.Set<CostElement>().Add(new CostElement
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return costElement;
    }

    [Fact]
    public async Task Should_Create_EmployeeAccountAssignment_With_Seven_CostElements()
    {
        //Arrange
        var employee = CreateTestEmployee("EAA100");
        var ce1 = CreateTestCostElement("CE1");
        var ce2 = CreateTestCostElement("CE2");
        var ce3 = CreateTestCostElement("CE3");
        var ce4 = CreateTestCostElement("CE4");
        var ce5 = CreateTestCostElement("CE5");
        var ce6 = CreateTestCostElement("CE6");
        var ce7 = CreateTestCostElement("CE7");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAccountAssignmentDto
        {
            EmployeeId = employee.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 12, 31),
            CostElement1Id = ce1.Id,
            CostElement2Id = ce2.Id,
            CostElement3Id = ce3.Id,
            CostElement4Id = ce4.Id,
            CostElement5Id = ce5.Id,
            CostElement6Id = ce6.Id,
            CostElement7Id = ce7.Id,
            CostPercentage = 100
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAccountAssignmentsInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CostElement7NameEn.ShouldBe("CE7");
    }
}
