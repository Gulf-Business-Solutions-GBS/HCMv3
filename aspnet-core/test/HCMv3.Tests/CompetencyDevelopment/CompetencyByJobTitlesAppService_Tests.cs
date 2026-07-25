using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.CompetencyDevelopment;
using HCMv3.CompetencyDevelopment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.CompetencyDevelopment;

// ReSharper disable once InconsistentNaming
public class CompetencyByJobTitlesAppService_Tests : CompetencyDevelopmentAppServiceTestBase
{
    private CompetencyByJobTitlesAppService CreateAppService()
    {
        return CreateService(() => new CompetencyByJobTitlesAppService(FakeRepository.For<CompetencyByJobTitle>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CompetencyByJobTitle()
    {
        var jobTitleId = CreateJobTitle("Software Engineer");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCompetencyByJobTitleDto
        {
            JobTitleId = jobTitleId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });

        var result = await service.GetAll(new GetAllCompetencyByJobTitlesInput { JobTitleId = jobTitleId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].JobTitleName.ShouldBe("Software Engineer");
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Before_StartDate()
    {
        var jobTitleId = CreateJobTitle("QA Engineer");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditCompetencyByJobTitleDto
            {
                JobTitleId = jobTitleId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }
}
