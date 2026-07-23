using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class AccountInfosAppService_Tests : PayrollManagementAppServiceTestBase
{
    private AccountInfosAppService CreateAppService()
    {
        return CreateService(() => new AccountInfosAppService(FakeRepository.For<AccountInfo>(Context)));
    }

    private int CreateAccountType(string code)
    {
        var entity = new AccountType
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.AccountTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_AccountInfo()
    {
        var accountTypeId = CreateAccountType("AC1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAccountInfoDto
        {
            Code = "AI1",
            NameEn = "Main Account",
            NameAr = "الحساب الرئيسي",
            AccountTypeId = accountTypeId
        });

        var result = await service.GetAll(new GetAllAccountInfosInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].NameEn.ShouldBe("Main Account");
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_Code()
    {
        var accountTypeId = CreateAccountType("AC2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditAccountInfoDto
        {
            Code = "AI2",
            NameEn = "Account A",
            NameAr = "الحساب أ",
            AccountTypeId = accountTypeId
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAccountInfoDto
            {
                Code = "AI2",
                NameEn = "Account B",
                NameAr = "الحساب ب",
                AccountTypeId = accountTypeId
            }));
    }

    [Fact]
    public async Task Should_Throw_When_AccountType_Missing()
    {
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditAccountInfoDto
            {
                Code = "AI3",
                NameEn = "Account C",
                NameAr = "الحساب ج",
                AccountTypeId = 0
            }));
    }
}
