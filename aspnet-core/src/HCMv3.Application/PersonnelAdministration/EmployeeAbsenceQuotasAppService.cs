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
/// Application service for <see cref="EmployeeAbsenceQuota"/> (ported from legacy XAF EmployeeAbsenceQuota
/// business object, Wave 1 deferred bucket). Ports the essential active validations only (required fields,
/// EntitlementTo not earlier than EntitlementFrom). The legacy max-limit-based auto-capping of QuotaAccrued
/// and notification-status cascade are NOT ported here -- see class remarks on
/// <see cref="EmployeeAbsenceQuota"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas)]
public class EmployeeAbsenceQuotasAppService : HCMv3AppServiceBase, IEmployeeAbsenceQuotasAppService
{
    private readonly IRepository<EmployeeAbsenceQuota> _repository;

    public EmployeeAbsenceQuotasAppService(IRepository<EmployeeAbsenceQuota> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAbsenceQuotaDto>> GetAll(GetAllEmployeeAbsenceQuotasInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(q => q.Employee)
            .Include(q => q.AbsenceType)
            .WhereIf(input.EmployeeId.HasValue, q => q.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAbsenceQuotaDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAbsenceQuotaForEditOutput> GetEmployeeAbsenceQuotaForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAbsenceQuotaForEditOutput
        {
            EmployeeAbsenceQuota = new CreateOrEditEmployeeAbsenceQuotaDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AbsenceTypeId = entity.AbsenceTypeId,
                EntitlementFrom = entity.EntitlementFrom,
                EntitlementTo = entity.EntitlementTo,
                DeductionFrom = entity.DeductionFrom,
                DeductionTo = entity.DeductionTo,
                QuotaAccrued = entity.QuotaAccrued,
                QuotaUsed = entity.QuotaUsed,
                QuotaAdjusted = entity.QuotaAdjusted,
                QuotaForfeited = entity.QuotaForfeited,
                NotificationSent = entity.NotificationSent
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAbsenceQuotaDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAbsenceQuotaDto input)
    {
        var entity = MapToEntity(input, new EmployeeAbsenceQuota());
        entity.TenantId = AbpSession.TenantId ?? 0;
        entity.ActualQuotaAccrued = entity.QuotaAccrued;

        ValidateEntity(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAbsenceQuotaDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateEntity(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateEntity(EmployeeAbsenceQuota entity)
    {
        if (entity.EntitlementTo < entity.EntitlementFrom)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeAbsenceQuota MapToEntity(CreateOrEditEmployeeAbsenceQuotaDto input, EmployeeAbsenceQuota entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.EntitlementFrom = input.EntitlementFrom.Date;
        entity.EntitlementTo = input.EntitlementTo.Date;
        entity.DeductionFrom = input.DeductionFrom.Date;
        entity.DeductionTo = input.DeductionTo.Date;
        entity.QuotaAccrued = System.Math.Round(input.QuotaAccrued, 6);
        entity.QuotaUsed = System.Math.Round(input.QuotaUsed, 6);
        entity.QuotaAdjusted = System.Math.Round(input.QuotaAdjusted, 6);
        entity.QuotaForfeited = System.Math.Round(input.QuotaForfeited, 6);
        entity.NotificationSent = input.NotificationSent;
        return entity;
    }

    private static EmployeeAbsenceQuotaDto ToDto(EmployeeAbsenceQuota entity)
    {
        return new EmployeeAbsenceQuotaDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            EntitlementFrom = entity.EntitlementFrom,
            EntitlementTo = entity.EntitlementTo,
            DeductionFrom = entity.DeductionFrom,
            DeductionTo = entity.DeductionTo,
            QuotaAccrued = entity.QuotaAccrued,
            ActualQuotaAccrued = entity.ActualQuotaAccrued,
            QuotaUsed = entity.QuotaUsed,
            QuotaAdjusted = entity.QuotaAdjusted,
            QuotaForfeited = entity.QuotaForfeited,
            NotificationSent = entity.NotificationSent,
            QuotaBalance = System.Math.Round(entity.QuotaAccrued - (entity.QuotaUsed + entity.QuotaAdjusted + entity.QuotaForfeited), 2)
        };
    }
}
