using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class SponsorJobTitlesAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private SponsorJobTitlesAppService CreateAppService()
    {
        return CreateService(() => new SponsorJobTitlesAppService(FakeRepository.For<SponsorJobTitle>(Context)));
    }

    private int CreateSponsor(string code)
    {
        var nationality = new Nationality { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Nationalities.Add(nationality);
        var businessType = new BusinessType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.BusinessTypes.Add(businessType);
        var governorate = new Governorate { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Governorates.Add(governorate);
        var orgUnit = new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, code, null) { Code = code };
        Context.OrganizationUnits.Add(orgUnit);
        Context.SaveChanges();

        var sponsor = new Sponsor
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            NationalityId = nationality.Id,
            BusinessTypeId = businessType.Id,
            GovernorateId = governorate.Id,
            OrganizationUnitId = orgUnit.Id
        };
        Context.Sponsors.Add(sponsor);
        Context.SaveChanges();
        return sponsor.Id;
    }

    private int CreateGovtJobTitle(string code)
    {
        var category = new GovtJobCategory { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.GovtJobCategories.Add(category);
        Context.SaveChanges();

        var jobTitle = new GovtJobTitle
        {
            TenantId = AbpSession.TenantId ?? 1,
            GovtJobCategoryId = category.Id,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.GovtJobTitles.Add(jobTitle);
        Context.SaveChanges();
        return jobTitle.Id;
    }

    [Fact]
    public async Task Should_Compute_Quota_Accrued_On_Create()
    {
        var sponsorId = CreateSponsor("SP1");
        var jobTitleId = CreateGovtJobTitle("JT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSponsorJobTitleDto
        {
            SponsorId = sponsorId,
            GovtJobTitleId = jobTitleId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            NoofPosition = 10,
            CitizenPercent = 30,
            NonCitizenPercent = 70
        });

        var result = await service.GetAll(new GetAllSponsorJobTitlesInput { SponsorId = sponsorId });
        result.TotalCount.ShouldBe(1);
        // floor(10 * 30 / 100) = 3
        result.Items[0].CitizenQuotaAccrued.ShouldBe(3);
        // ceiling(10 * 70 / 100) = 7
        result.Items[0].NonCitizenQuotaAccrued.ShouldBe(7);
        result.Items[0].CitizenQuotaBalance.ShouldBe(3);
        result.Items[0].NonCitizenQuotaBalance.ShouldBe(7);
    }

    [Fact]
    public async Task Should_Throw_When_Percent_Does_Not_Equal_100()
    {
        var sponsorId = CreateSponsor("SP2");
        var jobTitleId = CreateGovtJobTitle("JT2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSponsorJobTitleDto
            {
                SponsorId = sponsorId,
                GovtJobTitleId = jobTitleId,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 12, 31),
                NoofPosition = 10,
                CitizenPercent = 30,
                NonCitizenPercent = 60
            }));
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Earlier_Than_StartDate()
    {
        var sponsorId = CreateSponsor("SP3");
        var jobTitleId = CreateGovtJobTitle("JT3");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSponsorJobTitleDto
            {
                SponsorId = sponsorId,
                GovtJobTitleId = jobTitleId,
                StartDate = new DateTime(2025, 12, 31),
                EndDate = new DateTime(2025, 1, 1),
                NoofPosition = 10,
                CitizenPercent = 50,
                NonCitizenPercent = 50
            }));
    }
}
