using System.Threading.Tasks;
using HCMv3.GatePassManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class VehicleModelsAppService_Tests : GatePassManagementAppServiceTestBase
{
    private VehicleModelsAppService CreateAppService()
    {
        return CreateService(() => new VehicleModelsAppService(FakeRepository.For<VehicleModel>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_VehicleModel()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Corolla", NameAr = "كورولا" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Corolla");
    }
}
