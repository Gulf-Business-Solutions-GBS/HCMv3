using System.Threading.Tasks;
using Abp.UI;
using HCMv3.JobProfileManagement;
using HCMv3.JobProfileManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.JobProfileManagement;

// ReSharper disable once InconsistentNaming
public class JobDescriptionSkillsAppService_Tests : JobProfileManagementAppServiceTestBase
{
    private JobDescriptionSkillsAppService CreateAppService()
    {
        return CreateService(() => new JobDescriptionSkillsAppService(FakeRepository.For<JobDescriptionSkill>(Context)));
    }

    private int CreateSkill(string nameEn = "C#")
    {
        var entity = new Skill
        {
            TenantId = AbpSession.TenantId ?? 1,
            NameEn = nameEn,
            NameAr = nameEn
        };
        Context.Skills.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_JobDescriptionSkill()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var skillId = CreateSkill();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionSkillDto
        {
            JobDescriptionId = jobDescriptionId,
            SkillId = skillId
        });

        var result = await service.GetAll(new GetAllJobDescriptionSkillsInput { JobDescriptionId = jobDescriptionId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].SkillNameEn.ShouldBe("C#");
    }

    [Fact]
    public async Task Should_Throw_When_Duplicate_Skill_For_Same_JobDescription()
    {
        var service = CreateAppService();
        var jobDescriptionId = CreateJobDescription();
        var skillId = CreateSkill();

        await service.CreateOrEdit(new CreateOrEditJobDescriptionSkillDto
        {
            JobDescriptionId = jobDescriptionId,
            SkillId = skillId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditJobDescriptionSkillDto
            {
                JobDescriptionId = jobDescriptionId,
                SkillId = skillId
            }));
    }
}
