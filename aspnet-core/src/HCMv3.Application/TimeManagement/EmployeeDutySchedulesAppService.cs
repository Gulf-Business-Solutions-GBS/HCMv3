using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="EmployeeDutySchedule"/> (ported from legacy XAF EmployeeDutySchedule
/// business object, Wave 3). The legacy <c>IEvent</c> calendar-widget properties are not ported (see entity
/// class remarks). The legacy <c>PerformValidation</c> checks StartDate/EndDate presence, EndDate not
/// earlier than StartDate, and a conflicting (overlapping) date-range check for the same employee -- all
/// ported here, following the same pattern used for <see cref="RosterGroupDetail"/> (Wave 2).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeDutySchedules)]
public class EmployeeDutySchedulesAppService : HCMv3AppServiceBase, IEmployeeDutySchedulesAppService
{
    private readonly IRepository<EmployeeDutySchedule> _repository;

    public EmployeeDutySchedulesAppService(IRepository<EmployeeDutySchedule> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeDutyScheduleDto>> GetAll(GetAllEmployeeDutySchedulesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.RosterDutySchedule)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), e =>
                e.Employee.Code.Contains(input.Filter) ||
                e.Employee.FullNameEn.Contains(input.Filter) ||
                e.Employee.FullNameAr.Contains(input.Filter) ||
                e.RosterDutySchedule.Code.Contains(input.Filter) ||
                e.RosterDutySchedule.NameEn.Contains(input.Filter) ||
                e.RosterDutySchedule.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeDutyScheduleDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeDutyScheduleForEditOutput> GetEmployeeDutyScheduleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeDutyScheduleForEditOutput
        {
            EmployeeDutySchedule = new CreateOrEditEmployeeDutyScheduleDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                RosterDutyScheduleId = entity.RosterDutyScheduleId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeDutyScheduleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeDutySchedules_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeDutyScheduleDto input)
    {
        var entity = MapToEntity(input, new EmployeeDutySchedule());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeDutySchedules_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeDutyScheduleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeDutySchedules_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeDutySchedule MapToEntity(CreateOrEditEmployeeDutyScheduleDto input, EmployeeDutySchedule entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.RosterDutyScheduleId = input.RosterDutyScheduleId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: StartDate/EndDate required, EndDate not earlier than
    /// StartDate, and no conflicting (overlapping) date range for the same employee.
    /// </summary>
    private async Task ValidateAsync(EmployeeDutySchedule entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var overlaps = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id &&
                           e.EmployeeId == entity.EmployeeId &&
                           e.StartDate.Date <= entity.EndDate.Date &&
                           e.EndDate.Date >= entity.StartDate.Date);
        if (overlaps)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }
    }

    private static EmployeeDutyScheduleDto ToDto(EmployeeDutySchedule entity)
    {
        return new EmployeeDutyScheduleDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            RosterDutyScheduleId = entity.RosterDutyScheduleId,
            RosterDutyScheduleName = entity.RosterDutySchedule?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate
        };
    }
}
