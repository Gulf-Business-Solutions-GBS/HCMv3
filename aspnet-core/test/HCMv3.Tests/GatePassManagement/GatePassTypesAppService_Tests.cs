using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GatePassManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class GatePassTypesAppService_Tests : GatePassManagementAppServiceTestBase
{
    private GatePassTypesAppService CreateAppService()
    {
        return CreateService(() => new GatePassTypesAppService(FakeRepository.For<GatePassType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_GatePassType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto
        {
            NameEn = "Exit",
            NameAr = "خروج"
        });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Exit");
        result.Items[0].Code.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_NameEn_Duplicate()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Exit", NameAr = "خروج" });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Exit", NameAr = "Other" }));
    }
}
