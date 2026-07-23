using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class RosterDailyDutiesAppService_Tests : TimeManagementAppServiceTestBase
{
    private RosterDailyDutiesAppService CreateAppService()
    {
        return CreateService(() => new RosterDailyDutiesAppService(FakeRepository.For<RosterDailyDuty>(Context)));
    }

    private int CreateDutyPeriod(string code)
    {
        var entity = new DutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        Context.DutyPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateRosterDutySchedule(string code)
    {
        var dutyPeriodId = CreateDutyPeriod(code + "-DP");
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

    private int CreateDailyDuty(string code)
    {
        var entity = new DailyDuty
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0)
        };
        Context.DailyDuties.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_RosterDailyDuty()
    {
        var rosterDutyScheduleId = CreateRosterDutySchedule("RDS1");
        var dailyDutyId = CreateDailyDuty("DD1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterDailyDutyDto
        {
            CalendarDate = new DateTime(2026, 1, 5),
            RosterDutyScheduleId = rosterDutyScheduleId,
            DailyDutyId = dailyDutyId
        });

        var result = await service.GetAll(new GetAllRosterDailyDutiesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].RosterDutyScheduleId.ShouldBe(rosterDutyScheduleId);
    }

    [Fact]
    public async Task Should_Throw_When_CalendarDate_Is_Default()
    {
        var rosterDutyScheduleId = CreateRosterDutySchedule("RDS2");
        var dailyDutyId = CreateDailyDuty("DD2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditRosterDailyDutyDto
            {
                CalendarDate = default,
                RosterDutyScheduleId = rosterDutyScheduleId,
                DailyDutyId = dailyDutyId
            }));
    }
}
