using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class BuildingContractsAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private BuildingContractsAppService CreateAppService()
    {
        return CreateService(() => new BuildingContractsAppService(FakeRepository.For<BuildingContract>(Context)));
    }

    private int CreateBuilding(string code)
    {
        var location = new Location { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Locations.Add(location);
        Context.SaveChanges();

        var building = new Building
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            LocationId = location.Id
        };
        Context.Buildings.Add(building);
        Context.SaveChanges();
        return building.Id;
    }

    [Fact]
    public async Task Should_Create_BuildingContract()
    {
        var buildingId = CreateBuilding("BLD1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditBuildingContractDto
        {
            BuildingId = buildingId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31)
        });

        var result = await service.GetAll(new GetAllBuildingContractsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var buildingId = CreateBuilding("BLD2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditBuildingContractDto
            {
                BuildingId = buildingId,
                StartDate = new DateTime(2026, 12, 31),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }
}
