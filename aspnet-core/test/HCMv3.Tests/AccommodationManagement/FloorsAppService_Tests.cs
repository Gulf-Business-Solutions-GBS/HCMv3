using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class FloorsAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private FloorsAppService CreateAppService()
    {
        return CreateService(() => new FloorsAppService(FakeRepository.For<Floor>(Context)));
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
    public async Task Should_Create_Floor()
    {
        var buildingId = CreateBuilding("BLD1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditFloorDto
        {
            Code = "FLR1",
            NameEn = "Ground Floor",
            NameAr = "الطابق الأرضي",
            BuildingId = buildingId
        });

        var result = await service.GetAll(new GetAllFloorsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].BuildingName.ShouldBe("BLD1");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var buildingId = CreateBuilding("BLD2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditFloorDto
        {
            Code = "FLR2",
            NameEn = "Floor A",
            NameAr = "طابق أ",
            BuildingId = buildingId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditFloorDto
            {
                Code = "FLR2",
                NameEn = "Floor B",
                NameAr = "طابق ب",
                BuildingId = buildingId
            }));
    }
}
