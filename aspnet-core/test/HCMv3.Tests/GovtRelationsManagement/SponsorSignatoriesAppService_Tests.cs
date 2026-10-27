using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class SponsorSignatoriesAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private SponsorSignatoriesAppService CreateAppService()
    {
        return CreateService(() => new SponsorSignatoriesAppService(FakeRepository.For<SponsorSignatory>(Context)));
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

    private int CreateSignatory(string code)
    {
        var entity = new Signatory { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, CivilId = 1 };
        Context.Signatories.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_SponsorSignatory()
    {
        var sponsorId = CreateSponsor("SP1");
        var signatoryId = CreateSignatory("SIG1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSponsorSignatoryDto
        {
            SponsorId = sponsorId,
            SignatoryId = signatoryId
        });

        var result = await service.GetAll(new GetAllSponsorSignatoriesInput { SponsorId = sponsorId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Signatory_For_Sponsor()
    {
        var sponsorId = CreateSponsor("SP2");
        var signatoryId = CreateSignatory("SIG2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSponsorSignatoryDto
        {
            SponsorId = sponsorId,
            SignatoryId = signatoryId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSponsorSignatoryDto
            {
                SponsorId = sponsorId,
                SignatoryId = signatoryId
            }));
    }
}
