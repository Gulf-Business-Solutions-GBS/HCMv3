using System;
using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class ActionRegistersAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private ActionRegistersAppService CreateAppService()
    {
        return CreateService(() => new ActionRegistersAppService(FakeRepository.For<ActionRegister>(Context)));
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

    private int CreateRisk()
    {
        var riskType = new RiskType { TenantId = AbpSession.TenantId ?? 1, NameEn = "Type", NameAr = "Type" };
        var impact = new RiskImpact { TenantId = AbpSession.TenantId ?? 1, NameEn = "Impact", NameAr = "Impact" };
        var possibility = new RiskPossibility { TenantId = AbpSession.TenantId ?? 1, NameEn = "Possibility", NameAr = "Possibility" };
        Context.RiskTypes.Add(riskType);
        Context.RiskImpacts.Add(impact);
        Context.RiskPossibilities.Add(possibility);
        Context.SaveChanges();

        var risk = new RiskRegister
        {
            TenantId = AbpSession.TenantId ?? 1,
            ReportingDate = new DateTime(2026, 1, 1),
            RiskTypeId = riskType.Id,
            ImpactId = impact.Id,
            PossibilityId = possibility.Id,
            ReportedById = CreateEmployee(),
            LocationId = CreateWorkLocation(),
            ReassessmentDate = new DateTime(2026, 6, 1)
        };
        Context.RiskRegisters.Add(risk);
        Context.SaveChanges();
        return risk.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_ActionRegister()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditActionRegisterDto
        {
            ActionDate = new DateTime(2026, 1, 5),
            IncidentId = CreateIncident(),
            RiskId = CreateRisk(),
            AssignedToId = CreateEmployee(),
            Description = "Fix the hazard"
        });

        var result = await service.GetAll(new GetAllActionRegistersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Description.ShouldBe("Fix the hazard");
    }
}
