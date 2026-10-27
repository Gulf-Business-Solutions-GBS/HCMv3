using System.Threading.Tasks;
using Abp.UI;
using HCMv3.JobProfileManagement;
using HCMv3.JobProfileManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobDescriptionResponsibilitiesAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobDescriptionResponsibilitiesAppService CreateAppService()
    {
        return CreateService(() => new JobDescriptionResponsibilitiesAppService(FakeRepository.For<JobDescriptionResponsibility>(Context)));
    }

    private int CreateJobResponsibility(string nameEn = "Lead team")
    {
        var entity = new JobResponsibility
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.JobResponsibilities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobDescriptionResponsibility()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var responsibilityId = CreateJobResponsibility();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionResponsibilityDto
        {
            JobDescriptionId = jobDescriptionId,
            JobResponsibilityId = responsibilityId
        });

        var result = await service.GetAll(new GetAllJobDescriptionResponsibilitiesInput { JobDescriptionId = jobDescriptionId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].JobResponsibilityNameEn.ShouldBe("Lead team");
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Responsibility_For_Same_JobDescription()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var responsibilityId = CreateJobResponsibility();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionResponsibilityDto
        {
            JobDescriptionId = jobDescriptionId,
            JobResponsibilityId = responsibilityId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditJobDescriptionResponsibilityDto
            {
                JobDescriptionId = jobDescriptionId,
                JobResponsibilityId = responsibilityId
            }));
    }
}
