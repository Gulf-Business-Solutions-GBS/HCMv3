using System;
using System.Threading.Tasks;
using HCMv3.GatePassManagement;
using HCMv3.GatePassManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class PoolCarsAppService_Tests : GatePassManagementAppServiceTestBase
{
    private PoolCarsAppService CreateAppService()
    {
        return CreateService(() => new PoolCarsAppService(FakeRepository.For<PoolCar>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_PoolCar()
    {
        var service = CreateAppService();
        var (typeId, brandId, modelId) = CreateVehicleLookups();

        await service.CreateOrEdit(new CreateOrEditPoolCarDto
        {
            VehicleTypeId = typeId,
            VehiclePlateNumber = "POOL-1",
            VehicleDocExpiryDate = new DateTime(2025, 1, 1),
            VehicleBrandId = brandId,
            VehicleModelId = modelId
        });

        var result = await service.GetAll(new GetAllPoolCarsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].VehiclePlateNumber.ShouldBe("POOL-1");
    }
}
