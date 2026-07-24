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
public class EmployeeOvertimesAppService_Tests : AppTestBase
{
    private readonly IEmployeeOvertimesAppService _appService;

    public EmployeeOvertimesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeOvertimesAppService>();
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

    private OvertimeType CreateTestOvertimeType(string code)
    {
        OvertimeType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<OvertimeType>().Add(new OvertimeType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code,
                OvertimeRate = 1.5
            }).Entity;
        });
        return type;
    }

    [Fact]
    public async Task Should_Create_EmployeeOvertime()
    {
        //Arrange
        var employee = CreateTestEmployee("EOT100");
        var type = CreateTestOvertimeType("OT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 4
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeOvertimesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_EffectiveDate()
    {
        //Arrange
        var employee = CreateTestEmployee("EOT200");
        var type = CreateTestOvertimeType("OT200");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
        {
            EmployeeId = employee.Id,
            OvertimeTypeId = type.Id,
            EffectiveDate = new DateTime(2024, 3, 15),
            ApprovedOvertimeHours = 4
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeOvertimeDto
            {
                EmployeeId = employee.Id,
                OvertimeTypeId = type.Id,
                EffectiveDate = new DateTime(2024, 3, 15),
                ApprovedOvertimeHours = 2
            }));
    }
}
