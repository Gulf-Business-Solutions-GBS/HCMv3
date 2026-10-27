using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class CVLanguagesAppService_Tests : RecruitmentAppServiceTestBase
{
    private CVLanguagesAppService CreateAppService()
    {
        return CreateService(() => new CVLanguagesAppService(FakeRepository.For<CVLanguage>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CVLanguage()
    {
        var applicantCVId = CreateApplicantCV();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCVLanguageDto
        {
            ApplicantCVId = applicantCVId,
            Proficiency = ProficiencyType.Intermediate
        });

        var result = await service.GetAll(new GetAllCVLanguagesInput { ApplicantCVId = applicantCVId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].Proficiency.ShouldBe(ProficiencyType.Intermediate);
    }
}
