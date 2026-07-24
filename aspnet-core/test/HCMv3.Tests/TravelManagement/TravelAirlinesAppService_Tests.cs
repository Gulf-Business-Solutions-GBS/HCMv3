using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.TravelManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelAirlinesAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelAirlinesAppService CreateAppService()
    {
        return CreateService(() => new TravelAirlinesAppService(FakeRepository.For<TravelAirline>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TravelAirline()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Emirates",
            NameAr = "طيران الإمارات"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Emirates");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Qatar Airways", NameAr = "Qatar Airways" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Qatar Airways", NameAr = "Other" }));
    }
}
