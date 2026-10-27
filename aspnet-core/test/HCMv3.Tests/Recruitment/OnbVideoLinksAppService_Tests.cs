using System.Threading.Tasks;
using HCMv3.Recruitment;
using HCMv3.Recruitment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Recruitment;

// ReSharper disable once InconsistentNaming
public class OnbVideoLinksAppService_Tests : RecruitmentAppServiceTestBase
{
    private OnbVideoLinksAppService CreateAppService()
    {
        return CreateService(() => new OnbVideoLinksAppService(FakeRepository.For<OnbVideoLink>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_OnbVideoLink()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditOnbVideoLinkDto
        {
            NameEn = "Welcome Video",
            NameAr = "فيديو الترحيب",
            Link = "https://example.com/welcome",
            IsActive = true
        });

        var result = await service.GetAll(new GetAllOnbVideoLinksInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Welcome Video");
        result.Items[0].IsActive.ShouldBeTrue();
    }
}
