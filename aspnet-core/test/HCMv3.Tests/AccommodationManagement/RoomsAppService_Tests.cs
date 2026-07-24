using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class RoomsAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private RoomsAppService CreateAppService()
    {
        return CreateService(() => new RoomsAppService(FakeRepository.For<Room>(Context)));
    }

    private int CreateFlat(string code)
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

        var flat = new Flat
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            FloorId = floor.Id
        };
        Context.Flats.Add(flat);
        Context.SaveChanges();
        return flat.Id;
    }

    [Fact]
    public async Task Should_Create_Room()
    {
        var flatId = CreateFlat("FLT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRoomDto
        {
            Code = "RM1",
            NameEn = "Room One",
            NameAr = "غرفة واحد",
            FlatId = flatId
        });

        var result = await service.GetAll(new GetAllRoomsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].FlatName.ShouldBe("FLT1");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var flatId = CreateFlat("FLT2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRoomDto
        {
            Code = "RM2",
            NameEn = "Room A",
            NameAr = "غرفة أ",
            FlatId = flatId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditRoomDto
            {
                Code = "RM2",
                NameEn = "Room B",
                NameAr = "غرفة ب",
                FlatId = flatId
            }));
    }
}
