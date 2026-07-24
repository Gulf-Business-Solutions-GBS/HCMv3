using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement;

// ReSharper disable once InconsistentNaming
public class KpiByJobTitlesAppService_Tests : PerformanceManagementAppServiceTestBase
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private KpiByJobTitlesAppService CreateAppService()
    {
        return CreateService(() => new KpiByJobTitlesAppService(
            FakeRepository.For<KpiByJobTitle>(Context),
            FakeRepository.For<KpiByJobTitleDetail>(Context)));
    }

    [Fact]
    public async Task Should_Create_First_Record_With_Infinite_EndDate()
    {
        var jobTitleId = CreateJobTitle("JT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDto
        {
            JobTitleId = jobTitleId,
            StartDate = new DateTime(2026, 1, 1)
        });

        var result = await service.GetAll(new GetAllKpiByJobTitlesInput { JobTitleId = jobTitleId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].EndDate.ShouldBe(InfiniteEndDate);
        result.Items[0].TotalWeight.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Chain_EndDates_When_Second_Record_Added()
    {
        var jobTitleId = CreateJobTitle("JT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDto { JobTitleId = jobTitleId, StartDate = new DateTime(2026, 1, 1) });
        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDto { JobTitleId = jobTitleId, StartDate = new DateTime(2026, 6, 1) });

        var result = await service.GetAll(new GetAllKpiByJobTitlesInput { JobTitleId = jobTitleId });
        result.TotalCount.ShouldBe(2);

        var first = result.Items[0]; // ordered by StartDate ASC
        var second = result.Items[1];

        first.StartDate.ShouldBe(new DateTime(2026, 1, 1));
        first.EndDate.ShouldBe(new DateTime(2026, 5, 31));

        second.StartDate.ShouldBe(new DateTime(2026, 6, 1));
        second.EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Rechain_EndDates_After_Delete()
    {
        var jobTitleId = CreateJobTitle("JT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDto { JobTitleId = jobTitleId, StartDate = new DateTime(2026, 1, 1) });
        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDto { JobTitleId = jobTitleId, StartDate = new DateTime(2026, 6, 1) });

        var beforeDelete = await service.GetAll(new GetAllKpiByJobTitlesInput { JobTitleId = jobTitleId });
        var secondId = beforeDelete.Items[1].Id;

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(secondId));

        var afterDelete = await service.GetAll(new GetAllKpiByJobTitlesInput { JobTitleId = jobTitleId });
        afterDelete.TotalCount.ShouldBe(1);
        afterDelete.Items[0].EndDate.ShouldBe(InfiniteEndDate);
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_StartDate_For_Same_JobTitle()
    {
        var jobTitleId = CreateJobTitle("JT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditKpiByJobTitleDto { JobTitleId = jobTitleId, StartDate = new DateTime(2026, 1, 1) });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditKpiByJobTitleDto { JobTitleId = jobTitleId, StartDate = new DateTime(2026, 1, 1) }));
    }
}
