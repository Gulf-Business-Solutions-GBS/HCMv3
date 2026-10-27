using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class BedsAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private BedsAppService CreateAppService()
    {
        return CreateService(() => new BedsAppService(FakeRepository.For<Bed>(Context)));
    }

    private int CreateRoom(string code)
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

        var room = new Room
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            FlatId = flat.Id
        };
        Context.Rooms.Add(room);
        Context.SaveChanges();
        return room.Id;
    }

    [Fact]
    public async Task Should_Create_Bed()
    {
        var roomId = CreateRoom("RM1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditBedDto
        {
            Code = "BED1",
            NameEn = "Bed One",
            NameAr = "سرير واحد",
            RoomId = roomId
        });

        var result = await service.GetAll(new GetAllBedsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].RoomName.ShouldBe("RM1");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Name()
    {
        var roomId = CreateRoom("RM2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditBedDto
        {
            Code = "BED2",
            NameEn = "Duplicate Bed",
            NameAr = "سرير مكرر",
            RoomId = roomId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditBedDto
            {
                Code = "BED3",
                NameEn = "Duplicate Bed",
                NameAr = "سرير آخر",
                RoomId = roomId
            }));
    }
}
