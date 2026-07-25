using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.SuccessionManagement;
using HCMv3.SuccessionManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.SuccessionManagement;

// ReSharper disable once InconsistentNaming
public class EmployeeTalentFlagsAppService_Tests : SuccessionManagementAppServiceTestBase
{
    private EmployeeTalentFlagsAppService CreateAppService()
    {
        return CreateService(() => new EmployeeTalentFlagsAppService(FakeRepository.For<EmployeeTalentFlag>(Context)));
    }

    private int CreateRiskOfLoss()
    {
        var entity = new RiskOfLoss { TenantId = AbpSession.TenantId ?? 1, NameEn = "High", NameAr = "عالي" };
        Context.RiskOfLosses.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateImpactOfLoss()
    {
        var entity = new ImpactOfLoss { TenantId = AbpSession.TenantId ?? 1, NameEn = "Severe", NameAr = "شديد" };
        Context.ImpactOfLosses.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateReasonForLeaving()
    {
        var entity = new ReasonForLeaving { TenantId = AbpSession.TenantId ?? 1, NameEn = "Better Offer", NameAr = "عرض أفضل" };
        Context.ReasonForLeavings.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateTalentPool()
    {
        var entity = new TalentPool { TenantId = AbpSession.TenantId ?? 1, NameEn = "Leaders", NameAr = "قادة" };
        Context.TalentPools.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_EmployeeTalentFlag()
    {
        var employeeId = CreateEmployee();
        var riskOfLossId = CreateRiskOfLoss();
        var impactOfLossId = CreateImpactOfLoss();
        var reasonForLeavingId = CreateReasonForLeaving();
        var talentPoolId = CreateTalentPool();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTalentFlagDto
        {
            EmployeeId = employeeId,
            RiskOfLossId = riskOfLossId,
            ImpactOfLossId = impactOfLossId,
            ReasonForLeavingId = reasonForLeavingId,
            TalentPoolId = talentPoolId
        });

        var result = await service.GetAll(new GetAllEmployeeTalentFlagsInput { EmployeeId = employeeId });
        result.TotalCount.ShouldBe(1);
        result.Items.Single().TalentPoolId.ShouldBe(talentPoolId);
    }

    [Fact]
    public async Task Should_Update_EmployeeTalentFlag()
    {
        var employeeId = CreateEmployee();
        var riskOfLossId = CreateRiskOfLoss();
        var impactOfLossId = CreateImpactOfLoss();
        var reasonForLeavingId = CreateReasonForLeaving();
        var talentPoolId = CreateTalentPool();
        var newTalentPoolId = CreateTalentPool();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTalentFlagDto
        {
            EmployeeId = employeeId,
            RiskOfLossId = riskOfLossId,
            ImpactOfLossId = impactOfLossId,
            ReasonForLeavingId = reasonForLeavingId,
            TalentPoolId = talentPoolId
        });
        var all = await service.GetAll(new GetAllEmployeeTalentFlagsInput { EmployeeId = employeeId });
        var id = all.Items.Single().Id;

        await service.CreateOrEdit(new CreateOrEditEmployeeTalentFlagDto
        {
            Id = id,
            EmployeeId = employeeId,
            RiskOfLossId = riskOfLossId,
            ImpactOfLossId = impactOfLossId,
            ReasonForLeavingId = reasonForLeavingId,
            TalentPoolId = newTalentPoolId
        });

        var edit = await service.GetEmployeeTalentFlagForEdit(new EntityDto(id));
        edit.EmployeeTalentFlag.TalentPoolId.ShouldBe(newTalentPoolId);
    }

    [Fact]
    public async Task Should_Delete_EmployeeTalentFlag()
    {
        var employeeId = CreateEmployee();
        var riskOfLossId = CreateRiskOfLoss();
        var impactOfLossId = CreateImpactOfLoss();
        var reasonForLeavingId = CreateReasonForLeaving();
        var talentPoolId = CreateTalentPool();
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditEmployeeTalentFlagDto
        {
            EmployeeId = employeeId,
            RiskOfLossId = riskOfLossId,
            ImpactOfLossId = impactOfLossId,
            ReasonForLeavingId = reasonForLeavingId,
            TalentPoolId = talentPoolId
        });
        var all = await service.GetAll(new GetAllEmployeeTalentFlagsInput { EmployeeId = employeeId });
        var id = all.Items.Single().Id;

        await service.Delete(new EntityDto(id));

        (await service.GetAll(new GetAllEmployeeTalentFlagsInput { EmployeeId = employeeId })).TotalCount.ShouldBe(0);
    }
}
