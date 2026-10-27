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
public class EmployeeGrievancesAppService_Tests : AppTestBase
{
    private readonly IEmployeeGrievancesAppService _appService;

    public EmployeeGrievancesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeGrievancesAppService>();
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

    private GrievanceType CreateTestGrievanceType(string code)
    {
        GrievanceType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<GrievanceType>().Add(new GrievanceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return type;
    }

    [Fact]
    public async Task Should_Create_EmployeeGrievance()
    {
        //Arrange
        var employee = CreateTestEmployee("EG100");
        var type = CreateTestGrievanceType("GT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeGrievanceDto
        {
            EmployeeId = employee.Id,
            GrievanceTypeId = type.Id,
            GrievanceDate = new DateTime(2024, 3, 15)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeGrievancesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Grievance()
    {
        //Arrange
        var employee = CreateTestEmployee("EG200");
        var type = CreateTestGrievanceType("GT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeGrievanceDto
        {
            EmployeeId = employee.Id,
            GrievanceTypeId = type.Id,
            GrievanceDate = new DateTime(2024, 3, 15)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeGrievanceDto
            {
                EmployeeId = employee.Id,
                GrievanceTypeId = type.Id,
                GrievanceDate = new DateTime(2024, 3, 15)
            }));
    }
}
