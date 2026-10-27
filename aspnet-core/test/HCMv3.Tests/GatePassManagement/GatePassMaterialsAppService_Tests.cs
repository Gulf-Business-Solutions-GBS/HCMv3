using System;
using System.Threading.Tasks;
using HCMv3.GatePassManagement;
using HCMv3.GatePassManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class GatePassMaterialsAppService_Tests : GatePassManagementAppServiceTestBase
{
    private GatePassMaterialsAppService CreateAppService()
    {
        return CreateService(() => new GatePassMaterialsAppService(FakeRepository.For<GatePassMaterial>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_GatePassMaterial()
    {
        var service = CreateAppService();
        var gatePassId = CreateGatePass();

        await service.CreateOrEdit(new CreateOrEditGatePassMaterialDto
        {
            GatePassId = gatePassId,
            ItemCode = "IC1",
            ItemName = "Laptop",
            Quantity = 2,
            ExpectedReturnDate = new DateTime(2024, 2, 1)
        });

        var result = await service.GetAll(new GetAllGatePassMaterialsInput { GatePassId = gatePassId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].ItemName.ShouldBe("Laptop");
    }
}
