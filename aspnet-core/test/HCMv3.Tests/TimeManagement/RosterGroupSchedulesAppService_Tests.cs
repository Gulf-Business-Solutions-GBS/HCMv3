using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TimeManagement;

// ReSharper disable once InconsistentNaming
public class RosterGroupSchedulesAppService_Tests : TimeManagementAppServiceTestBase
{
    private RosterGroupSchedulesAppService CreateAppService()
    {
        return CreateService(() => new RosterGroupSchedulesAppService(FakeRepository.For<RosterGroupSchedule>(Context)));
    }

    private int CreateRosterGroup(string code)
    {
        var entity = new RosterGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.RosterGroups.Add(entity);
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
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        };
        Context.DutyPeriods.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_RosterGroupSchedule_Referencing_Classic_DutyPeriod()
    {
        var rosterGroupId = CreateRosterGroup("RG1");
        var dutyPeriodId = CreateDutyPeriod("DP1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRosterGroupScheduleDto
        {
            RosterGroupId = rosterGroupId,
            DutyPeriodId = dutyPeriodId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            StartPoint = 1
        });

        var result = await service.GetAll(new GetAllRosterGroupSchedulesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].DutyPeriodId.ShouldBe(dutyPeriodId);
    }

    [Fact]
    public async Task Should_Throw_When_StartPoint_Not_Greater_Than_Zero()
    {
        var rosterGroupId = CreateRosterGroup("RG2");
        var dutyPeriodId = CreateDutyPeriod("DP2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditRosterGroupScheduleDto
            {
                RosterGroupId = rosterGroupId,
                DutyPeriodId = dutyPeriodId,
                StartDate = new DateTime(2026, 1, 1),
                EndDate = new DateTime(2026, 6, 30),
                StartPoint = 0
            }));
    }
}
