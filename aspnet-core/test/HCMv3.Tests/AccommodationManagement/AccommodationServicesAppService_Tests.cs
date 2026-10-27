using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.AccommodationManagement;
using HCMv3.AccommodationManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.AccommodationManagement;

// ReSharper disable once InconsistentNaming
public class AccommodationServicesAppService_Tests : AccommodationManagementAppServiceTestBase
{
    private AccommodationServicesAppService CreateAppService()
    {
        return CreateService(() => new AccommodationServicesAppService(FakeRepository.For<AccommodationServices>(Context)));
    }

    private int CreateEmployeeAccommodation(string code)
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

        var employeeAccommodation = new EmployeeAccommodation
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employee.Id,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 12, 31),
            AccommodationTypeId = accommodationType.Id,
            LocationId = location.Id,
            BuildingId = building.Id,
            FloorId = floor.Id,
            FlatId = flat.Id,
            RoomId = room.Id,
            BedId = bed.Id
        };
        Context.EmployeeAccommodations.Add(employeeAccommodation);
        Context.SaveChanges();
        return employeeAccommodation.Id;
    }

    private int CreateAccommodationServiceType(string code)
    {
        var entity = new AccommodationServiceType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.AccommodationServiceTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_AccommodationServices()
    {
        var employeeAccommodationId = CreateEmployeeAccommodation("EA1");
        var serviceTypeId = CreateAccommodationServiceType("AST1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAccommodationServicesDto
        {
            EmployeeAccommodationId = employeeAccommodationId,
            AccommodationServiceTypeId = serviceTypeId,
            StartDate = new DateTime(2026, 1, 1),
            EndDate = new DateTime(2026, 3, 31)
        });

        var result = await service.GetAll(new GetAllAccommodationServicesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].AccommodationServiceTypeName.ShouldBe("AST1");
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var employeeAccommodationId = CreateEmployeeAccommodation("EA2");
        var serviceTypeId = CreateAccommodationServiceType("AST2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAccommodationServicesDto
            {
                EmployeeAccommodationId = employeeAccommodationId,
                AccommodationServiceTypeId = serviceTypeId,
                StartDate = new DateTime(2026, 3, 31),
                EndDate = new DateTime(2026, 1, 1)
            }));
    }
}
