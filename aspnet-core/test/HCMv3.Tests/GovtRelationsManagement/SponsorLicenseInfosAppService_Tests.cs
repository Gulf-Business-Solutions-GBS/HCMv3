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
public class SponsorLicenseInfosAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private SponsorLicenseInfosAppService CreateAppService()
    {
        return CreateService(() => new SponsorLicenseInfosAppService(FakeRepository.For<SponsorLicenseInfo>(Context)));
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

    [Fact]
    public async Task Should_Create_SponsorLicenseInfo()
    {
        var sponsorId = CreateSponsor("SP1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSponsorLicenseInfoDto
        {
            SponsorId = sponsorId,
            TradeLicenseNum = "TL1",
            LicenseIssueDate = new DateTime(2025, 1, 1),
            LicenseExpiryDate = new DateTime(2026, 1, 1),
            CivilId = 123
        });

        var result = await service.GetAll(new GetAllSponsorLicenseInfosInput { SponsorId = sponsorId });
        result.TotalCount.ShouldBe(1);
        result.Items[0].TradeLicenseNum.ShouldBe("TL1");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_TradeLicenseNum_For_Sponsor()
    {
        var sponsorId = CreateSponsor("SP2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSponsorLicenseInfoDto
        {
            SponsorId = sponsorId,
            TradeLicenseNum = "TL2",
            LicenseIssueDate = new DateTime(2025, 1, 1),
            LicenseExpiryDate = new DateTime(2026, 1, 1)
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSponsorLicenseInfoDto
            {
                SponsorId = sponsorId,
                TradeLicenseNum = "TL2",
                LicenseIssueDate = new DateTime(2025, 2, 1),
                LicenseExpiryDate = new DateTime(2026, 2, 1)
            }));
    }
}
