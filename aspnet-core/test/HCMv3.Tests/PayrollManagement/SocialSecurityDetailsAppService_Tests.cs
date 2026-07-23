using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class SocialSecurityDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SocialSecurityDetailsAppService CreateAppService()
    {
        return CreateService(() => new SocialSecurityDetailsAppService(FakeRepository.For<SocialSecurityDetail>(Context)));
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

    [Fact]
    public async Task Should_Create_SocialSecurityDetail()
    {
        var socialSecurityId = CreateSocialSecurity("SS1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSocialSecurityDetailDto
        {
            SocialSecurityId = socialSecurityId,
            MinEarnings = 0,
            MaxEarnings = 1000,
            EmployeeContribution = 5,
            EmployerContribution = 10,
            MinEarningsEmployeeContribution = 1
        });

        var result = await service.GetAll(new GetAllSocialSecurityDetailsInput { SocialSecurityId = socialSecurityId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_MaxEarnings_Less_Than_MinEarnings()
    {
        var socialSecurityId = CreateSocialSecurity("SS2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSocialSecurityDetailDto
            {
                SocialSecurityId = socialSecurityId,
                MinEarnings = 1000,
                MaxEarnings = 500
            }));
    }

    [Fact]
    public async Task Should_Throw_On_Overlapping_Earnings_Tier()
    {
        var socialSecurityId = CreateSocialSecurity("SS3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSocialSecurityDetailDto
        {
            SocialSecurityId = socialSecurityId,
            MinEarnings = 0,
            MaxEarnings = 1000
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSocialSecurityDetailDto
            {
                SocialSecurityId = socialSecurityId,
                MinEarnings = 500,
                MaxEarnings = 1500
            }));
    }
}
