using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class SocialSecuritiesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SocialSecuritiesAppService CreateAppService()
    {
        return CreateService(() => new SocialSecuritiesAppService(FakeRepository.For<SocialSecurity>(Context)));
    }

    private int CreateNationality(string code)
    {
        var entity = new HCMv3.GeneralMasterInfo.Nationality { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.Nationalities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_SocialSecurity()
    {
        var nationalityId = CreateNationality("N1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSocialSecurityDto
        {
            NationalityId = nationalityId,
            EarningsCap = 5000,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            ChildAllowance = 100,
            SpecialNeedChildAllowance = 200
        });

        var result = await service.GetAll(new GetAllSocialSecuritiesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NationalityId.ShouldBe(nationalityId);
    }

    [Fact]
    public async Task Should_Throw_When_EndDate_Before_StartDate()
    {
        var nationalityId = CreateNationality("N2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSocialSecurityDto
            {
                NationalityId = nationalityId,
                StartDate = new DateTime(2025, 12, 31),
                EndDate = new DateTime(2025, 1, 1)
            }));
    }
}
