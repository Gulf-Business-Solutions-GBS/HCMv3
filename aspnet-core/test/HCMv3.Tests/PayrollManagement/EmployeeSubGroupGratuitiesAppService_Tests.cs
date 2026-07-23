using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupGratuitiesAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupGratuitiesAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupGratuitiesAppService(FakeRepository.For<EmployeeSubGroupGratuity>(Context)));
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
    public async Task Should_Create_EmployeeSubGroupGratuity()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupGratuityDto
        {
            EmployeeSubGroupId = subGroupId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31),
            ChangeYear = 1,
            GratuityDays = 21,
            BasisDays = 30
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupGratuitiesInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_EndDate_Before_StartDate()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG2");
        var service = CreateAppService();

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupGratuityDto
            {
                EmployeeSubGroupId = subGroupId,
                StartDate = new DateTime(2020, 12, 31),
                EndDate = new DateTime(2020, 1, 1),
                ChangeYear = 1
            }));
    }
}
