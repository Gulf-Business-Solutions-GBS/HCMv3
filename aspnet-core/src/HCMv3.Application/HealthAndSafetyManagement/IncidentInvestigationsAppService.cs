using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.HealthAndSafetyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Application service for the investigation record raised against an <see cref="IncidentRegister"/>
/// (ported from legacy XAF IncidentInvestigation business object). Legacy <c>PerformValidation</c>
/// required InvestigationDate, Incident and RootCause -- ported as-is via DataAnnotations on
/// <see cref="CreateOrEditIncidentInvestigationDto"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentInvestigations)]
public class IncidentInvestigationsAppService : HCMv3AppServiceBase, IIncidentInvestigationsAppService
{
    private readonly IRepository<IncidentInvestigation> _incidentInvestigationRepository;

    public IncidentInvestigationsAppService(IRepository<IncidentInvestigation> incidentInvestigationRepository)
    {
        _incidentInvestigationRepository = incidentInvestigationRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<IncidentInvestigationDto>> GetAll(GetAllIncidentInvestigationsInput input)
    {
        input.Normalize();

        var query = _incidentInvestigationRepository.GetAll()
            .Include(i => i.RootCause)
            .WhereIf(input.IncidentId.HasValue, i => i.IncidentId == input.IncidentId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<IncidentInvestigationDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetIncidentInvestigationForEditOutput> GetIncidentInvestigationForEdit(EntityDto input)
    {
        var entity = await _incidentInvestigationRepository.GetAsync(input.Id);

        return new GetIncidentInvestigationForEditOutput
        {
            IncidentInvestigation = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditIncidentInvestigationDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentInvestigations_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _incidentInvestigationRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentInvestigations_Create)]
    protected virtual async Task CreateAsync(CreateOrEditIncidentInvestigationDto input)
    {
        var entity = MapToEntity(input, new IncidentInvestigation());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _incidentInvestigationRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentInvestigations_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditIncidentInvestigationDto input)
    {
        var entity = await _incidentInvestigationRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _incidentInvestigationRepository.UpdateAsync(entity);
    }

    private static IncidentInvestigation MapToEntity(CreateOrEditIncidentInvestigationDto input, IncidentInvestigation entity)
    {
        entity.InvestigationDate = input.InvestigationDate;
        entity.InvestigationTime = input.InvestigationTime;
        entity.IncidentId = input.IncidentId;
        entity.RootCauseId = input.RootCauseId;
        entity.RootCauseAnalysis = input.RootCauseAnalysis;
        entity.Comments = input.Comments;

        return entity;
    }

    private static IncidentInvestigationDto ToDto(IncidentInvestigation entity)
    {
        return new IncidentInvestigationDto
        {
            Id = entity.Id,
            InvestigationDate = entity.InvestigationDate,
            InvestigationTime = entity.InvestigationTime,
            IncidentId = entity.IncidentId,
            RootCauseId = entity.RootCauseId,
            RootCauseNameEn = entity.RootCause?.NameEn,
            RootCauseAnalysis = entity.RootCauseAnalysis,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditIncidentInvestigationDto ToCreateOrEditDto(IncidentInvestigation entity)
    {
        return new CreateOrEditIncidentInvestigationDto
        {
            Id = entity.Id,
            InvestigationDate = entity.InvestigationDate,
            InvestigationTime = entity.InvestigationTime,
            IncidentId = entity.IncidentId,
            RootCauseId = entity.RootCauseId,
            RootCauseAnalysis = entity.RootCauseAnalysis,
            Comments = entity.Comments
        };
    }
}
