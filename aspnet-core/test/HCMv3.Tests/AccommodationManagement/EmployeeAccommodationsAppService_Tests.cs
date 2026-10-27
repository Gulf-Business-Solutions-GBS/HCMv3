using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeAccommodationsAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private EmployeeAccommodationsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeAccommodationsAppService(FakeRepository.For<EmployeeAccommodation>(Context)));
    }

    private int CreateEmployee(string code)
    {
        var nationality = new HCMv3.GeneralMasterInfo.Nationality
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var employee = new HCMv3.PersonnelAdministration.Employee
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            FirstNameEn = code,
            LastNameEn = code,
            FirstNameAr = code,
            LastNameAr = code,
            NationalityId = nationality.Id
        };
        Context.Employees.Add(employee);
        Context.SaveChanges();
        return employee.Id;
    }

    private (int locationId, int buildingId, int floorId, int flatId, int roomId, int bedId, int accommodationTypeId) CreateFullChain(string code)
    {
        var location = new Location { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Locations.Add(location);
        Context.SaveChanges();

        var building = new Building { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, LocationId = location.Id };
        Context.Buildings.Add(building);
        Context.SaveChanges();

        var floor = new Floor { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, BuildingId = building.Id };
        Context.Floors.Add(floor);
        Context.SaveChanges();

        var flat = new Flat { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, FloorId = floor.Id };
        Context.Flats.Add(flat);
        Context.SaveChanges();

        var room = new Room { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, FlatId = flat.Id };
        Context.Rooms.Add(room);
        Context.SaveChanges();

        var bed = new Bed { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, RoomId = room.Id };
        Context.Beds.Add(bed);
        Context.SaveChanges();

        var accommodationType = new AccommodationType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.AccommodationTypes.Add(accommodationType);
        Context.SaveChanges();

        return (location.Id, building.Id, floor.Id, flat.Id, room.Id, bed.Id, accommodationType.Id);
    }

    [Fact]
    public async Task Should_Create_EmployeeAccommodation()
    {
        var employeeId = CreateEmployee("EMP1");
        var chain = CreateFullChain("C1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeAccommodationDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            AccommodationTypeId = chain.accommodationTypeId,
            DeductionAmount = 150,
            LocationId = chain.locationId,
            BuildingId = chain.buildingId,
            FloorId = chain.floorId,
            FlatId = chain.flatId,
            RoomId = chain.roomId,
            BedId = chain.bedId
        });

        var result = await service.GetAll(new GetAllEmployeeAccommodationsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].DeductionAmount.ShouldBe(150);
        result.Items[0].EmployeeCode.ShouldBe("EMP1");
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var employeeId = CreateEmployee("EMP2");
        var chain = CreateFullChain("C2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeAccommodationDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 30),
                EndDate = new DateTime(2026, 1, 1),
                AccommodationTypeId = chain.accommodationTypeId,
                LocationId = chain.locationId,
                BuildingId = chain.buildingId,
                FloorId = chain.floorId,
                FlatId = chain.flatId,
                RoomId = chain.roomId,
                BedId = chain.bedId
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Date_Range_For_Same_Employee()
    {
        var employeeId = CreateEmployee("EMP3");
        var chain = CreateFullChain("C3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeAccommodationDto
        {
            EmployeeId = employeeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 6, 30),
            AccommodationTypeId = chain.accommodationTypeId,
            LocationId = chain.locationId,
            BuildingId = chain.buildingId,
            FloorId = chain.floorId,
            FlatId = chain.flatId,
            RoomId = chain.roomId,
            BedId = chain.bedId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeAccommodationDto
            {
                EmployeeId = employeeId,
                StartDate = new DateTime(2026, 6, 1),
                EndDate = new DateTime(2026, 12, 31),
                AccommodationTypeId = chain.accommodationTypeId,
                LocationId = chain.locationId,
                BuildingId = chain.buildingId,
                FloorId = chain.floorId,
                FlatId = chain.flatId,
                RoomId = chain.roomId,
                BedId = chain.bedId
            }));
    }
}
