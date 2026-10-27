using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GatePassManagement;
using HCMv3.GatePassManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GatePassManagement;

// ReSharper disable once InconsistentNaming
public class EmployeePoolCarsAppService_Tests : GatePassManagementAppServiceTestBase
{
    private EmployeePoolCarsAppService CreateAppService()
    {
        return CreateService(() => new EmployeePoolCarsAppService(FakeRepository.For<EmployeePoolCar>(Context)));
    }

    private int CreatePoolCar()
    {
        var (typeId, brandId, modelId) = CreateVehicleLookups();
        var entity = new PoolCar
        {
            TenantId = AbpSession.TenantId ?? 1,
            VehicleTypeId = typeId,
            VehiclePlateNumber = "POOL-1",
            VehicleDocExpiryDate = new DateTime(2025, 1, 1),
            VehicleBrandId = brandId,
            VehicleModelId = modelId
        };
        Context.PoolCars.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeePoolCar()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var poolCarId = CreatePoolCar();

        await service.CreateOrEdit(new CreateOrEditEmployeePoolCarDto
        {
            DriverEmployeeId = employeeId,
            PoolCarId = poolCarId,
            StartDate = new DateTime(2024, 1, 1),
            EndDate = new DateTime(2024, 1, 10)
        });

        var result = await service.GetAll(new GetAllEmployeePoolCarsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].DriverEmployeeId.ShouldBe(employeeId);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Before_StartDate()
    {
        var service = CreateAppService();
        var employeeId = CreateEmployee();
        var poolCarId = CreatePoolCar();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.CreateOrEdit(new CreateOrEditEmployeePoolCarDto
            {
                DriverEmployeeId = employeeId,
                PoolCarId = poolCarId,
                StartDate = new DateTime(2024, 1, 10),
                EndDate = new DateTime(2024, 1, 1)
            }));
    }
}
