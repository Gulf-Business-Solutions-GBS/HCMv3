using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.Transaction;
using HCMv3.GridReports.Transaction.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Employee Asset Invoice grid report (legacy <c>uscRptEmployeeAssetInvoice.ascx</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeAssetInvoiceReportAppService_Tests : AppTestBase
{
    private readonly IEmployeeAssetInvoiceReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public EmployeeAssetInvoiceReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IEmployeeAssetInvoiceReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Employee CreateEmployee(string code)
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
                Status = EmployeeStatus.Active
            }).Entity;
        });

        return employee;
    }

    private Asset CreateAsset(string code)
    {
        Asset asset = null;
        UsingDbContext(context =>
        {
            var assetType = context.Set<AssetType>().Add(new AssetType
            {
                TenantId = TenantId,
                Code = code + "-TYPE",
                NameEn = code + "-Type",
                NameAr = code + "-Type"
            }).Entity;

            context.SaveChanges();

            asset = context.Set<Asset>().Add(new Asset
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code,
                NameAr = code,
                AssetTypeId = assetType.Id
            }).Entity;
        });

        return asset;
    }

    private void CreateInvoice(Employee employee, Asset asset, DateTime invoiceDate, double amount)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeAssetInvoice>().Add(new EmployeeAssetInvoice
            {
                TenantId = TenantId,
                EmployeeId = employee.Id,
                AssetId = asset.Id,
                InvoiceDate = invoiceDate,
                InvoiceAmount = amount
            });
        });
    }

    private static GetEmployeeAssetInvoiceReportInput Input(DateTime from, DateTime to, string employeeCode = null)
    {
        return new GetEmployeeAssetInvoiceReportInput
        {
            FromDate = from,
            ToDate = to,
            EmployeeCode = employeeCode,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Return_Only_Invoices_Within_The_Date_Range()
    {
        var employee = CreateEmployee("ASI-1");
        var asset = CreateAsset("ASI-ASSET-1");

        CreateInvoice(employee, asset, new DateTime(2024, 6, 1), 100);
        CreateInvoice(employee, asset, new DateTime(2023, 1, 1), 200);

        var result = await _appService.GetEmployeeAssetInvoiceReport(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ASI-1"));

        result.Items.Count.ShouldBe(1);
        result.Items.Single().InvoiceAmount.ShouldBe(100);
    }

    [Fact]
    public async Task Should_Export_All_Matching_Rows_Not_Just_The_Current_Page()
    {
        var employee = CreateEmployee("ASI-2");
        var asset = CreateAsset("ASI-ASSET-2");

        CreateInvoice(employee, asset, new DateTime(2024, 3, 1), 50);
        CreateInvoice(employee, asset, new DateTime(2024, 4, 1), 60);

        var input = Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ASI-2");
        input.MaxResultCount = 1;

        var file = await _appService.GetEmployeeAssetInvoiceReportToExcel(input);
        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        exported.Count.ShouldBe(2);
    }

    /// <summary>Every exported header must resolve to real text, not the "[KeyName]" missing-key fallback.</summary>
    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var employee = CreateEmployee("ASI-3");
        var asset = CreateAsset("ASI-ASSET-3");
        CreateInvoice(employee, asset, new DateTime(2024, 5, 1), 75);

        var file = await _appService.GetEmployeeAssetInvoiceReportToExcel(
            Input(new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), "ASI-3"));

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
