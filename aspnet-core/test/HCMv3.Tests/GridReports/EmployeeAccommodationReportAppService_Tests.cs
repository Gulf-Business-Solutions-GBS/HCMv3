using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.AccommodationManagement;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.AccommodationReport;
using HCMv3.GridReports.AccommodationReport.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class EmployeeAccommodationReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeAccommodationReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeAccommodationReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAccommodationReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
    {
        Employee employee = null;
        UsingDbContext(context =>
        {
            var nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            context.SaveChanges();

            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = "F" + code,
                LastNameEn = "L",
                FirstNameAr = "ف",
                LastNameAr = "ل",
                FullNameEn = "Full " + code,
                FullNameAr = "عربي " + code,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id,
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private (AccommodationType type, Location location, Building building, Floor floor, Flat flat, Room room, Bed bed) CreateAccommodationLookups()
    {
        AccommodationType type = null;
        Location location = null;
        Building building = null;
        Floor floor = null;
        Flat flat = null;
        Room room = null;
        Bed bed = null;

        UsingDbContext(context =>
        {
            type = context.Set<AccommodationType>().Add(new AccommodationType
            { TenantId = TenantId, Code = "AT1", NameEn = "Type1", NameAr = "Type1" }).Entity;
            location = context.Set<Location>().Add(new Location
            { TenantId = TenantId, Code = "LOC1", NameEn = "Location1", NameAr = "Location1" }).Entity;
            context.SaveChanges();

            building = context.Set<Building>().Add(new Building
            { TenantId = TenantId, Code = "BLD1", NameEn = "Building1", NameAr = "Building1", LocationId = location.Id }).Entity;
            context.SaveChanges();

            floor = context.Set<Floor>().Add(new Floor
            { TenantId = TenantId, Code = "FLR1", NameEn = "Floor1", NameAr = "Floor1", BuildingId = building.Id }).Entity;
            context.SaveChanges();

            flat = context.Set<Flat>().Add(new Flat
            { TenantId = TenantId, Code = "FLT1", NameEn = "Flat1", NameAr = "Flat1", FloorId = floor.Id }).Entity;
            context.SaveChanges();

            room = context.Set<Room>().Add(new Room
            { TenantId = TenantId, Code = "RM1", NameEn = "Room1", NameAr = "Room1", FlatId = flat.Id }).Entity;
            context.SaveChanges();

            bed = context.Set<Bed>().Add(new Bed
            { TenantId = TenantId, Code = "BED1", NameEn = "Bed1", NameAr = "Bed1", RoomId = room.Id }).Entity;
        });

        return (type, location, building, floor, flat, room, bed);
    }

    private EmployeeAccommodation CreateAccommodation(Employee employee, DateTime start, DateTime end)
    {
        var lookups = CreateAccommodationLookups();

        EmployeeAccommodation accommodation = null;
        UsingDbContext(context =>
        {
            accommodation = context.Set<EmployeeAccommodation>().Add(new EmployeeAccommodation
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                StartDate = start,
                EndDate = end,
                AccommodationTypeId = lookups.type.Id,
                LocationId = lookups.location.Id,
                BuildingId = lookups.building.Id,
                FloorId = lookups.floor.Id,
                FlatId = lookups.flat.Id,
                RoomId = lookups.room.Id,
                BedId = lookups.bed.Id,
                DeductionAmount = 0
            }).Entity;
        });

        return accommodation;
    }

    private static GetEmployeeAccommodationReportInput Input(
        DateTime from, DateTime to, string employeeCode = null, int maxResultCount = 100)
    {
        return new GetEmployeeAccommodationReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            MaxResultCount = maxResultCount
        };
    }

    [Fact]
    public async Task Should_Return_Only_Accommodations_Overlapping_The_Requested_Window()
    {
        var inRange = CreateEmployee("ACC-IN");
        var before = CreateEmployee("ACC-BEFORE");

        CreateAccommodation(inRange, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateAccommodation(before, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));

        var result = await _appService.GetEmployeeAccommodationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("ACC-IN");
        codes.ShouldNotContain("ACC-BEFORE");
    }

    [Fact]
    public async Task Should_Reproduce_The_Legacy_Left_Join_Producing_One_Row_When_No_Services_Exist()
    {
        var employee = CreateEmployee("ACC-NOSVC");
        CreateAccommodation(employee, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeAccommodationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "ACC-NOSVC"));

        result.Items.Count.ShouldBe(1);
        result.Items.Single().ServiceTypeName.ShouldBe(string.Empty);
        result.Items.Single().ServiceStartDate.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var first = CreateEmployee("ACC-A");
        var second = CreateEmployee("ACC-B");
        var excluded = CreateEmployee("ACC-C");

        CreateAccommodation(first, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateAccommodation(second, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateAccommodation(excluded, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeAccommodationReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "ACC-A,ACC-B"));

        var codes = result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "ACC-A", "ACC-B" });
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var first = CreateEmployee("ACC-EXP-A");
        var second = CreateEmployee("ACC-EXP-B");

        CreateAccommodation(first, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateAccommodation(second, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var input = Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "ACC-EXP-A,ACC-EXP-B", maxResultCount: 1);

        var file = await _appService.GetEmployeeAccommodationReportToExcel(input);

        file.FileName.ShouldBe("EmployeeAccommodation.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
        exported.First().Keys.ShouldContain("Accommodation Type");
    }
}
