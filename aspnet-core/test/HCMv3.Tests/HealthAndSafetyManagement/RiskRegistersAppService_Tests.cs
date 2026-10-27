using System;
using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class RiskRegistersAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private RiskRegistersAppService CreateAppService()
    {
        return CreateService(() => new RiskRegistersAppService(FakeRepository.For<RiskRegister>(Context)));
    }

    private int CreateRiskType()
    {
        var entity = new RiskType { TenantId = AbpSession.TenantId ?? 1, NameEn = "Type", NameAr = "Type" };
        Context.RiskTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateRiskImpact()
    {
        var entity = new RiskImpact { TenantId = AbpSession.TenantId ?? 1, NameEn = "Impact", NameAr = "Impact" };
        Context.RiskImpacts.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateRiskPossibility()
    {
        var entity = new RiskPossibility { TenantId = AbpSession.TenantId ?? 1, NameEn = "Possibility", NameAr = "Possibility" };
        Context.RiskPossibilities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_RiskRegister()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRiskRegisterDto
        {
            ReportingDate = new DateTime(2026, 1, 1),
            RiskTypeId = CreateRiskType(),
            ImpactId = CreateRiskImpact(),
            PossibilityId = CreateRiskPossibility(),
            ReportedById = CreateEmployee(),
            LocationId = CreateWorkLocation(),
            ReassessmentDate = new DateTime(2026, 6, 1),
            Description = "Chemical spill risk"
        });

        var result = await service.GetAll(new GetAllRiskRegistersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Description.ShouldBe("Chemical spill risk");
    }

    [Fact]
    public async Task Should_Delete_RiskRegister()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditRiskRegisterDto
        {
            ReportingDate = new DateTime(2026, 1, 1),
            RiskTypeId = CreateRiskType(),
            ImpactId = CreateRiskImpact(),
            PossibilityId = CreateRiskPossibility(),
            ReportedById = CreateEmployee(),
            LocationId = CreateWorkLocation(),
            ReassessmentDate = new DateTime(2026, 6, 1)
        });

        var created = (await service.GetAll(new GetAllRiskRegistersInput())).Items[0];

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(created.Id));

        var result = await service.GetAll(new GetAllRiskRegistersInput());
        result.TotalCount.ShouldBe(0);
    }
}
