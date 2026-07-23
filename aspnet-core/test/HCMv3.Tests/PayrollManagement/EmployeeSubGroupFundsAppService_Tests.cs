using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupFundsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupFundsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupFundsAppService(FakeRepository.For<EmployeeSubGroupFund>(Context)));
    }

    private int CreateEmployeeSubGroup(string code)
    {
        var group = new EmployeeGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.EmployeeGroups.Add(group);
        Context.SaveChanges();

        var subGroup = new EmployeeSubGroup { TenantId = AbpSession.TenantId ?? 1, EmployeeGroupId = group.Id, Code = code, NameEn = code, NameAr = code };
        Context.EmployeeSubGroups.Add(subGroup);
        Context.SaveChanges();
        return subGroup.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeSubGroupFund()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupFundDto
        {
            EmployeeSubGroupId = subGroupId,
            FromPay = 0,
            ToPay = 1000,
            FundDedType = FundDedType.Percentage,
            Percentage = 5
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupFundsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_When_FromPay_Greater_Than_ToPay()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupFundDto
            {
                EmployeeSubGroupId = subGroupId,
                FromPay = 2000,
                ToPay = 1000
            }));
    }

    [Fact]
    public async Task Should_Throw_On_PayBand_Overlap()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG3");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupFundDto
        {
            EmployeeSubGroupId = subGroupId,
            FromPay = 0,
            ToPay = 1000
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupFundDto
            {
                EmployeeSubGroupId = subGroupId,
                FromPay = 500,
                ToPay = 1500
            }));
    }
}
