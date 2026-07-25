using System;
using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class SiteAuditsAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private SiteAuditsAppService CreateAppService()
    {
        return CreateService(() => new SiteAuditsAppService(FakeRepository.For<SiteAudit>(Context)));
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_SiteAudit()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditSiteAuditDto
        {
            AuditDate = new DateTime(2026, 2, 1),
            LocationId = CreateWorkLocation(),
            ConductedById = CreateEmployee(),
            ReassessmentDate = new DateTime(2026, 8, 1),
            Comments = "Routine audit"
        });

        var result = await service.GetAll(new GetAllSiteAuditsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Comments.ShouldBe("Routine audit");
    }
}
