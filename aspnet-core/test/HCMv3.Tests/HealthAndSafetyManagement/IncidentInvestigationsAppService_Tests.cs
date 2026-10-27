using System;
using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class IncidentInvestigationsAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private IncidentInvestigationsAppService CreateAppService()
    {
        return CreateService(() => new IncidentInvestigationsAppService(FakeRepository.For<IncidentInvestigation>(Context)));
    }

    private int CreateIncident()
    {
        var incidentType = new IncidentType { TenantId = AbpSession.TenantId ?? 1, NameEn = "Type", NameAr = "Type" };
        var nature = new IncidentNature { TenantId = AbpSession.TenantId ?? 1, NameEn = "Nature", NameAr = "Nature" };
        var severity = new IncidentSeverity { TenantId = AbpSession.TenantId ?? 1, NameEn = "Severity", NameAr = "Severity" };
        Context.IncidentTypes.Add(incidentType);
        Context.IncidentNatures.Add(nature);
        Context.IncidentSeverities.Add(severity);
        Context.SaveChanges();

        var incident = new IncidentRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            IncidentDate = new DateTime(2026, 1, 1),
            IncidentTypeId = incidentType.Id,
            NatureId = nature.Id,
            SeverityId = severity.Id
        };
        Context.IncidentRegisters.Add(incident);
        Context.SaveChanges();
        return incident.Id;
    }

    private int CreateRootCause()
    {
        var entity = new InvestigationRootCause { TenantId = AbpSession.TenantId ?? 1, NameEn = "Cause", NameAr = "Cause" };
        Context.InvestigationRootCauses.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_IncidentInvestigation()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditIncidentInvestigationDto
        {
            InvestigationDate = new DateTime(2026, 1, 3),
            IncidentId = CreateIncident(),
            RootCauseId = CreateRootCause(),
            RootCauseAnalysis = "Wet floor"
        });

        var result = await service.GetAll(new GetAllIncidentInvestigationsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].RootCauseAnalysis.ShouldBe("Wet floor");
    }
}
