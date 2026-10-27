using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class FlatsAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private FlatsAppService CreateAppService()
    {
        return CreateService(() => new FlatsAppService(FakeRepository.For<Flat>(Context)));
    }

    private int CreateFloor(string code)
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

        var floor = new Floor
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            BuildingId = building.Id
        };
        Context.Floors.Add(floor);
        Context.SaveChanges();
        return floor.Id;
    }

    [Fact]
    public async Task Should_Create_Flat()
    {
        var floorId = CreateFloor("FLR1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditFlatDto
        {
            Code = "FLT1",
            NameEn = "Flat One",
            NameAr = "شقة واحد",
            FloorId = floorId
        });

        var result = await service.GetAll(new GetAllFlatsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].FloorName.ShouldBe("FLR1");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var floorId = CreateFloor("FLR2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditFlatDto
        {
            Code = "FLT2",
            NameEn = "Duplicate Flat",
            NameAr = "شقة مكررة",
            FloorId = floorId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditFlatDto
            {
                Code = "FLT3",
                NameEn = "Duplicate Flat",
                NameAr = "شقة أخرى",
                FloorId = floorId
            }));
    }
}
