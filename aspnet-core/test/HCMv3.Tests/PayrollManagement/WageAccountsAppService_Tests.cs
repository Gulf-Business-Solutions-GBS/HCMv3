using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class WageAccountsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private WageAccountsAppService CreateAppService()
    {
        return CreateService(() => new WageAccountsAppService(FakeRepository.For<WageAccount>(Context)));
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
    public async Task Should_Create_WageAccount()
    {
        var wageTypeId = CreateWageType("WT_WA1");
        var accountInfoId = CreateAccountInfo("AI_WA1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageAccountDto
        {
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(9999, 12, 31),
            AccountInfoId = accountInfoId,
            CostPercentage = 100
        });

        var result = await service.GetAll(new GetAllWageAccountsInput { WageTypeId = wageTypeId });
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Overlap_For_Same_WageType()
    {
        var wageTypeId = CreateWageType("WT_WA2");
        var accountInfoId = CreateAccountInfo("AI_WA2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditWageAccountDto
        {
            WageTypeId = wageTypeId,
            StartDate = new DateTime(2025, 1, 1),
            EndDate = new DateTime(2025, 12, 31),
            AccountInfoId = accountInfoId,
            CostPercentage = 100
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditWageAccountDto
            {
                WageTypeId = wageTypeId,
                StartDate = new DateTime(2025, 6, 1),
                EndDate = new DateTime(2026, 6, 1),
                AccountInfoId = accountInfoId,
                CostPercentage = 100
            }));
    }
}
