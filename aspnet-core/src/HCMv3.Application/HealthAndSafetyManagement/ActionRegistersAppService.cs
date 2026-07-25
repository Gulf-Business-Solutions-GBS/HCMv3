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
/// Application service for the corrective/preventive action tracker (ported from legacy XAF
/// ActionRegister business object). Legacy <c>PerformValidation</c> required Incident, Risk, AssignedTo
/// and ActionDate -- ported as-is via DataAnnotations on <see cref="CreateOrEditActionRegisterDto"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_ActionRegisters)]
public class ActionRegistersAppService : HCMv3AppServiceBase, IActionRegistersAppService
{
    private readonly IRepository<ActionRegister> _actionRegisterRepository;

    public ActionRegistersAppService(IRepository<ActionRegister> actionRegisterRepository)
    {
        _actionRegisterRepository = actionRegisterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<ActionRegisterDto>> GetAll(GetAllActionRegistersInput input)
    {
        input.Normalize();

        var query = _actionRegisterRepository.GetAll()
            .Include(a => a.AssignedTo)
            .Include(a => a.Status)
            .WhereIf(input.IncidentId.HasValue, a => a.IncidentId == input.IncidentId)
            .WhereIf(input.RiskId.HasValue, a => a.RiskId == input.RiskId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<ActionRegisterDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetActionRegisterForEditOutput> GetActionRegisterForEdit(EntityDto input)
    {
        var entity = await _actionRegisterRepository.GetAsync(input.Id);

        return new GetActionRegisterForEditOutput
        {
            ActionRegister = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditActionRegisterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_ActionRegisters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _actionRegisterRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_ActionRegisters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditActionRegisterDto input)
    {
        var entity = MapToEntity(input, new ActionRegister());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _actionRegisterRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_HealthAndSafetyManagement_ActionRegisters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditActionRegisterDto input)
    {
        var entity = await _actionRegisterRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await _actionRegisterRepository.UpdateAsync(entity);
    }

    private static ActionRegister MapToEntity(CreateOrEditActionRegisterDto input, ActionRegister entity)
    {
        entity.ActionDate = input.ActionDate;
        entity.Description = input.Description;
        entity.IncidentId = input.IncidentId;
        entity.RiskId = input.RiskId;
        entity.AssignedToId = input.AssignedToId;
        entity.StatusId = input.StatusId;

        return entity;
    }

    private static ActionRegisterDto ToDto(ActionRegister entity)
    {
        return new ActionRegisterDto
        {
            Id = entity.Id,
            ActionDate = entity.ActionDate,
            Description = entity.Description,
            IncidentId = entity.IncidentId,
            RiskId = entity.RiskId,
            AssignedToId = entity.AssignedToId,
            AssignedToCode = entity.AssignedTo?.Code,
            StatusId = entity.StatusId,
            StatusNameEn = entity.Status?.NameEn
        };
    }

    private static CreateOrEditActionRegisterDto ToCreateOrEditDto(ActionRegister entity)
    {
        return new CreateOrEditActionRegisterDto
        {
            Id = entity.Id,
            ActionDate = entity.ActionDate,
            Description = entity.Description,
            IncidentId = entity.IncidentId,
            RiskId = entity.RiskId,
            AssignedToId = entity.AssignedToId,
            StatusId = entity.StatusId
        };
    }
}
