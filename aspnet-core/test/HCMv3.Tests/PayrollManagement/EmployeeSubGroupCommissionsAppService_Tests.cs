using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupCommissionsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupCommissionsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupCommissionsAppService(FakeRepository.For<EmployeeSubGroupCommission>(Context)));
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
    public async Task Should_Create_EmployeeSubGroupCommission()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupCommissionDto
        {
            EmployeeSubGroupId = subGroupId,
            WageTypeId = 100
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupCommissionsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Duplicate_WageType()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupCommissionDto
        {
            EmployeeSubGroupId = subGroupId,
            WageTypeId = 200
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupCommissionDto
            {
                EmployeeSubGroupId = subGroupId,
                WageTypeId = 200
            }));
    }
}
