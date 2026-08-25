using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GridReports.MasterReport;
using HCMv3.GridReports.MasterReport.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

/// <summary>
/// Tests for the Asset grid report (legacy <c>uscRptAsset.ascx</c>, <c>Table1()</c>). This screen has no
/// employee dimension and legacy applies no row-level security (its override is entirely commented out).
/// </summary>
// ReSharper disable once InconsistentNaming
public class AssetReportAppService_Tests : AppTestBase
{
    private readonly IAssetReportAppService _appService;

    public AssetReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<IAssetReportAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Asset CreateAsset(string name, AssetStatus status = AssetStatus.Active, string assetTypeCode = null)
    {
        Asset asset = null;
        UsingDbContext(context =>
        {
            var typeCode = assetTypeCode ?? Guid.NewGuid().ToString("N").Substring(0, 8);
            var assetType = context.Set<AssetType>().FirstOrDefault(t => t.Code == typeCode && t.TenantId == TenantId);
            if (assetType == null)
            {
                assetType = context.Set<AssetType>().Add(new AssetType
                {
                    TenantId = TenantId,
                    Code = typeCode,
                    NameEn = typeCode + "-Type",
                    NameAr = typeCode + "-TypeAr"
                }).Entity;
                context.SaveChanges();
            }

            asset = context.Set<Asset>().Add(new Asset
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = name,
                NameAr = name + "-Ar",
                AssetTypeId = assetType.Id,
                Brand = "Brand",
                Model = "M1",
                Year = 2024,
                Color = "Red",
                VIN_Number = "VIN-" + name,
                Serial = "SER-" + name,
                PlateNumber = "PL-" + name,
                Barcode = "BC-" + name,
                Status = status,
                AssetAllocationStatus = AssetAllocationStatus.Unoccupied,
                ExpiryDate = new DateTime(2030, 1, 1),
                Value = 1000
            }).Entity;
        });

        return asset;
    }

    private static GetAssetReportInput Input(AssetStatus? status = null, string assetTypeCode = null)
    {
        return new GetAssetReportInput
        {
            AssetStatus = status,
            AssetTypeCode = assetTypeCode,
            MaxResultCount = 100,
            SkipCount = 0
        };
    }

    [Fact]
    public async Task Should_Filter_By_Asset_Status()
    {
        CreateAsset("AST-ACTIVE", AssetStatus.Active);
        CreateAsset("AST-INACTIVE", AssetStatus.Inactive);

        var result = await _appService.GetAssetReport(Input(status: AssetStatus.Inactive));

        var names = result.Items.Select(i => i.NameEn).ToList();
        names.ShouldContain("AST-INACTIVE");
        names.ShouldNotContain("AST-ACTIVE");
    }

    [Fact]
    public async Task Should_Render_Year_As_A_String_Column()
    {
        CreateAsset("AST-YEAR");

        var result = await _appService.GetAssetReport(Input());

        result.Items.Single(i => i.NameEn == "AST-YEAR").Year.ShouldBe("2024");
    }
}
