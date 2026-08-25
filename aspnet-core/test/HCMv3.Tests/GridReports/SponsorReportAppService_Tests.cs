using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GridReports.GovtRelations;
using HCMv3.GridReports.GovtRelations.Dto;
using HCMv3.Storage;
using MiniExcelLibs;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GridReports;

// ReSharper disable once InconsistentNaming
public class SponsorReportAppService_Tests : AppTestBase
{
    private readonly ISponsorReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public SponsorReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISponsorReportAppService>();
        _tempFileCacheManager = Resolve<ITempFileCacheManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private Sponsor CreateSponsor(string code)
    {
        Nationality nationality = null;
        BusinessType businessType = null;
        Governorate governorate = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
            businessType = context.Set<BusinessType>().Add(new BusinessType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "BT",
                NameAr = "BT"
            }).Entity;
            governorate = context.Set<Governorate>().Add(new Governorate
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "GOV",
                NameAr = "GOV"
            }).Entity;
        });

        Sponsor sponsor = null;
        UsingDbContext(context =>
        {
            sponsor = context.Set<Sponsor>().Add(new Sponsor
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                NationalityId = nationality.Id,
                BusinessTypeId = businessType.Id,
                GovernorateId = governorate.Id,
                Telephone = 12345678,
                UnifiedNum = 111,
                SocialSecurityNum = 222,
                MOSALID = 333
            }).Entity;
        });
        return sponsor;
    }

    [Fact]
    public async Task Should_Return_All_Sponsors_Unfiltered()
    {
        CreateSponsor("SPN-A");
        CreateSponsor("SPN-B");

        var result = await _appService.GetSponsorReport(new GetSponsorReportInput { MaxResultCount = 100 });

        var codes = result.Items.Select(i => i.SponsorCode).ToList();
        codes.ShouldContain("SPN-A");
        codes.ShouldContain("SPN-B");
    }

    /// <summary>
    /// LEGACY DEFECT FLAGGED FOR REVIEW pinning test: legacy's "Code" column value is the Oid, not the
    /// Sponsor's business code -- reproduced faithfully. See the row DTO remarks.
    /// </summary>
    [Fact]
    public async Task Should_Reproduce_The_Legacy_Code_Column_Holding_The_Surrogate_Id_Not_The_Business_Code()
    {
        var sponsor = CreateSponsor("SPN-DEFECT");

        var result = await _appService.GetSponsorReport(new GetSponsorReportInput { MaxResultCount = 100 });

        var row = result.Items.Single(i => i.SponsorCode == "SPN-DEFECT");
        row.Code.ShouldBe(sponsor.Id);
    }

    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        CreateSponsor("SPN-HDR");

        var file = await _appService.GetSponsorReportToExcel(new GetSponsorReportInput { MaxResultCount = 100 });

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
