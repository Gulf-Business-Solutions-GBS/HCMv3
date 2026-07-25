using System;
using System.Threading.Tasks;
using HCMv3.HealthAndSafetyManagement;
using HCMv3.HealthAndSafetyManagement.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.HealthAndSafetyManagement;

// ReSharper disable once InconsistentNaming
public class IncidentRegistersAppService_Tests : HealthAndSafetyManagementAppServiceTestBase
{
    private IncidentRegistersAppService CreateAppService()
    {
        return CreateService(() => new IncidentRegistersAppService(FakeRepository.For<IncidentRegister>(Context)));
    }

    private int CreateIncidentType()
    {
        var entity = new IncidentType { TenantId = AbpSession.TenantId ?? 1, NameEn = "Type", NameAr = "Type" };
        Context.IncidentTypes.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateIncidentNature()
    {
        var entity = new IncidentNature { TenantId = AbpSession.TenantId ?? 1, NameEn = "Nature", NameAr = "Nature" };
        Context.IncidentNatures.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    private int CreateIncidentSeverity()
    {
        var entity = new IncidentSeverity { TenantId = AbpSession.TenantId ?? 1, NameEn = "Severity", NameAr = "Severity" };
        Context.IncidentSeverities.Add(entity);
        Context.SaveChanges();
        return entity.Id;
    }

    [Fact]
    public async Task Should_Create_And_Retrieve_IncidentRegister()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditIncidentRegisterDto
        {
            IncidentDate = new DateTime(2026, 1, 1),
            IncidentTypeId = CreateIncidentType(),
            NatureId = CreateIncidentNature(),
            SeverityId = CreateIncidentSeverity(),
            Description = "Slip and fall"
        });

        var result = await service.GetAll(new GetAllIncidentRegistersInput());
        result.TotalCount.ShouldBe(1);
        result.Items[0].Description.ShouldBe("Slip and fall");
    }

    [Fact]
    public async Task Should_Update_IncidentRegister()
    {
        var service = CreateAppService();
        var incidentTypeId = CreateIncidentType();
        var natureId = CreateIncidentNature();
        var severityId = CreateIncidentSeverity();

        await service.CreateOrEdit(new CreateOrEditIncidentRegisterDto
        {
            IncidentDate = new DateTime(2026, 1, 1),
            IncidentTypeId = incidentTypeId,
            NatureId = natureId,
            SeverityId = severityId,
            Description = "Initial"
        });

        var created = (await service.GetAll(new GetAllIncidentRegistersInput())).Items[0];

        await service.CreateOrEdit(new CreateOrEditIncidentRegisterDto
        {
            Id = created.Id,
            IncidentDate = created.IncidentDate,
            IncidentTypeId = incidentTypeId,
            NatureId = natureId,
            SeverityId = severityId,
            Description = "Updated"
        });

        var edited = await service.GetIncidentRegisterForEdit(new Abp.Application.Services.Dto.EntityDto(created.Id));
        edited.IncidentRegister.Description.ShouldBe("Updated");
    }

    [Fact]
    public async Task Should_Delete_IncidentRegister()
    {
        var service = CreateAppService();

        await service.CreateOrEdit(new CreateOrEditIncidentRegisterDto
        {
            IncidentDate = new DateTime(2026, 1, 1),
            IncidentTypeId = CreateIncidentType(),
            NatureId = CreateIncidentNature(),
            SeverityId = CreateIncidentSeverity()
        });

        var created = (await service.GetAll(new GetAllIncidentRegistersInput())).Items[0];

        await service.Delete(new Abp.Application.Services.Dto.EntityDto(created.Id));

        var result = await service.GetAll(new GetAllIncidentRegistersInput());
        result.TotalCount.ShouldBe(0);
    }
}
