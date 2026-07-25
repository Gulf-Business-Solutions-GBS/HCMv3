using System.Threading.Tasks;
using HCMv3.GatePassManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class VehicleBrandsAppService_Tests : GatePassManagementAppServiceTestBase
{
    private VehicleBrandsAppService CreateAppService()
    {
        return CreateService(() => new VehicleBrandsAppService(FakeRepository.For<VehicleBrand>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_VehicleBrand()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "Toyota", NameAr = "تويوتا" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Toyota");
    }
}
