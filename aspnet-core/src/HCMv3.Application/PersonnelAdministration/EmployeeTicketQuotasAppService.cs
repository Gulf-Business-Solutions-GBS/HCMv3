using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeTicketQuota"/> (ported from legacy XAF EmployeeTicketQuota
/// business object, Wave 3 deferred bucket). <c>QuotaBalance</c> is computed here (not stored) --
/// see <see cref="EmployeeTicketQuota"/> remarks.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketQuotas)]
public class EmployeeTicketQuotasAppService : HCMv3AppServiceBase, IEmployeeTicketQuotasAppService
{
    private readonly IRepository<EmployeeTicketQuota> _repository;

    public EmployeeTicketQuotasAppService(IRepository<EmployeeTicketQuota> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTicketQuotaDto>> GetAll(GetAllEmployeeTicketQuotasInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(q => q.Employee)
            .Include(q => q.TicketType)
            .WhereIf(input.EmployeeId.HasValue, q => q.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTicketQuotaDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTicketQuotaForEditOutput> GetEmployeeTicketQuotaForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeTicketQuotaForEditOutput
        {
            EmployeeTicketQuota = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeTicketQuotaDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketQuotas_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketQuotas_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTicketQuotaDto input)
    {
        var entity = MapToEntity(input, new EmployeeTicketQuota());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeTicketQuotas_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTicketQuotaDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static EmployeeTicketQuota MapToEntity(CreateOrEditEmployeeTicketQuotaDto input, EmployeeTicketQuota entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TicketTypeId = input.TicketTypeId;
        entity.EntitlementFrom = input.EntitlementFrom.Date;
        entity.EntitlementTo = input.EntitlementTo.Date;
        entity.DeductionFrom = input.DeductionFrom.Date;
        entity.DeductionTo = input.DeductionTo.Date;
        entity.QuotaAccrued = System.Math.Round(input.QuotaAccrued, 6);
        entity.QuotaUsed = System.Math.Round(input.QuotaUsed, 6);
        entity.QuotaAdjusted = System.Math.Round(input.QuotaAdjusted, 6);
        return entity;
    }

    private static EmployeeTicketQuotaDto ToDto(EmployeeTicketQuota entity)
    {
        return new EmployeeTicketQuotaDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            TicketTypeId = entity.TicketTypeId,
            TicketTypeName = entity.TicketType?.NameEn,
            EntitlementFrom = entity.EntitlementFrom,
            EntitlementTo = entity.EntitlementTo,
            DeductionFrom = entity.DeductionFrom,
            DeductionTo = entity.DeductionTo,
            QuotaAccrued = entity.QuotaAccrued,
            QuotaUsed = entity.QuotaUsed,
            QuotaAdjusted = entity.QuotaAdjusted,
            QuotaBalance = System.Math.Round(entity.QuotaAccrued - (entity.QuotaUsed + entity.QuotaAdjusted), 3)
        };
    }

    private static CreateOrEditEmployeeTicketQuotaDto ToCreateOrEditDto(EmployeeTicketQuota entity)
    {
        return new CreateOrEditEmployeeTicketQuotaDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            TicketTypeId = entity.TicketTypeId,
            EntitlementFrom = entity.EntitlementFrom,
            EntitlementTo = entity.EntitlementTo,
            DeductionFrom = entity.DeductionFrom,
            DeductionTo = entity.DeductionTo,
            QuotaAccrued = entity.QuotaAccrued,
            QuotaUsed = entity.QuotaUsed,
            QuotaAdjusted = entity.QuotaAdjusted
        };
    }
}
