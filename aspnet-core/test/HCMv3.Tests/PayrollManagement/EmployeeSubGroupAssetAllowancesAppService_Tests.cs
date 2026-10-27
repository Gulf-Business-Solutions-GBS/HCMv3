using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupAssetAllowancesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupAssetAllowancesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupAssetAllowancesAppService(FakeRepository.For<EmployeeSubGroupAssetAllowance>(Context)));
    }

    private int CreateEmployeeSubGroup(string code)
    {
        var group = new EmployeeGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.EmployeeGroups.Add(group);
        Context.SaveChanges();

        var subGroup = new EmployeeSubGroup
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeGroupId = group.Id,
            Code = code,
            NameEn = code,
            NameAr = code
        };
        Context.EmployeeSubGroups.Add(subGroup);
        Context.SaveChanges();
        return subGroup.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeSubGroupAssetAllowance()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAssetAllowanceDto
        {
            EmployeeSubGroupId = subGroupId,
            WageTypeId = 100,
            Amount = 50
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupAssetAllowancesInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(50);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_WageType()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAssetAllowanceDto
        {
            EmployeeSubGroupId = subGroupId,
            WageTypeId = 200,
            Amount = 10
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupAssetAllowanceDto
            {
                EmployeeSubGroupId = subGroupId,
                WageTypeId = 200,
                Amount = 20
            }));
    }
}
