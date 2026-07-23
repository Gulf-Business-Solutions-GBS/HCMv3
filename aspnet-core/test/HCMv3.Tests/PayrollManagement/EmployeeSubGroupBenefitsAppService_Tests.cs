using System.Threading.Tasks;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupBenefitsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupBenefitsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupBenefitsAppService(FakeRepository.For<EmployeeSubGroupBenefit>(Context)));
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

    private int CreateBenefitType(string code)
    {
        var entity = new BenefitType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.BenefitTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeSubGroupBenefit()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG1");
        var benefitTypeId = CreateBenefitType("BT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupBenefitDto
        {
            EmployeeSubGroupId = subGroupId,
            BenefitTypeId = benefitTypeId,
            MaxAmount = 500
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupBenefitsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].MaxAmount.ShouldBe(500);
    }

    [Fact]
    public async Task Should_Update_EmployeeSubGroupBenefit()
    {
        var subGroupId = CreateEmployeeSubGroup("ESG2");
        var benefitTypeId = CreateBenefitType("BT2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupBenefitDto
        {
            EmployeeSubGroupId = subGroupId,
            BenefitTypeId = benefitTypeId,
            MaxAmount = 100
        });

        var created = await service.GetAll(new GetAllEmployeeSubGroupBenefitsInput());
        var id = created.Items[0].Id;

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupBenefitDto
        {
            Id = id,
            EmployeeSubGroupId = subGroupId,
            BenefitTypeId = benefitTypeId,
            MaxAmount = 999
        });

        var updated = await service.GetEmployeeSubGroupBenefitForEdit(new Abp.Application.Services.Dto.EntityDto(id));
        updated.EmployeeSubGroupBenefit.MaxAmount.ShouldBe(999);
    }
}
