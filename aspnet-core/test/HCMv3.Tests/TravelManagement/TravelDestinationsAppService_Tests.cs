using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.TravelManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelDestinationsAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelDestinationsAppService CreateAppService()
    {
        return CreateService(() => new TravelDestinationsAppService(FakeRepository.For<TravelDestination>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TravelDestination()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Dubai",
            NameAr = "دبي"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Dubai");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "London", NameAr = "London" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "London", NameAr = "Other" }));
    }
}
