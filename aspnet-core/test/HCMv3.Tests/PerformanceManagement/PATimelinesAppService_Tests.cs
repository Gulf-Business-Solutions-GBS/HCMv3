using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class PATimelinesAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private PATimelinesAppService CreateAppService()
    {
        return CreateService(() => new PATimelinesAppService(FakeRepository.For<PATimeline>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Timeline()
    {
        var periodId = CreatePAPeriod("2026");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPATimelineDto
        {
            PAPeriodId = periodId,
            ActivityType = ActivityType.Plan,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 2, 1)
        });

        var result = await service.GetAll(new GetAllPATimelinesInput { PAPeriodId = periodId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].ActivityType.ShouldBe(ActivityType.Plan);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Before_StartDate()
    {
        var periodId = CreatePAPeriod("2026");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPATimelineDto
            {
                PAPeriodId = periodId,
                ActivityType = ActivityType.Plan,
                StartDate = new DateTime(2026, 2, 1),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Period_And_ActivityType()
    {
        var periodId = CreatePAPeriod("2026");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPATimelineDto
        {
            PAPeriodId = periodId,
            ActivityType = ActivityType.Plan,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 2, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditPATimelineDto
            {
                PAPeriodId = periodId,
                ActivityType = ActivityType.Plan,
                StartDate = new DateTime(2026, 3, 1),
                EndDate = new DateTime(2026, 4, 1)
            }));
    }

    [Fact]
    public async Task Should_Delete_Timeline()
    {
        var periodId = CreatePAPeriod("2026");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditPATimelineDto
        {
            PAPeriodId = periodId,
            ActivityType = ActivityType.Plan,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 2, 1)
        });

        var all = await service.GetAll(new GetAllPATimelinesInput { PAPeriodId = periodId });
        var id = all.Items[0].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(id));

        var afterDelete = await service.GetAll(new GetAllPATimelinesInput { PAPeriodId = periodId });
        afterDelete.TotalCount.ShouldBe(0);
    }
}
