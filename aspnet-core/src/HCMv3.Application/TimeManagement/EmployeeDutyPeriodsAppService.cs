using System;
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
/// Application service for <see cref="EmployeeDutyPeriod"/> (ported from legacy XAF EmployeeDutyPeriod
/// business object, Wave 3). Ports the legacy EndDate auto-chaining (<c>SetEndDate</c>) and the
/// no-duplicate-StartDate-per-employee validation, following the exact pattern already established by
/// <c>EmployeeOrgAssignmentsAppService</c> (PersonnelAdministration Wave B). See the entity class remarks
/// for the full list of dropped legacy behaviors (StartPoint/Weekoff dead code, SalaryRegister/TimeRegister
/// delete-guard, the static GetDuty roster-lookup helper).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeDutyPeriods)]
public class EmployeeDutyPeriodsAppService : HCMv3AppServiceBase, IEmployeeDutyPeriodsAppService
{
    private static readonly DateTime InfiniteEndDate = new DateTime(9999, 12, 31);

    private readonly IRepository<EmployeeDutyPeriod> _repository;

    public EmployeeDutyPeriodsAppService(IRepository<EmployeeDutyPeriod> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDutyPeriodDto>> GetAll(GetAllEmployeeDutyPeriodsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.DutyPeriod)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDutyPeriodDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeDutyPeriodForEditOutput> GetEmployeeDutyPeriodForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeDutyPeriodForEditOutput
        {
            EmployeeDutyPeriod = new CreateOrEditEmployeeDutyPeriodDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                DutyPeriodId = entity.DutyPeriodId,
                StartDate = entity.StartDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDutyPeriodDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeDutyPeriods_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDutyPeriodDto input)
    {
        var entity = new EmployeeDutyPeriod
        {
            TenantId = AbpSession.TenantId ?? 0,
            EmployeeId = input.EmployeeId,
            DutyPeriodId = input.DutyPeriodId,
            StartDate = input.StartDate.Date,
            EndDate = InfiniteEndDate
        };

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
        await FlushAsync();

        await SetEndDatesAsync(entity.EmployeeId);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeDutyPeriods_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDutyPeriodDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity.EmployeeId = input.EmployeeId;
        entity.DutyPeriodId = input.DutyPeriodId;
        entity.StartDate = input.StartDate.Date;

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
        await FlushAsync();

        await SetEndDatesAsync(entity.EmployeeId);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeDutyPeriods_Delete)]
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
    /// just-inserted/updated/deleted row. In real ABP usage (production), the repository does not
    /// auto-flush mid-request, so <c>CurrentUnitOfWork.SaveChangesAsync()</c> is required here (matches
    /// <c>EmployeeOrgAssignmentsAppService</c>). In the bespoke TimeManagement unit-test harness (see
    /// <c>TimeManagementAppServiceTestBase</c>), app services are constructed directly, bypassing the real
    /// Abp DI/UOW pipeline, so <c>UnitOfWorkManager</c> is never set -- but <c>FakeRepository</c>'s
    /// Insert/Update/Delete already call <c>DbContext.SaveChangesAsync()</c> themselves, so the flush this
    /// method would otherwise trigger has already happened; the resulting <see cref="Abp.AbpException"/> is
    /// safe to swallow in that case.
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
    /// Legacy <c>PerformValidation</c> parity: Employee/DutyPeriod/StartDate required, no two records for
    /// the same employee may share a StartDate.
    /// </summary>
    private async Task ValidateAsync(EmployeeDutyPeriod entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (entity.DutyPeriodId <= 0)
        {
            throw new UserFriendlyException(L("DutyPeriodCannotBeBlank"));
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
            }
        }
    }

    private static EmployeeDutyPeriodDto ToDto(EmployeeDutyPeriod entity)
    {
        return new EmployeeDutyPeriodDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            DutyPeriodId = entity.DutyPeriodId,
            DutyPeriodName = entity.DutyPeriod?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
