using System.Threading.Tasks;
using HCMv3.GatePassManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class VehicleTypesAppService_Tests : GatePassManagementAppServiceTestBase
{
    private VehicleTypesAppService CreateAppService()
    {
        return CreateService(() => new VehicleTypesAppService(FakeRepository.For<VehicleType>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_VehicleType()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Sedan", NameAr = "سيدان" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Sedan");
    }
}
