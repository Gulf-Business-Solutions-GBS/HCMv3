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
/// Application service for <see cref="EmployeeAttendanceQuota"/> (ported from legacy XAF
/// EmployeeAttendanceQuota business object, Wave 1 deferred bucket). Note (legacy quirk, intentionally
/// preserved): this quota is Employee-scoped only, with no AttendanceType FK -- see class remarks on
/// <see cref="EmployeeAttendanceQuota"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendanceQuotas)]
public class EmployeeAttendanceQuotasAppService : HCMv3AppServiceBase, IEmployeeAttendanceQuotasAppService
{
    private readonly IRepository<EmployeeAttendanceQuota> _repository;

    public EmployeeAttendanceQuotasAppService(IRepository<EmployeeAttendanceQuota> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAttendanceQuotaDto>> GetAll(GetAllEmployeeAttendanceQuotasInput input)
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

        return new PagedResultDto<EmployeeAttendanceQuotaDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAttendanceQuotaForEditOutput> GetEmployeeAttendanceQuotaForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAttendanceQuotaForEditOutput
        {
            EmployeeAttendanceQuota = new CreateOrEditEmployeeAttendanceQuotaDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                EntitlementFrom = entity.EntitlementFrom,
                EntitlementTo = entity.EntitlementTo,
                DeductionFrom = entity.DeductionFrom,
                DeductionTo = entity.DeductionTo,
                QuotaAccrued = entity.QuotaAccrued,
                QuotaUsed = entity.QuotaUsed
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAttendanceQuotaDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendanceQuotas_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAttendanceQuotaDto input)
    {
        var entity = MapToEntity(input, new EmployeeAttendanceQuota());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateEntity(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendanceQuotas_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAttendanceQuotaDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        ValidateEntity(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendanceQuotas_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private void ValidateEntity(EmployeeAttendanceQuota entity)
    {
        if (entity.EntitlementTo < entity.EntitlementFrom)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeAttendanceQuota MapToEntity(CreateOrEditEmployeeAttendanceQuotaDto input, EmployeeAttendanceQuota entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.EntitlementFrom = input.EntitlementFrom.Date;
        entity.EntitlementTo = input.EntitlementTo.Date;
        entity.DeductionFrom = input.DeductionFrom.Date;
        entity.DeductionTo = input.DeductionTo.Date;
        entity.QuotaAccrued = System.Math.Round(input.QuotaAccrued, 3);
        entity.QuotaUsed = System.Math.Round(input.QuotaUsed, 3);
        return entity;
    }

    private static EmployeeAttendanceQuotaDto ToDto(EmployeeAttendanceQuota entity)
    {
        return new EmployeeAttendanceQuotaDto
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
            QuotaBalance = System.Math.Round(entity.QuotaAccrued - entity.QuotaUsed, 2)
        };
    }
}
