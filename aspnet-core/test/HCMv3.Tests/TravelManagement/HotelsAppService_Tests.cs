using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.TravelManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class HotelsAppService_Tests : TravelManagementAppServiceTestBase
{
    private HotelsAppService CreateAppService()
    {
        return CreateService(() => new HotelsAppService(FakeRepository.For<Hotel>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_Hotel()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Hilton",
            NameAr = "هيلتون"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Hilton");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Marriott", NameAr = "Marriott" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Marriott", NameAr = "Other" }));
    }
}
