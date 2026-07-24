using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

// ReSharper disable once InconsistentNaming
public class EmployeeAbsencesAppService_Tests : AppTestBase
{
    private readonly IEmployeeAbsencesAppService _appService;

    public EmployeeAbsencesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAbsencesAppService>();
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

    private AbsenceType CreateTestAbsenceType(string code)
    {
        AbsenceType absenceType = null;
        UsingDbContext(context =>
        {
            absenceType = context.Set<AbsenceType>().Add(new AbsenceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return absenceType;
    }

    [Fact]
    public async Task Should_Create_Absence()
    {
        //Arrange
        var employee = CreateTestEmployee("EA100");
        var absenceType = CreateTestAbsenceType("AT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 5)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAbsencesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CalendarDays.ShouldBe(5);
        result.Items[0].TotalLeaveDays.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("EA200");
        var absenceType = CreateTestAbsenceType("AT200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
            {
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceType.Id,
                StartDate = new DateTime(2024, 1, 10),
                EndDate = new DateTime(2024, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Absence_Period()
    {
        //Arrange
        var employee = CreateTestEmployee("EA300");
        var absenceType = CreateTestAbsenceType("AT300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
        {
            EmployeeId = employee.Id,
            AbsenceTypeId = absenceType.Id,
            StartDate = new DateTime(2024, 2, 1),
            EndDate = new DateTime(2024, 2, 10)
        });

        //Act & Assert - overlapping period for the same employee
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAbsenceDto
            {
                EmployeeId = employee.Id,
                AbsenceTypeId = absenceType.Id,
                StartDate = new DateTime(2024, 2, 5),
                EndDate = new DateTime(2024, 2, 15)
            }));
    }
}
