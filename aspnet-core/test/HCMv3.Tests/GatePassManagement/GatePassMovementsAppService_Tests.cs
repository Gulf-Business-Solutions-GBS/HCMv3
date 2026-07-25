using System.Threading.Tasks;
using HCMv3.GatePassManagement;
using HCMv3.GeneralMasterInfo.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class GatePassMovementsAppService_Tests : GatePassManagementAppServiceTestBase
{
    private GatePassMovementsAppService CreateAppService()
    {
        return CreateService(() => new GatePassMovementsAppService(FakeRepository.For<GatePassMovement>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_GatePassMovement()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSimpleLookupDto { NameEn = "In", NameAr = "داخل" });

        var result = await service.GetAll(new GetAllSimpleLookupsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("In");
    }
}
