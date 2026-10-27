using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeGratuityQuota"/> (ported from legacy XAF
/// EmployeeGratuityQuota business object, Wave 2 deferred bucket). Ports the essential active validations
/// only (required fields, EntitlementTo/DeductionTo not earlier than their respective "From" dates).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuityQuotas)]
public class EmployeeGratuityQuotasAppService : HCMv3AppServiceBase, IEmployeeGratuityQuotasAppService
{
    private readonly IRepository<EmployeeGratuityQuota> _repository;

    public EmployeeGratuityQuotasAppService(IRepository<EmployeeGratuityQuota> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeGratuityQuotaDto>> GetAll(GetAllEmployeeGratuityQuotasInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(q => q.Employee)
            .WhereIf(input.EmployeeId.HasValue, q => q.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeGratuityQuotaDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeGratuityQuotaForEditOutput> GetEmployeeGratuityQuotaForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeGratuityQuotaForEditOutput
        {
            EmployeeGratuityQuota = new CreateOrEditEmployeeGratuityQuotaDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EntitlementFrom = entity.EntitlementFrom,
                EntitlementTo = entity.EntitlementTo,
                DeductionFrom = entity.DeductionFrom,
                DeductionTo = entity.DeductionTo,
                QuotaAccrued = entity.QuotaAccrued,
                QuotaUsed = entity.QuotaUsed,
                QuotaAdjusted = entity.QuotaAdjusted
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeGratuityQuotaDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuityQuotas_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeGratuityQuotaDto input)
    {
        var entity = MapToEntity(input, new EmployeeGratuityQuota());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateEntity(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuityQuotas_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeGratuityQuotaDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateEntity(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeGratuityQuotas_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateEntity(EmployeeGratuityQuota entity)
    {
        if (entity.EntitlementTo < entity.EntitlementFrom || entity.DeductionTo < entity.DeductionFrom)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeGratuityQuota MapToEntity(CreateOrEditEmployeeGratuityQuotaDto input, EmployeeGratuityQuota entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.EntitlementFrom = input.EntitlementFrom.Date;
        entity.EntitlementTo = input.EntitlementTo.Date;
        entity.DeductionFrom = input.DeductionFrom.Date;
        entity.DeductionTo = input.DeductionTo.Date;
        entity.QuotaAccrued = System.Math.Round(input.QuotaAccrued, 3);
        entity.QuotaUsed = System.Math.Round(input.QuotaUsed, 3);
        entity.QuotaAdjusted = System.Math.Round(input.QuotaAdjusted, 3);
        return entity;
    }

    private static EmployeeGratuityQuotaDto ToDto(EmployeeGratuityQuota entity)
    {
        return new EmployeeGratuityQuotaDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
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
}
