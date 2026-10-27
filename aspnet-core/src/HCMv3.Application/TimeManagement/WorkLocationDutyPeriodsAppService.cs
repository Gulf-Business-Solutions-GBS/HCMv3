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
/// Application service for <see cref="WorkLocationDutyPeriod"/> (ported from legacy XAF
/// WorkLocationDutyPeriod business object, Wave 3). Ports the legacy EndDate auto-chaining
/// (<c>SetEndDate</c>) and the no-duplicate-StartDate-per-WorkLocation validation, following the same
/// pattern already established by <c>EmployeeOrgAssignmentsAppService</c> (PersonnelAdministration Wave B)
/// and <see cref="EmployeeDutyPeriodsAppService"/> -- keyed by WorkLocation instead of Employee.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_WorkLocationDutyPeriods)]
public class WorkLocationDutyPeriodsAppService : HCMv3AppServiceBase, IWorkLocationDutyPeriodsAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<WorkLocationDutyPeriod> _repository;

    public WorkLocationDutyPeriodsAppService(IRepository<WorkLocationDutyPeriod> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkLocationDutyPeriodDto>> GetAll(GetAllWorkLocationDutyPeriodsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(w => w.WorkLocation)
            .Include(w => w.DutyPeriod)
            .WhereIf(input.WorkLocationId.HasValue, w => w.WorkLocationId == input.WorkLocationId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkLocationDutyPeriodDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWorkLocationDutyPeriodForEditOutput> GetWorkLocationDutyPeriodForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetWorkLocationDutyPeriodForEditOutput
        {
            WorkLocationDutyPeriod = new CreateOrEditWorkLocationDutyPeriodDto
            {
                Id = entity.Id,
                WorkLocationId = entity.WorkLocationId,
                DutyPeriodId = entity.DutyPeriodId,
                StartDate = entity.StartDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditWorkLocationDutyPeriodDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_WorkLocationDutyPeriods_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkLocationDutyPeriodDto input)
    {
        var entity = new WorkLocationDutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 0,
            WorkLocationId = input.WorkLocationId,
            DutyPeriodId = input.DutyPeriodId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await FlushAsync();

        await SetEndDatesAsync(entity.WorkLocationId);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_WorkLocationDutyPeriods_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkLocationDutyPeriodDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.WorkLocationId = input.WorkLocationId;
        entity.DutyPeriodId = input.DutyPeriodId;
        entity.StartDate = input.StartDate.Date;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await FlushAsync();

        await SetEndDatesAsync(entity.WorkLocationId);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_WorkLocationDutyPeriods_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        var workLocationId = entity.WorkLocationId;

        await _repository.DeleteAsync(entity.Id);
        await FlushAsync();

        await SetEndDatesAsync(workLocationId);
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
    /// Legacy <c>PerformValidation</c> parity: WorkLocation/DutyPeriod/StartDate required, no two records
    /// for the same work location may share a StartDate.
    /// </summary>
    private async Task ValidateAsync(WorkLocationDutyPeriod entity)
    {
        if (entity.WorkLocationId <= 0)
        {
            throw new UserFriendlyException(L("WorkLocationCannotBeBlank"));
        }

        if (entity.DutyPeriodId <= 0)
        {
            throw new UserFriendlyException(L("DutyPeriodCannotBeBlank"));
        }

        var duplicateStartDate = await _repository.GetAll()
            .AnyAsync(w => w.Id != entity.Id
                           && w.WorkLocationId == entity.WorkLocationId
                           && w.StartDate == entity.StartDate);
        if (duplicateStartDate)
        {
            throw new UserFriendlyException(L("StartDateAlreadyExists", entity.StartDate.ToString("dd/MM/yyyy")));
        }
    }

    /// <summary>
    /// Legacy <c>SetEndDate</c> parity: for each remaining record of the work location (ordered by
    /// StartDate), EndDate = (next record's StartDate - 1 day), or infinite for the last one.
    /// </summary>
    /// <summary>
    /// Property-injected: this service is built by the container, and a chained row must be marked before it is
    /// saved. See <see cref="IPayrollRetroTrigger.MarkEndDateAutoChange"/>.
    /// </summary>
    public IPayrollRetroTrigger RetroTrigger { get; set; }

    private async Task SetEndDatesAsync(int workLocationId)
    {
        var records = await _repository.GetAll()
            .Where(w => w.WorkLocationId == workLocationId)
            .OrderBy(w => w.StartDate)
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

    private static WorkLocationDutyPeriodDto ToDto(WorkLocationDutyPeriod entity)
    {
        return new WorkLocationDutyPeriodDto
        {
            Id = entity.Id,
            WorkLocationId = entity.WorkLocationId,
            WorkLocationName = entity.WorkLocation?.NameEn,
            DutyPeriodId = entity.DutyPeriodId,
            DutyPeriodName = entity.DutyPeriod?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
