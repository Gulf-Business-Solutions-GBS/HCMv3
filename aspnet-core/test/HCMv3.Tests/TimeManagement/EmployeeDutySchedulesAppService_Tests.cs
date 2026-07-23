using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeDutySchedulesAppService_Tests : TimeManagementAppServiceTestBase
{
    private EmployeeDutySchedulesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeDutySchedulesAppService(FakeRepository.For<EmployeeDutySchedule>(Context)));
    }

    private int CreateNationality()
    {
        var entity = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = "Local",
            NameAr = "محلي"
        };
        Context.Nationalities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateEmployee(string firstName)
    {
        var entity = new Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            FirstNameEn = firstName,
            LastNameEn = "Doe",
            FirstNameAr = firstName,
            LastNameAr = "Doe",
            FullNameEn = firstName,
            FullNameAr = firstName,
            NationalityId = CreateNationality()
        };
        Context.Employees.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateDutyPeriod(string code)
    {
        var entity = new DutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(9999, 12, 31)
        };
        Context.DutyPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateRosterDutySchedule(string code, int dutyPeriodId)
    {
        var entity = new RosterDutySchedule
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            StartDate = new DateTime(2026, 1, 1),
            DutyPeriodId = dutyPeriodId,
            StartPoint = 1
        };
        Context.RosterDutySchedules.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeDutySchedule()
    {
        var employeeId = CreateEmployee("John");
        var dutyPeriodId = CreateDutyPeriod("DP1");
        var rosterDutyScheduleId = CreateRosterDutySchedule("RDS1", dutyPeriodId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeDutyScheduleDto
        {
            EmployeeId = employeeId,
            RosterDutyScheduleId = rosterDutyScheduleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30)
        });

        var result = await service.GetAll(new GetAllEmployeeDutySchedulesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].EmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Date_Range_For_Same_Employee()
    {
        var employeeId = CreateEmployee("Jane");
        var dutyPeriodId = CreateDutyPeriod("DP2");
        var rosterDutyScheduleId = CreateRosterDutySchedule("RDS2", dutyPeriodId);
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeDutyScheduleDto
        {
            EmployeeId = employeeId,
            RosterDutyScheduleId = rosterDutyScheduleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeDutyScheduleDto
            {
                EmployeeId = employeeId,
                RosterDutyScheduleId = rosterDutyScheduleId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 12, 31)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var employeeId = CreateEmployee("Alex");
        var dutyPeriodId = CreateDutyPeriod("DP3");
        var rosterDutyScheduleId = CreateRosterDutySchedule("RDS3", dutyPeriodId);
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeDutyScheduleDto
            {
                EmployeeId = employeeId,
                RosterDutyScheduleId = rosterDutyScheduleId,
                StartDate = new DateTime(2026, 6, 30),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }
}
