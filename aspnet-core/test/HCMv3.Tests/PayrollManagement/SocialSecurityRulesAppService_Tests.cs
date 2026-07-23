using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class SocialSecurityRulesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SocialSecurityRulesAppService CreateAppService()
    {
        return CreateService(() => new SocialSecurityRulesAppService(FakeRepository.For<SocialSecurityRule>(Context)));
    }

    private int CreateSocialSecurity(string code)
    {
        var nationality = new HCMv3.GeneralMasterInfo.Nationality { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Nationalities.Add(nationality);
        Context.SaveChanges();

        var entity = new SocialSecurity
        {
            TenantId = AbpSession.TenantId ?? 1,
            NationalityId = nationality.Id,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31)
        };
        Context.SocialSecurities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateQualification(string code)
    {
        var entity = new Qualification { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Qualifications.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_SocialSecurityRule()
    {
        var socialSecurityId = CreateSocialSecurity("SS1");
        var qualificationId = CreateQualification("Q1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSocialSecurityRuleDto
        {
            SocialSecurityId = socialSecurityId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            QualificationId = qualificationId,
            Gender = Gender.Male,
            Dependant = 1,
            Disability = false,
            Amount = 150
        });

        var result = await service.GetAll(new GetAllSocialSecurityRulesInput { SocialSecurityId = socialSecurityId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Before_StartDate()
    {
        var socialSecurityId = CreateSocialSecurity("SS2");
        var qualificationId = CreateQualification("Q2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSocialSecurityRuleDto
            {
                SocialSecurityId = socialSecurityId,
                StartDate = new DateTime(2025, 12, 31),
                EndDate = new DateTime(2025, 1, 1),
                QualificationId = qualificationId
            }));
    }

    [Fact]
    public async Task Should_Allow_Overlapping_Date_Ranges()
    {
        // Unlike SocialSecurityDetail, the legacy has NO overlap check for SocialSecurityRule.
        var socialSecurityId = CreateSocialSecurity("SS3");
        var qualificationId = CreateQualification("Q3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSocialSecurityRuleDto
        {
            SocialSecurityId = socialSecurityId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            QualificationId = qualificationId
        });

        await service.CreateOrEdit(new CreateOrEditSocialSecurityRuleDto
        {
            SocialSecurityId = socialSecurityId,
            StartDate = new DateTime(2025, 6, 1),
            EndDate = new DateTime(2025, 12, 31),
            QualificationId = qualificationId
        });

        var result = await service.GetAll(new GetAllSocialSecurityRulesInput { SocialSecurityId = socialSecurityId });
        result.TotalCount.ShouldBe(2);
    }
}
