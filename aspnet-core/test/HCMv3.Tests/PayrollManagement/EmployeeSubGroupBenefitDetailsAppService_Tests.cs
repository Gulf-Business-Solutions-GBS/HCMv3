using System;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeSubGroupBenefitDetailsAppService_Tests : PayrollManagementAppServiceTestBase
{
    private EmployeeSubGroupBenefitDetailsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeSubGroupBenefitDetailsAppService(FakeRepository.For<EmployeeSubGroupBenefitDetail>(Context)));
    }

    private int CreateEmployeeSubGroupBenefit(string code)
    {
        var group = new EmployeeGroup { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.EmployeeGroups.Add(group);
        Context.SaveChanges();

        var subGroup = new EmployeeSubGroup { TenantId = AbpSession.TenantId ?? 1, EmployeeGroupId = group.Id, Code = code, NameEn = code, NameAr = code };
        Context.EmployeeSubGroups.Add(subGroup);
        Context.SaveChanges();

        var benefitType = new BenefitType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.BenefitTypes.Add(benefitType);
        Context.SaveChanges();

        var benefit = new EmployeeSubGroupBenefit { TenantId = AbpSession.TenantId ?? 1, EmployeeSubGroupId = subGroup.Id, BenefitTypeId = benefitType.Id };
        Context.EmployeeSubGroupBenefits.Add(benefit);
        Context.SaveChanges();
        return benefit.Id;
    }

    private int CreateDependantType(string code)
    {
        var entity = new DependantType { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code };
        Context.DependantTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_EmployeeSubGroupBenefitDetail()
    {
        var benefitId = CreateEmployeeSubGroupBenefit("ESGB1");
        var dependantTypeId = CreateDependantType("DT1");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupBenefitDetailDto
        {
            EmployeeSubGroupBenefitId = benefitId,
            DependantTypeId = dependantTypeId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31),
            Amount = 100
        });

        var result = await service.GetAll(new GetAllEmployeeSubGroupBenefitDetailsInput());
        result.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Throw_On_Overlap()
    {
        var benefitId = CreateEmployeeSubGroupBenefit("ESGB2");
        var dependantTypeId = CreateDependantType("DT2");
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupBenefitDetailDto
        {
            EmployeeSubGroupBenefitId = benefitId,
            DependantTypeId = dependantTypeId,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2020, 12, 31),
            Amount = 100
        });

        await Should.ThrowAsync<UserFriendlyException>(async () =>
            await service.CreateOrEdit(new CreateOrEditEmployeeSubGroupBenefitDetailDto
            {
                EmployeeSubGroupBenefitId = benefitId,
                DependantTypeId = dependantTypeId,
                StartDate = new DateTime(2020, 6, 1),
                EndDate = new DateTime(2021, 6, 1),
                Amount = 200
            }));
    }
}
