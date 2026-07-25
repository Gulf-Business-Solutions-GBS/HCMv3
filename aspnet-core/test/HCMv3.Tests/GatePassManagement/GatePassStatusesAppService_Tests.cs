using System.Threading.Tasks;
using HCMv3.GatePassManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class GatePassStatusesAppService_Tests : GatePassManagementAppServiceTestBase
{
    private GatePassStatusesAppService CreateAppService()
    {
        return CreateService(() => new GatePassStatusesAppService(FakeRepository.For<GatePassStatus>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_GatePassStatus()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Open", NameAr = "مفتوح" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Open");
    }
}
