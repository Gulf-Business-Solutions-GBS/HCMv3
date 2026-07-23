using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class RosterDutySchedulesAppService_Tests : TimeManagementAppServiceTestBase
{
    private RosterDutySchedulesAppService CreateAppService()
    {
        return CreateService(() => new RosterDutySchedulesAppService(FakeRepository.For<RosterDutySchedule>(Context)));
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

    [Fact]
    public async Task Should_Create_RosterDutySchedule_Referencing_Classic_DutyPeriod()
    {
        var dutyPeriodId = CreateDutyPeriod("DP1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterDutyScheduleDto
        {
            Code = "RDS1",
            NameEn = "Schedule A",
            NameAr = "جدول أ",
            StartDate = new DateTime(2026, 1, 1),
            DutyPeriodId = dutyPeriodId,
            StartPoint = 1
        });

        var result = await service.GetAll(new GetAllRosterDutySchedulesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].DutyPeriodId.ShouldBe(dutyPeriodId);
    }

    [Fact]
    public async Task Should_Throw_When_StartPoint_Not_Greater_Than_Zero()
    {
        var dutyPeriodId = CreateDutyPeriod("DP2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditRosterDutyScheduleDto
            {
                Code = "RDS2",
                NameEn = "Schedule B",
                NameAr = "جدول ب",
                StartDate = new DateTime(2026, 1, 1),
                DutyPeriodId = dutyPeriodId,
                StartPoint = 0
            }));
    }
}
