using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Abp.Organizations;
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
public class SponsorJobTitleQuotaReportAppService_Tests : AppTestBase
{
    private readonly ISponsorJobTitleQuotaReportAppService _appService;
    private readonly ITempFileCacheManager _tempFileCacheManager;

    public SponsorJobTitleQuotaReportAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _appService = Resolve<ISponsorJobTitleQuotaReportAppService>();
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

        OrganizationUnit organizationUnit = null;
        UsingDbContext(context =>
        {
            organizationUnit = context.OrganizationUnits.Add(new OrganizationUnit(TenantId, "Sponsor OU")
            {
                Code = Guid.NewGuid().ToString("N").Substring(0, 8)
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
                OrganizationUnitId = organizationUnit.Id,
                Telephone = 1
            }).Entity;
        });
        return sponsor;
    }

    private GovtJobTitle CreateGovtJobTitle(string code)
    {
        GovtJobCategory category = null;
        UsingDbContext(context =>
        {
            category = context.Set<GovtJobCategory>().Add(new GovtJobCategory
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Cat",
                NameAr = "Cat"
            }).Entity;
        });

        GovtJobTitle title = null;
        UsingDbContext(context =>
        {
            title = context.Set<GovtJobTitle>().Add(new GovtJobTitle
            {
                TenantId = TenantId,
                Code = code,
                NameEn = code + "-En",
                NameAr = code + "-Ar",
                GovtJobCategoryId = category.Id
            }).Entity;
        });
        return title;
    }

    private void CreateQuota(Sponsor sponsor, GovtJobTitle title, int citizenAccrued, int citizenUsed)
    {
        UsingDbContext(context =>
        {
            context.Set<SponsorJobTitle>().Add(new SponsorJobTitle
            {
                TenantId = TenantId,
                SponsorId = sponsor.Id,
                GovtJobTitleId = title.Id,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31),
                NoofPosition = 10,
                CitizenPercent = 50,
                CitizenQuotaAccrued = citizenAccrued,
                CitizenQuotaUsed = citizenUsed,
                NonCitizenPercent = 50,
                NonCitizenQuotaAccrued = 5,
                NonCitizenQuotaUsed = 2
            });
        });
    }

    [Fact]
    public async Task Should_Filter_By_Sponsor_Code()
    {
        var sponsorA = CreateSponsor("SJQ-A");
        var sponsorB = CreateSponsor("SJQ-B");
        var title = CreateGovtJobTitle("SJQ-T");
        CreateQuota(sponsorA, title, 5, 2);
        CreateQuota(sponsorB, title, 5, 2);

        var result = await _appService.GetSponsorJobTitleQuotaReport(new GetSponsorJobTitleQuotaReportInput
        {
            SponsorCode = "SJQ-A",
            MaxResultCount = 100
        });

        var names = result.Items.Select(i => i.SponsorName).ToList();
        names.ShouldContain("SJQ-A-En");
        names.ShouldNotContain("SJQ-B-En");
    }

    /// <summary>Balance = Accrued - Used, computed here since it is DTO-only in this port.</summary>
    [Fact]
    public async Task Should_Compute_Citizen_Quota_Balance_As_Accrued_Minus_Used()
    {
        var sponsor = CreateSponsor("SJQ-BAL");
        var title = CreateGovtJobTitle("SJQ-BAL-T");
        CreateQuota(sponsor, title, citizenAccrued: 8, citizenUsed: 3);

        var result = await _appService.GetSponsorJobTitleQuotaReport(new GetSponsorJobTitleQuotaReportInput
        {
            SponsorCode = "SJQ-BAL",
            MaxResultCount = 100
        });

        result.Items.Single().CitizenQuotaBalance.ShouldBe(5);
    }

    [Fact]
    public async Task Should_Resolve_Every_Export_Header_To_Localized_Text_Not_A_Missing_Key_Placeholder()
    {
        var sponsor = CreateSponsor("SJQ-HDR");
        var title = CreateGovtJobTitle("SJQ-HDR-T");
        CreateQuota(sponsor, title, 5, 2);

        var file = await _appService.GetSponsorJobTitleQuotaReportToExcel(new GetSponsorJobTitleQuotaReportInput
        {
            MaxResultCount = 100
        });

        var bytes = _tempFileCacheManager.GetFile(file.FileToken);
        using var stream = new MemoryStream(bytes);
        var exported = stream.Query(useHeaderRow: true).Cast<IDictionary<string, object>>().ToList();

        foreach (var header in exported.First().Keys)
        {
            header.ShouldNotStartWith("[");
        }
    }
}
