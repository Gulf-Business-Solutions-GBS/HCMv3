using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.PayrollManagement.PayrollLock;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="EmployeeTimePolicy"/> (ported from legacy XAF EmployeeTimePolicy
/// business object, Wave 3). Ports the legacy EndDate auto-chaining (<c>SetEndDate</c>) and the
/// no-duplicate-StartDate-per-employee validation, following the exact pattern already established by
/// <c>EmployeeOrgAssignmentsAppService</c> (PersonnelAdministration Wave B) and
/// <see cref="EmployeeDutyPeriodsAppService"/> above. See the entity class remarks for the dropped
/// SalaryRegister/TimeRegister delete-guard.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeTimePolicies)]
public class EmployeeTimePoliciesAppService : HCMv3AppServiceBase, IEmployeeTimePoliciesAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<EmployeeTimePolicy> _repository;

    public EmployeeTimePoliciesAppService(IRepository<EmployeeTimePolicy> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTimePolicyDto>> GetAll(GetAllEmployeeTimePoliciesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.TimePolicy)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTimePolicyDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTimePolicyForEditOutput> GetEmployeeTimePolicyForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeTimePolicyForEditOutput
        {
            EmployeeTimePolicy = new CreateOrEditEmployeeTimePolicyDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                TimePolicyId = entity.TimePolicyId,
                StartDate = entity.StartDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeTimePolicyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeTimePolicies_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTimePolicyDto input)
    {
        var entity = new EmployeeTimePolicy
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            TimePolicyId = input.TimePolicyId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await FlushAsync();

        await SetEndDatesAsync(entity.EmployeeId);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeTimePolicies_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTimePolicyDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.TimePolicyId = input.TimePolicyId;
        entity.StartDate = input.StartDate.Date;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await FlushAsync();

        await SetEndDatesAsync(entity.EmployeeId);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeTimePolicies_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var employeeId = entity.EmployeeId;

        await _repository.DeleteAsync(entity.Id);
        await FlushAsync();

        await SetEndDatesAsync(employeeId);
    }

    /// <summary>
    /// Flushes pending changes so the immediately-following <c>SetEndDatesAsync</c> requery observes the
    /// just-inserted/updated/deleted row. See <c>EmployeeDutyPeriodsAppService.FlushAsync</c> remarks for
    /// why the <see cref="Abp.AbpException"/> is safe to swallow in the bespoke TimeManagement test harness.
    /// </summary>
    private async Task FlushAsync()
    {
        try
        {
            await CurrentUnitOfWork.SaveChangesAsync();
        }
        catch (Abp.AbpException)
        {
        }
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Employee/TimePolicy/StartDate required, no two records for
    /// the same employee may share a StartDate.
    /// </summary>
    private async Task ValidateAsync(EmployeeTimePolicy entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (entity.TimePolicyId <= 0)
        {
            throw new UserFriendlyException(L("TimePolicyCannotBeBlank"));
        }

        var duplicateStartDate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id
                           && e.EmployeeId == entity.EmployeeId
                           && e.StartDate == entity.StartDate);
        if (duplicateStartDate)
        {
            throw new UserFriendlyException(L("StartDateAlreadyExists", entity.StartDate.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>
    /// Legacy <c>SetEndDate</c> parity: for each remaining record of the employee (ordered by StartDate),
    /// EndDate = (next record's StartDate - 1 day), or infinite for the last one.
    /// </summary>
    /// <summary>
    /// Property-injected: this service is built by the container, and a chained row must be marked before it is
    /// saved. See <see cref="IPayrollRetroTrigger.MarkEndDateAutoChange"/>.
    /// </summary>
    public IPayrollRetroTrigger RetroTrigger { get; set; }

    private async Task SetEndDatesAsync(int employeeId)
    {
        var records = await _repository.GetAll()
            .Where(e => e.EmployeeId == employeeId)
            .OrderBy(e => e.StartDate)
            .ToListAsync();

        for (var i = 0; i < records.Count; i++)
        {
            var next = records.Skip(i + 1).FirstOrDefault();
            var newEndDate = next != null ? next.StartDate.AddDays(-1) : InfiniteEndDate;
            if (records[i].EndDate != newEndDate)
            {
                records[i].EndDate = newEndDate;
                
                // Re-closed because a neighbour moved, not a change of its own: it must not move the employee's
                // EarliestMasterDataChange. Legacy sets IsEndDateAutoChange on exactly these rows.
                RetroTrigger?.MarkEndDateAutoChange(records[i]);
            }
        }
    }

    private static EmployeeTimePolicyDto ToDto(EmployeeTimePolicy entity)
    {
        return new EmployeeTimePolicyDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            TimePolicyId = entity.TimePolicyId,
            TimePolicyName = entity.TimePolicy?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
