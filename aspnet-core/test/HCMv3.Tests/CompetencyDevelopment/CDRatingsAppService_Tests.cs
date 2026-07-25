using System.Threading.Tasks;
using Abp.UI;
using HCMv3.CompetencyDevelopment;
using HCMv3.CompetencyDevelopment.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.CompetencyDevelopment;

// ReSharper disable once InconsistentNaming
public class CDRatingsAppService_Tests : CompetencyDevelopmentAppServiceTestBase
{
    private CDRatingsAppService CreateAppService()
    {
        return CreateService(() => new CDRatingsAppService(FakeRepository.For<CDRating>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_CDRating()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCDRatingDto { NameEn = "Exceeds Expectations", NameAr = "يفوق التوقعات" });

        var result = await service.GetAll(new GetAllCDRatingsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Exceeds Expectations");
    }

    [Fact]
    public async Task Should_Throw_When_Names_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditCDRatingDto { NameEn = "Meets", NameAr = "A" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditCDRatingDto { NameEn = "Meets", NameAr = "B" }));
    }
}
