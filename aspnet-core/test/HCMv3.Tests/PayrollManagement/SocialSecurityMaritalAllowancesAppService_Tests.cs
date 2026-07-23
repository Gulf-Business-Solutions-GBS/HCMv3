using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class SocialSecurityMaritalAllowancesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private SocialSecurityMaritalAllowancesAppService CreateAppService()
    {
        return CreateService(() => new SocialSecurityMaritalAllowancesAppService(FakeRepository.For<SocialSecurityMaritalAllowance>(Context)));
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

    private (int educationBranchId, int maritalStatusId) CreateLookups(string code)
    {
        var educationBranch = new HCMv3.GeneralMasterInfo.EducationBranch { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.EducationBranches.Add(educationBranch);

        var maritalStatus = new HCMv3.GeneralMasterInfo.MaritalStatus { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.MaritalStatuses.Add(maritalStatus);

        Context.SaveChanges();
        return (educationBranch.Id, maritalStatus.Id);
    }

    [Fact]
    public async Task Should_Create_SocialSecurityMaritalAllowance()
    {
        var socialSecurityId = CreateSocialSecurity("SS1");
        var (educationBranchId, maritalStatusId) = CreateLookups("L1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSocialSecurityMaritalAllowanceDto
        {
            SocialSecurityId = socialSecurityId,
            EducationBranchId = educationBranchId,
            MaritalStatusId = maritalStatusId,
            Amount = 250
        });

        var result = await service.GetAll(new GetAllSocialSecurityMaritalAllowancesInput { SocialSecurityId = socialSecurityId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Combination()
    {
        var socialSecurityId = CreateSocialSecurity("SS2");
        var (educationBranchId, maritalStatusId) = CreateLookups("L2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSocialSecurityMaritalAllowanceDto
        {
            SocialSecurityId = socialSecurityId,
            EducationBranchId = educationBranchId,
            MaritalStatusId = maritalStatusId,
            Amount = 100
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditSocialSecurityMaritalAllowanceDto
            {
                SocialSecurityId = socialSecurityId,
                EducationBranchId = educationBranchId,
                MaritalStatusId = maritalStatusId,
                Amount = 200
            }));
    }
}
