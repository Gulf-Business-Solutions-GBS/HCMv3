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
/// Application service for <see cref="EmployeeTimesheet"/> (ported from legacy XAF EmployeeTimesheet
/// business object, Wave 3). Simple manual timesheet entry, one row per (Employee, TransDate) -- no
/// auto-chaining. Ports the legacy <c>PerformValidation</c> duplicate (Employee, TransDate) check.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeTimesheets)]
public class EmployeeTimesheetsAppService : HCMv3AppServiceBase, IEmployeeTimesheetsAppService
{
    private readonly IRepository<EmployeeTimesheet> _repository;

    public EmployeeTimesheetsAppService(IRepository<EmployeeTimesheet> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeTimesheetDto>> GetAll(GetAllEmployeeTimesheetsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeTimesheetDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeTimesheetForEditOutput> GetEmployeeTimesheetForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeTimesheetForEditOutput
        {
            EmployeeTimesheet = new CreateOrEditEmployeeTimesheetDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                TransDate = entity.TransDate,
                ActualStartTime = entity.ActualStartTime,
                BreakStartTime = entity.BreakStartTime,
                BreakEndTime = entity.BreakEndTime,
                ActualEndTime = entity.ActualEndTime,
                TotalHours = entity.TotalHours,
                Approved = entity.Approved,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeTimesheetDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeTimesheets_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeTimesheetDto input)
    {
        var entity = MapToEntity(input, new EmployeeTimesheet());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeTimesheets_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeTimesheetDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_EmployeeTimesheets_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeTimesheet MapToEntity(CreateOrEditEmployeeTimesheetDto input, EmployeeTimesheet entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.TransDate = input.TransDate.Date;
        entity.ActualStartTime = input.ActualStartTime;
        entity.BreakStartTime = input.BreakStartTime;
        entity.BreakEndTime = input.BreakEndTime;
        entity.ActualEndTime = input.ActualEndTime;
        entity.TotalHours = input.TotalHours;
        entity.Approved = input.Approved;
        entity.Comments = input.Comments;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Employee/TransDate required, no two records for the same
    /// employee may share a TransDate.
    /// </summary>
    private async Task ValidateAsync(EmployeeTimesheet entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id
                           && e.EmployeeId == entity.EmployeeId
                           && e.TransDate == entity.TransDate);
        if (duplicate)
        {
            throw new UserFriendlyException(L("TimesheetAlreadyExists", entity.TransDate.ToString("dd/MM/yyyy")));
        }
    }

    private static EmployeeTimesheetDto ToDto(EmployeeTimesheet entity)
    {
        return new EmployeeTimesheetDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            TransDate = entity.TransDate,
            ActualStartTime = entity.ActualStartTime,
            BreakStartTime = entity.BreakStartTime,
            BreakEndTime = entity.BreakEndTime,
            ActualEndTime = entity.ActualEndTime,
            TotalHours = entity.TotalHours,
            Approved = entity.Approved,
            Comments = entity.Comments
        };
    }
}
