using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="EmployeeSubstituteDuty"/> (ported from legacy XAF
/// EmployeeSubstituteDuty business object, Wave 3).
///
/// DELIBERATE, SIGNIFICANT SIMPLIFICATION: see <see cref="EmployeeSubstituteDuty"/> class remarks -- the
/// legacy leave-quota-recalculation cascade (<c>RecalculateTotalLeaveDays</c>/<c>CheckQuotaBalance</c>,
/// touching <c>EmployeeAbsence</c>/<c>EmployeeAbsenceQuota</c>/<c>EmployeeSubGroupAbsence</c>/
/// <c>TimeTransfer</c>) is NOT ported. Only the entity shape and the legacy <c>PerformValidation</c>
/// duplicate (Employee, SubstituteDate) check are ported here.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties)]
public class EmployeeSubstituteDutiesAppService : HCMv3AppServiceBase, IEmployeeSubstituteDutiesAppService
{
    private readonly IRepository<EmployeeSubstituteDuty> _repository;

    public EmployeeSubstituteDutiesAppService(IRepository<EmployeeSubstituteDuty> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeSubstituteDutyDto>> GetAll(GetAllEmployeeSubstituteDutiesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.Duty)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeSubstituteDutyDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeSubstituteDutyForEditOutput> GetEmployeeSubstituteDutyForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeSubstituteDutyForEditOutput
        {
            EmployeeSubstituteDuty = new CreateOrEditEmployeeSubstituteDutyDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                DutyId = entity.DutyId,
                SubstituteDate = entity.SubstituteDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeSubstituteDutyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeSubstituteDutyDto input)
    {
        var entity = new EmployeeSubstituteDuty
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            DutyId = input.DutyId,
            SubstituteDate = input.SubstituteDate.Date
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeSubstituteDutyDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.DutyId = input.DutyId;
        entity.SubstituteDate = input.SubstituteDate.Date;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeSubstituteDuties_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Employee/Duty/SubstituteDate required, no two records for
    /// the same employee may share a SubstituteDate.
    /// </summary>
    private async Task ValidateAsync(EmployeeSubstituteDuty entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (entity.DutyId <= 0)
        {
            throw new UserFriendlyException(L("DutyCannotBeBlank"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id
                           && e.EmployeeId == entity.EmployeeId
                           && e.SubstituteDate == entity.SubstituteDate);
        if (duplicate)
        {
            throw new UserFriendlyException(L("SubstituteDateAlreadyExists", entity.SubstituteDate.ToString("dd/MM/yyyy")));
        }
    }

    private static EmployeeSubstituteDutyDto ToDto(EmployeeSubstituteDuty entity)
    {
        return new EmployeeSubstituteDutyDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DutyId = entity.DutyId,
            DutyName = entity.Duty?.NameEn,
            SubstituteDate = entity.SubstituteDate
        };
    }
}
