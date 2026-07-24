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
/// Application service for <see cref="EmployeeAttendance"/> (ported from legacy XAF EmployeeAttendance
/// business object, Wave 1 deferred bucket). Ports the essential active validations only (required fields,
/// EndDate not earlier than StartDate, EndTime not earlier than StartTime, no-duplicate-attendance-period
/// for the same employee for <see cref="AttendanceUnit.Period"/> entries). The legacy hourly-based
/// duty/roster lookup to derive <c>TotalAttendDays</c> from hours, and the auto-accrual cascade into a
/// linked <see cref="EmployeeAbsenceQuota"/>, are NOT ported here -- see class remarks on
/// <see cref="EmployeeAttendance"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances)]
public class EmployeeAttendancesAppService : HCMv3AppServiceBase, IEmployeeAttendancesAppService
{
    private readonly IRepository<EmployeeAttendance> _repository;

    public EmployeeAttendancesAppService(IRepository<EmployeeAttendance> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAttendanceDto>> GetAll(GetAllEmployeeAttendancesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.Employee)
            .Include(a => a.AttendanceType)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAttendanceDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAttendanceForEditOutput> GetEmployeeAttendanceForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAttendanceForEditOutput
        {
            EmployeeAttendance = new CreateOrEditEmployeeAttendanceDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AttendanceTypeId = entity.AttendanceTypeId,
                AttendanceUnit = entity.AttendanceUnit,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                AttendanceHours = entity.AttendanceHours,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAttendanceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAttendanceDto input)
    {
        var entity = MapToEntity(input, new EmployeeAttendance());
        entity.TenantId = AbpSession.TenantId ?? 0;
        entity.TotalAttendDays = CalculateTotalAttendDays(entity);

        await ValidateAsync(entity);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAttendanceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);
        entity.TotalAttendDays = CalculateTotalAttendDays(entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private async Task ValidateAsync(EmployeeAttendance entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        if (entity.AttendanceUnit == AttendanceUnit.Period && entity.EndTime < entity.StartTime)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        if (entity.AttendanceUnit == AttendanceUnit.Period)
        {
            var overlaps = await _repository.GetAll()
                .AnyAsync(a => a.Id != entity.Id
                               && a.EmployeeId == entity.EmployeeId
                               && a.AttendanceUnit == AttendanceUnit.Period
                               && a.StartDate.Date <= entity.EndDate.Date
                               && a.EndDate.Date >= entity.StartDate.Date);
            if (overlaps)
            {
                throw new UserFriendlyException(L("OverlapConflictFound"));
            }
        }
    }

    private static double CalculateTotalAttendDays(EmployeeAttendance entity)
    {
        if (entity.AttendanceUnit == AttendanceUnit.Day)
        {
            return entity.EndDate.Subtract(entity.StartDate).TotalDays + 1;
        }

        // Period/Hours-based day-count derivation from duty/roster is not ported -- see class remarks.
        return entity.TotalAttendDays;
    }

    private static EmployeeAttendance MapToEntity(CreateOrEditEmployeeAttendanceDto input, EmployeeAttendance entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AttendanceTypeId = input.AttendanceTypeId;
        entity.AttendanceUnit = input.AttendanceUnit;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.StartTime = input.StartTime;
        entity.EndTime = input.EndTime;
        entity.AttendanceHours = input.AttendanceHours;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeAttendanceDto ToDto(EmployeeAttendance entity)
    {
        return new EmployeeAttendanceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AttendanceTypeId = entity.AttendanceTypeId,
            AttendanceTypeName = entity.AttendanceType?.NameEn,
            AttendanceUnit = entity.AttendanceUnit,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            AttendanceHours = entity.AttendanceHours,
            TotalAttendDays = entity.TotalAttendDays,
            Comments = entity.Comments
        };
    }
}
