using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class OnbTemplatesAppService_Tests : RecruitmentAppServiceTestBase
{
    private OnbTemplatesAppService CreateAppService()
    {
        return CreateService(() => new OnbTemplatesAppService(FakeRepository.For<OnbTemplate>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_OnbTemplate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOnbTemplateDto
        {
            TemplateId = 1001,
            TemplateTitle = "Standard Onboarding"
        });

        var result = await service.GetAll(new GetAllOnbTemplatesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].TemplateTitle.ShouldBe("Standard Onboarding");
    }
}
