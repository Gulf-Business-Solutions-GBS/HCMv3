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
public class EmployeeAttendancesAppService_Tests : AppTestBase
{
    private readonly IEmployeeAttendancesAppService _appService;

    public EmployeeAttendancesAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAttendancesAppService>();
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

    private AttendanceType CreateTestAttendanceType(string code)
    {
        AttendanceType attendanceType = null;
        UsingDbContext(context =>
        {
            attendanceType = context.Set<AttendanceType>().Add(new AttendanceType
            {
                TenantId = AbpSession.TenantId ?? 1,
                Code = code,
                NameEn = code,
                NameAr = code
            }).Entity;
        });
        return attendanceType;
    }

    [Fact]
    public async Task Should_Create_Attendance_With_Day_Unit()
    {
        //Arrange
        var employee = CreateTestEmployee("EAT100");
        var attendanceType = CreateTestAttendanceType("ATT100");

        //Act
        await _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceDto
        {
            EmployeeId = employee.Id,
            AttendanceTypeId = attendanceType.Id,
            AttendanceUnit = AttendanceUnit.Day,
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2024, 4, 3)
        });

        //Assert
        var result = await _appService.GetAll(new GetAllEmployeeAttendancesInput { EmployeeId = employee.Id });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TotalAttendDays.ShouldBe(3);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        //Arrange
        var employee = CreateTestEmployee("EAT200");
        var attendanceType = CreateTestAttendanceType("ATT200");

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceDto
            {
                EmployeeId = employee.Id,
                AttendanceTypeId = attendanceType.Id,
                AttendanceUnit = AttendanceUnit.Day,
                StartDate = new DateTime(2024, 4, 10),
                EndDate = new DateTime(2024, 4, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Period_Attendance()
    {
        //Arrange
        var employee = CreateTestEmployee("EAT300");
        var attendanceType = CreateTestAttendanceType("ATT300");

        await _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceDto
        {
            EmployeeId = employee.Id,
            AttendanceTypeId = attendanceType.Id,
            AttendanceUnit = AttendanceUnit.Period,
            StartDate = new DateTime(2024, 5, 1),
            EndDate = new DateTime(2024, 5, 10)
        });

        //Act & Assert
        await Assert.ThrowsAsync<UserFriendlyException>(async () =>
            await _appService.CreateOrEdit(new CreateOrEditEmployeeAttendanceDto
            {
                EmployeeId = employee.Id,
                AttendanceTypeId = attendanceType.Id,
                AttendanceUnit = AttendanceUnit.Period,
                StartDate = new DateTime(2024, 5, 5),
                EndDate = new DateTime(2024, 5, 15)
            }));
    }
}
