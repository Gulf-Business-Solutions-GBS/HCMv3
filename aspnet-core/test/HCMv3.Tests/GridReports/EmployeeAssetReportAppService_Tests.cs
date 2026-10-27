using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Employees;
using HCMv3.GridReports.Employees.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Asset grid report (legacy <c>uscRptEmployeeAsset.ascx</c>, <c>Table1()</c>).
/// Modelled on the reference <see cref="EmployeeContractReportAppService_Tests"/>: each test pins one
/// legacy filter/projection semantic, and the boundary-date and "export ignores page size" tests are
/// kept because those are the two failure modes this whole report family shares.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAssetReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeAssetReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeAssetReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAssetReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Nationalities.Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
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
                Status = status
            }).Entity;
        });

        return employee;
    }

    private Asset CreateAsset(string name, string brand = "Brand", double value = 100)
    {
        Asset asset = null;
        UsingDbContext(context =>
        {
            var assetType = context.Set<AssetType>().Add(new AssetType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = name + "-Type",
                NameAr = name + "-TypeAr"
            }).Entity;

            context.SaveChanges();

            asset = context.Set<Asset>().Add(new Asset
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = name,
                NameAr = name + "-Ar",
                AssetTypeId = assetType.Id,
                Brand = brand,
                Model = "M1",
                Year = 2024,
                Color = "Red",
                VIN_Number = "VIN-" + name,
                Serial = "SER-" + name,
                PlateNumber = "PL-" + name,
                Barcode = "BC-" + name,
                Status = AssetStatus.Active,
                ExpiryDate = new DateTime(2030, 1, 1),
                Value = value
            }).Entity;
        });

        return asset;
    }

    private void CreateEmployeeAsset(Employee employee, Asset asset, DateTime start, DateTime end)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAsset>().Add(new EmployeeAsset
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                AssetId = asset.Id,
                StartDate = start,
                EndDate = end
            });
        });
    }

    private static GetEmployeeAssetReportInput Input(
        DateTime from,
        DateTime to,
        string employeeCode = null,
        EmployeeStatus? status = null,
        int maxResultCount = 100)
    {
        return new GetEmployeeAssetReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            EmployeeStatus = status,
            MaxResultCount = maxResultCount,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Assets_Overlapping_The_Requested_Window()
    {
        var asset = CreateAsset("OVL-ASSET");
        var inRange = CreateEmployee("AOVL-IN");
        var before = CreateEmployee("AOVL-BEFORE");
        var after = CreateEmployee("AOVL-AFTER");

        CreateEmployeeAsset(inRange, asset, new DateTime(2024, 6, 1), new DateTime(2024, 12, 31));
        CreateEmployeeAsset(before, asset, new DateTime(2023, 1, 1), new DateTime(2023, 6, 30));
        CreateEmployeeAsset(after, asset, new DateTime(2025, 6, 1), new DateTime(2025, 12, 31));

        var result = await _appService.GetEmployeeAssetReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("AOVL-IN");
        codes.ShouldNotContain("AOVL-BEFORE");
        codes.ShouldNotContain("AOVL-AFTER");
    }

    [Fact]
    public async Task Should_Include_Assets_Touching_The_Window_Boundary()
    {
        var asset = CreateAsset("EDGE-ASSET");
        var startsOnToDate = CreateEmployee("AEDGE-START");
        var endsOnFromDate = CreateEmployee("AEDGE-END");

        // Legacy predicate is NOT(StartDate > To || EndDate < From), so both boundary rows are included.
        CreateEmployeeAsset(startsOnToDate, asset, new DateTime(2024, 12, 31), new DateTime(2025, 12, 31));
        CreateEmployeeAsset(endsOnFromDate, asset, new DateTime(2023, 1, 1), new DateTime(2024, 1, 1));

        var result = await _appService.GetEmployeeAssetReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31)));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("AEDGE-START");
        codes.ShouldContain("AEDGE-END");
    }

    [Fact]
    public async Task Should_Filter_By_Employee_Status()
    {
        var asset = CreateAsset("ST-ASSET");
        var active = CreateEmployee("AST-ACTIVE");
        var inactive = CreateEmployee("AST-INACTIVE", EmployeeStatus.Inactive);

        CreateEmployeeAsset(active, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeAsset(inactive, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeAssetReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), status: EmployeeStatus.Inactive));

        var codes = result.Items.Select(i => i.EmployeeCode).ToList();
        codes.ShouldContain("AST-INACTIVE");
        codes.ShouldNotContain("AST-ACTIVE");
    }

    [Fact]
    public async Task Should_Filter_By_Comma_Separated_Employee_Codes()
    {
        var asset = CreateAsset("CODE-ASSET");
        var first = CreateEmployee("ACODE-A");
        var second = CreateEmployee("ACODE-B");
        var third = CreateEmployee("ACODE-C");

        CreateEmployeeAsset(first, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeAsset(second, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeAsset(third, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeAssetReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "ACODE-A , ACODE-C"));

        var codes = result.Items.Select(i => i.EmployeeCode).OrderBy(c => c).ToList();
        codes.ShouldBe(new List<string> { "ACODE-A", "ACODE-C" });
    }

    [Fact]
    public async Task Should_Project_The_Legacy_Asset_Columns()
    {
        var asset = CreateAsset("PROJ-ASSET", "Toyota", 1250);
        var employee = CreateEmployee("APROJ-1");

        CreateEmployeeAsset(employee, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        var result = await _appService.GetEmployeeAssetReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), employeeCode: "APROJ-1"));

        var row = result.Items.Single();
        row.EmployeeCode.ShouldBe("APROJ-1");
        row.EmployeeName.ShouldBe("Full APROJ-1");
        row.AssetName.ShouldBe("PROJ-ASSET");
        row.AssetTypeName.ShouldBe("PROJ-ASSET-Type");
        row.Brand.ShouldBe("Toyota");
        row.Model.ShouldBe("M1");
        // Legacy rendered Year through Year.ToString() into a string column.
        row.Year.ShouldBe("2024");
        row.VinNumber.ShouldBe("VIN-PROJ-ASSET");
        // Legacy "Status" is the ASSET status, not the employee's.
        row.AssetStatus.ShouldBe(AssetStatus.Active.ToString());
        row.Value.ShouldBe(1250);
        row.StartDate.ShouldBe(new DateTime(2024, 1, 1));
        row.EndDate.ShouldBe(new DateTime(2024, 12, 31));
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var asset = CreateAsset("EXP-ASSET");
        var first = CreateEmployee("AEXP-A");
        var second = CreateEmployee("AEXP-B");
        var third = CreateEmployee("AEXP-C");
        var excluded = CreateEmployee("AEXP-OUT");

        CreateEmployeeAsset(first, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeAsset(second, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeAsset(third, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));
        CreateEmployeeAsset(excluded, asset, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31));

        // Page size of 1 - the export must ignore it and still return every filtered row.
        var input = Input(
            new DateTime(2024, 1, 1),
            new DateTime(2024, 12, 31),
            employeeCode: "AEXP-A,AEXP-B,AEXP-C",
            maxResultCount: 1);

        var file = await _appService.GetEmployeeAssetReportToExcel(input);

        file.FileName.ShouldBe("EmployeeAsset.xlsx");
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        bytes.ShouldNotBeNull();

        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(3);
        var exportedCodes = exported.Select(r => r["Code"]?.ToString()).OrderBy(c => c).ToList();
        exportedCodes.ShouldBe(new List<string> { "AEXP-A", "AEXP-B", "AEXP-C" });
        // Legacy rendered SEVEN org-level columns, so the export must carry all seven headers.
        for (var level = 1; level <= 7; level++)
        {
            exported.First().Keys.ShouldContain("Org Level " + level);
        }
    }
}
