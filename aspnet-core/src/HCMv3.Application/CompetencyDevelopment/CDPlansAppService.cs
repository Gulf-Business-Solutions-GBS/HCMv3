using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.CompetencyDevelopment.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.CompetencyDevelopment;

/// <summary>
/// Application service for the weighted competency targets planned within a <see cref="CDForm"/> (ported
/// from legacy XAF CDPlan business object). See <see cref="CDPlan"/> class remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDPlans)]
public class CDPlansAppService : HCMv3AppServiceBase, ICDPlansAppService
{
    private readonly IRepository<CDPlan> _repository;

    public CDPlansAppService(IRepository<CDPlan> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<CDPlanDto>> GetAll(GetAllCDPlansInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(p => p.Competency)
            .WhereIf(input.CDFormId.HasValue, p => p.CDFormId == input.CDFormId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<CDPlanDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetCDPlanForEditOutput> GetCDPlanForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetCDPlanForEditOutput
        {
            CDPlan = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditCDPlanDto input)
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

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDPlans_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDPlans_Create)]
    protected virtual async Task CreateAsync(CreateOrEditCDPlanDto input)
    {
        var entity = MapToEntity(input, new CDPlan());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_CompetencyDevelopment_CDPlans_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditCDPlanDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static CDPlan MapToEntity(CreateOrEditCDPlanDto input, CDPlan entity)
    {
        entity.CDFormId = input.CDFormId;
        entity.CompetencyId = input.CompetencyId;
        entity.Weight = input.Weight;
        entity.Target = input.Target;
        entity.EmployeeComments = input.EmployeeComments;
        entity.ManagerComments = input.ManagerComments;

        return entity;
    }

    /// <summary>Legacy <c>PerformValidation</c> parity: no duplicate Competency per parent CDForm.</summary>
    private async Task ValidateAsync(CDPlan entity)
    {
        var duplicate = await _repository.GetAll()
            .AnyAsync(p => p.Id != entity.Id &&
                           p.CDFormId == entity.CDFormId &&
                           p.CompetencyId == entity.CompetencyId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("CompetencyAlreadyAssigned"));
        }
    }

    private static CDPlanDto ToDto(CDPlan entity)
    {
        return new CDPlanDto
        {
            Id = entity.Id,
            CDFormId = entity.CDFormId,
            CompetencyId = entity.CompetencyId,
            CompetencyName = entity.Competency?.NameEn,
            Weight = entity.Weight,
            Target = entity.Target,
            EmployeeComments = entity.EmployeeComments,
            ManagerComments = entity.ManagerComments
        };
    }

    private static CreateOrEditCDPlanDto ToCreateOrEditDto(CDPlan entity)
    {
        return new CreateOrEditCDPlanDto
        {
            Id = entity.Id,
            CDFormId = entity.CDFormId,
            CompetencyId = entity.CompetencyId,
            Weight = entity.Weight,
            Target = entity.Target,
            EmployeeComments = entity.EmployeeComments,
            ManagerComments = entity.ManagerComments
        };
    }
}
