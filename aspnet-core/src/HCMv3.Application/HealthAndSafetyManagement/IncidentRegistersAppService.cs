using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.HealthAndSafetyManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HealthAndSafetyManagement;

/// <summary>
/// Application service for the health and safety incident register (ported from legacy XAF
/// IncidentRegister business object). Legacy <c>PerformValidation</c> required IncidentDate,
/// IncidentType, Nature and Severity -- ported as-is via DataAnnotations on
/// <see cref="CreateOrEditIncidentRegisterDto"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentRegisters)]
public class IncidentRegistersAppService : HCMv3AppServiceBase, IIncidentRegistersAppService
{
    private readonly IRepository<IncidentRegister> _incidentRegisterRepository;

    public IncidentRegistersAppService(IRepository<IncidentRegister> incidentRegisterRepository)
    {
        _incidentRegisterRepository = incidentRegisterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<IncidentRegisterDto>> GetAll(GetAllIncidentRegistersInput input)
    {
        input.Normalize();

        var query = _incidentRegisterRepository.GetAll()
            .Include(i => i.IncidentType)
            .Include(i => i.Nature)
            .Include(i => i.Severity)
            .Include(i => i.ReportedBy)
            .Include(i => i.PersonInvolved)
            .Include(i => i.Location)
            .Include(i => i.AssignedTo)
            .Include(i => i.Status)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), i => i.Description.Contains(input.Filter))
            .WhereIf(input.IncidentTypeId.HasValue, i => i.IncidentTypeId == input.IncidentTypeId)
            .WhereIf(input.StatusId.HasValue, i => i.StatusId == input.StatusId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<IncidentRegisterDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetIncidentRegisterForEditOutput> GetIncidentRegisterForEdit(EntityDto input)
    {
        var entity = await _incidentRegisterRepository.GetAsync(input.Id);

        return new GetIncidentRegisterForEditOutput
        {
            IncidentRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditIncidentRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _incidentRegisterRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditIncidentRegisterDto input)
    {
        var entity = MapToEntity(input, new IncidentRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _incidentRegisterRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_IncidentRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditIncidentRegisterDto input)
    {
        var entity = await _incidentRegisterRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _incidentRegisterRepository.UpdateAsync(entity);
    }

    private static IncidentRegister MapToEntity(CreateOrEditIncidentRegisterDto input, IncidentRegister entity)
    {
        entity.IncidentDate = input.IncidentDate;
        entity.IncidentTime = input.IncidentTime;
        entity.IncidentTypeId = input.IncidentTypeId;
        entity.Description = input.Description;
        entity.NatureId = input.NatureId;
        entity.SeverityId = input.SeverityId;
        entity.ReportedById = input.ReportedById;
        entity.PersonInvolvedId = input.PersonInvolvedId;
        entity.OtherPeopleInvolved = input.OtherPeopleInvolved;
        entity.LocationId = input.LocationId;
        entity.AssignedToId = input.AssignedToId;
        entity.DisabilityPercentage = input.DisabilityPercentage;
        entity.StatusId = input.StatusId;
        entity.Comments = input.Comments;

        return entity;
    }

    private static IncidentRegisterDto ToDto(IncidentRegister entity)
    {
        return new IncidentRegisterDto
        {
            Id = entity.Id,
            IncidentDate = entity.IncidentDate,
            IncidentTime = entity.IncidentTime,
            IncidentTypeId = entity.IncidentTypeId,
            IncidentTypeNameEn = entity.IncidentType?.NameEn,
            Description = entity.Description,
            NatureId = entity.NatureId,
            NatureNameEn = entity.Nature?.NameEn,
            SeverityId = entity.SeverityId,
            SeverityNameEn = entity.Severity?.NameEn,
            ReportedById = entity.ReportedById,
            ReportedByCode = entity.ReportedBy?.Code,
            PersonInvolvedId = entity.PersonInvolvedId,
            PersonInvolvedCode = entity.PersonInvolved?.Code,
            OtherPeopleInvolved = entity.OtherPeopleInvolved,
            LocationId = entity.LocationId,
            LocationName = entity.Location?.NameEn,
            AssignedToId = entity.AssignedToId,
            AssignedToCode = entity.AssignedTo?.Code,
            DisabilityPercentage = entity.DisabilityPercentage,
            StatusId = entity.StatusId,
            StatusNameEn = entity.Status?.NameEn,
            Comments = entity.Comments
        };
    }

    private static CreateOrEditIncidentRegisterDto ToCreateOrEditDto(IncidentRegister entity)
    {
        return new CreateOrEditIncidentRegisterDto
        {
            Id = entity.Id,
            IncidentDate = entity.IncidentDate,
            IncidentTime = entity.IncidentTime,
            IncidentTypeId = entity.IncidentTypeId,
            Description = entity.Description,
            NatureId = entity.NatureId,
            SeverityId = entity.SeverityId,
            ReportedById = entity.ReportedById,
            PersonInvolvedId = entity.PersonInvolvedId,
            OtherPeopleInvolved = entity.OtherPeopleInvolved,
            LocationId = entity.LocationId,
            AssignedToId = entity.AssignedToId,
            DisabilityPercentage = entity.DisabilityPercentage,
            StatusId = entity.StatusId,
            Comments = entity.Comments
        };
    }
}
