using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.GovtRelationsManagement;

// ReSharper disable once InconsistentNaming
public class SponsorsAppService_Tests : GovtRelationsManagementAppServiceTestBase
{
    private SponsorsAppService CreateAppService()
    {
        return CreateService(() => new SponsorsAppService(FakeRepository.For<Sponsor>(Context)));
    }

    private int CreateNationality(string code)
    {
        var entity = new Nationality { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Nationalities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateBusinessType(string code)
    {
        var entity = new BusinessType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.BusinessTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateGovernorate(string code)
    {
        var entity = new Governorate { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Governorates.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private long CreateOrganizationUnit(string displayName)
    {
        var entity = new Abp.Organizations.OrganizationUnit(AbpSession.TenantId, displayName, null)
        {
            Code = displayName
        };
        Context.OrganizationUnits.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_Sponsor()
    {
        var nationalityId = CreateNationality("NAT1");
        var businessTypeId = CreateBusinessType("BT1");
        var governorateId = CreateGovernorate("GOV1");
        var organizationUnitId = CreateOrganizationUnit("Org1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSponsorDto
        {
            Code = "SP1",
            NameEn = "Al Sponsor Co",
            NameAr = "شركة الراعي",
            NationalityId = nationalityId,
            BusinessTypeId = businessTypeId,
            GovernorateId = governorateId,
            OrganizationUnitId = organizationUnitId,
            UnifiedNum = 123,
            SocialSecurityNum = 456,
            MOSALID = 789
        });

        var result = await service.GetAll(new GetAllSponsorsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Al Sponsor Co");
        result.Items[0].NationalityName.ShouldBe("NAT1");
        result.Items[0].OrganizationUnitId.ShouldBe(organizationUnitId);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var nationalityId = CreateNationality("NAT2");
        var businessTypeId = CreateBusinessType("BT2");
        var governorateId = CreateGovernorate("GOV2");
        var organizationUnitId = CreateOrganizationUnit("Org2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSponsorDto
        {
            Code = "SP2",
            NameEn = "Sponsor A",
            NameAr = "الراعي أ",
            NationalityId = nationalityId,
            BusinessTypeId = businessTypeId,
            GovernorateId = governorateId,
            OrganizationUnitId = organizationUnitId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSponsorDto
            {
                Code = "SP2",
                NameEn = "Sponsor B",
                NameAr = "الراعي ب",
                NationalityId = nationalityId,
                BusinessTypeId = businessTypeId,
                GovernorateId = governorateId,
                OrganizationUnitId = organizationUnitId
            }));
    }
}
