using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.TravelManagement;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.TravelManagement;

// ReSharper disable once InconsistentNaming
public class TravelCancelReasonsAppService_Tests : TravelManagementAppServiceTestBase
{
    private TravelCancelReasonsAppService CreateAppService()
    {
        return CreateService(() => new TravelCancelReasonsAppService(FakeRepository.For<TravelCancelReason>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_TravelCancelReason()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Sickness",
            NameAr = "مرض"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Sickness");
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Emergency", NameAr = "Emergency" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Emergency", NameAr = "Other" }));
    }
}
