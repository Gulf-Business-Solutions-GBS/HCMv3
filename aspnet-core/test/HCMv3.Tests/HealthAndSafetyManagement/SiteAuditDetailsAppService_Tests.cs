using System;
using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class SiteAuditDetailsAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private SiteAuditDetailsAppService CreateAppService()
    {
        return CreateService(() => new SiteAuditDetailsAppService(FakeRepository.For<SiteAuditDetail>(Context)));
    }

    private int CreateSiteAudit()
    {
        var entity = new SiteAudit
        {
            TenantId = AbpSession.TenantId ?? 1,
            AuditDate = new DateTime(2026, 2, 1),
            LocationId = CreateWorkLocation(),
            ConductedById = CreateEmployee(),
            ReassessmentDate = new DateTime(2026, 8, 1)
        };
        Context.SiteAudits.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SiteAuditDetail()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSiteAuditDetailDto
        {
            SiteAuditId = CreateSiteAudit(),
            Observation = "Blocked fire exit",
            CorrectionAction = "Clear the exit"
        });

        var result = await service.GetAll(new GetAllSiteAuditDetailsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Observation.ShouldBe("Blocked fire exit");
    }
}
