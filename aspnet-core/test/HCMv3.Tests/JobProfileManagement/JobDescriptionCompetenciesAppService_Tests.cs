using System.Threading.Tasks;
using Abp.UI;
using HCMv3.JobProfileManagement;
using HCMv3.JobProfileManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobDescriptionCompetenciesAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobDescriptionCompetenciesAppService CreateAppService()
    {
        return CreateService(() => new JobDescriptionCompetenciesAppService(FakeRepository.For<JobDescriptionCompetency>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobDescriptionCompetency()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var competencyId = CreateCompetency();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionCompetencyDto
        {
            JobDescriptionId = jobDescriptionId,
            CompetencyId = competencyId
        });

        var result = await service.GetAll(new GetAllJobDescriptionCompetenciesInput { JobDescriptionId = jobDescriptionId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].CompetencyNameEn.ShouldBe("Teamwork");
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Competency_For_Same_JobDescription()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var competencyId = CreateCompetency();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionCompetencyDto
        {
            JobDescriptionId = jobDescriptionId,
            CompetencyId = competencyId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditJobDescriptionCompetencyDto
            {
                JobDescriptionId = jobDescriptionId,
                CompetencyId = competencyId
            }));
    }
}
