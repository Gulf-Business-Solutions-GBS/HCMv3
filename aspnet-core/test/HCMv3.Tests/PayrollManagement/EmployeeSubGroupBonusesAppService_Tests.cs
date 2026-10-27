using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupBonusesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupBonusesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupBonusesAppService(FakeRepository.For<EmployeeSubGroupBonus>(Context)));
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
    public async Task Should_Create_EmployeeSubGroupBonus()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupBonusDto
        {
            EmployeeSubGroupId = subGroupId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31),
            ChangeYear = 1,
            Percentage = 10
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupBonusesInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Overlap()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupBonusDto
        {
            EmployeeSubGroupId = subGroupId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31),
            ChangeYear = 1,
            Percentage = 10
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupBonusDto
            {
                EmployeeSubGroupId = subGroupId,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2021, 6, 1),
                ChangeYear = 1,
                Percentage = 20
            }));
    }
}
