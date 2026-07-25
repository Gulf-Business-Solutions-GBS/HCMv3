using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class CVSkillsAppService_Tests : RecruitmentAppServiceTestBase
{
    private CVSkillsAppService CreateAppService()
    {
        return CreateService(() => new CVSkillsAppService(FakeRepository.For<CVSkill>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CVSkill()
    {
        var applicantCVId = CreateApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCVSkillDto
        {
            ApplicantCVId = applicantCVId,
            Skill = "C#",
            Proficiency = ProficiencyType.Advanced
        });

        var result = await service.GetAll(new GetAllCVSkillsInput { ApplicantCVId = applicantCVId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Skill.ShouldBe("C#");
        result.Items[0].Proficiency.ShouldBe(ProficiencyType.Advanced);
    }
}
