using System;
using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class IncidentClaimsAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private IncidentClaimsAppService CreateAppService()
    {
        return CreateService(() => new IncidentClaimsAppService(FakeRepository.For<IncidentClaim>(Context)));
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

    [Fact]
    public async Task Should_Create_And_Retrieve_IncidentClaim()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditIncidentClaimDto
        {
            ClaimDate = new DateTime(2026, 1, 2),
            IncidentId = CreateIncident(),
            CurrencyId = CreateCurrency(),
            PaymentDate = new DateTime(2026, 1, 10),
            Amount = 500
        });

        var result = await service.GetAll(new GetAllIncidentClaimsInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Amount.ShouldBe(500);
    }
}
