using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GatePassManagement;
using HCMv3.GatePassManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class GatePassVehiclesAppService_Tests : GatePassManagementAppServiceTestBase
{
    private GatePassVehiclesAppService CreateAppService()
    {
        return CreateService(() => new GatePassVehiclesAppService(FakeRepository.For<GatePassVehicle>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_GatePassVehicle()
    {
        var service = CreateAppService();
        var gatePassId = CreateGatePass();
        var (typeId, brandId, modelId) = CreateVehicleLookups();

        await service.CreateOrEdit(new CreateOrEditGatePassVehicleDto
        {
            GatePassId = gatePassId,
            VehicleTypeId = typeId,
            VehiclePlateNumber = "ABC-123",
            VehicleDocExpiryDate = new DateTime(2025, 1, 1),
            VehicleBrandId = brandId,
            VehicleModelId = modelId
        });

        var result = await service.GetAll(new GetAllGatePassVehiclesInput { GatePassId = gatePassId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].VehiclePlateNumber.ShouldBe("ABC-123");
    }

    [Fact]
    public async Task Should_Throw_When_PlateNumber_Duplicate_In_Same_GatePass()
    {
        var service = CreateAppService();
        var gatePassId = CreateGatePass();
        var (typeId, brandId, modelId) = CreateVehicleLookups();

        await service.CreateOrEdit(new CreateOrEditGatePassVehicleDto
        {
            GatePassId = gatePassId,
            VehicleTypeId = typeId,
            VehiclePlateNumber = "ABC-123",
            VehicleDocExpiryDate = new DateTime(2025, 1, 1),
            VehicleBrandId = brandId,
            VehicleModelId = modelId
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditGatePassVehicleDto
            {
                GatePassId = gatePassId,
                VehicleTypeId = typeId,
                VehiclePlateNumber = "ABC-123",
                VehicleDocExpiryDate = new DateTime(2025, 2, 1),
                VehicleBrandId = brandId,
                VehicleModelId = modelId
            }));
    }
}
