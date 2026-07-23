using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class WageAccGroupDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private WageAccGroupDetailsAppService CreateAppService()
    {
        return CreateService(() => new WageAccGroupDetailsAppService(FakeRepository.For<WageAccGroupDetail>(Context)));
    }

    private int CreateWageAccGroup(string code)
    {
        var entity = new WageAccGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.WageAccGroups.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateWageType(string code)
    {
        var entity = new WageType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, PayrollOrder = 1 };
        Context.WageTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateAccountInfo(string code)
    {
        var accountType = new AccountType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.AccountTypes.Add(accountType);
        Context.SaveChanges();

        var accountInfo = new AccountInfo
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            AccountTypeId = accountType.Id
        };
        Context.AccountInfos.Add(accountInfo);
        Context.SaveChanges();
        return accountInfo.Id;
    }

    [Fact]
    public async Task Should_Create_WageAccGroupDetail()
    {
        var groupId = CreateWageAccGroup("WAGD1");
        var wageTypeId = CreateWageType("WT_WAGD1");
        var accountInfoId = CreateAccountInfo("AI_WAGD1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageAccGroupDetailDto
        {
            WageAccGroupId = groupId,
            WageTypeId = wageTypeId,
            AccountInfoId = accountInfoId,
            CostPercentage = 100
        });

        var result = await service.GetAll(new GetAllWageAccGroupDetailsInput { WageAccGroupId = groupId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Combination()
    {
        var groupId = CreateWageAccGroup("WAGD2");
        var wageTypeId = CreateWageType("WT_WAGD2");
        var accountInfoId = CreateAccountInfo("AI_WAGD2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageAccGroupDetailDto
        {
            WageAccGroupId = groupId,
            WageTypeId = wageTypeId,
            AccountInfoId = accountInfoId,
            CostPercentage = 100
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditWageAccGroupDetailDto
            {
                WageAccGroupId = groupId,
                WageTypeId = wageTypeId,
                AccountInfoId = accountInfoId,
                CostPercentage = 50
            }));
    }
}
