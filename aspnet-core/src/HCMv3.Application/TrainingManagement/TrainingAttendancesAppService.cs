using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TrainingManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TrainingManagement;

/// <summary>
/// Application service for <see cref="TrainingAttendance"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.TrainingAttendance</c>). Legacy
/// <c>PerformValidation</c> parity: DayDate must fall within the parent TrainingSchedule's
/// Start/EndDate, and no duplicate Employee+TrainingSchedule+DayDate row. The legacy
/// "TrainingSchedule/Employee/DayDate/Attendance should not be blank" checks are covered by
/// <c>[Required]</c> on <see cref="CreateOrEditTrainingAttendanceDto"/> instead.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingAttendances)]
public class TrainingAttendancesAppService : HCMv3AppServiceBase, ITrainingAttendancesAppService
{
    private readonly IRepository<TrainingAttendance> _trainingAttendanceRepository;
    private readonly IRepository<TrainingSchedule> _trainingScheduleRepository;

    public TrainingAttendancesAppService(
        IRepository<TrainingAttendance> trainingAttendanceRepository,
        IRepository<TrainingSchedule> trainingScheduleRepository)
    {
        _trainingAttendanceRepository = trainingAttendanceRepository;
        _trainingScheduleRepository = trainingScheduleRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TrainingAttendanceDto>> GetAll(GetAllTrainingAttendancesInput input)
    {
        input.Normalize();

        var query = _trainingAttendanceRepository.GetAll()
            .Include(a => a.TrainingSchedule)
            .Include(a => a.Employee)
            .WhereIf(input.TrainingScheduleId.HasValue, a => a.TrainingScheduleId == input.TrainingScheduleId)
            .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TrainingAttendanceDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTrainingAttendanceForEditOutput> GetTrainingAttendanceForEdit(EntityDto input)
    {
        var entity = await _trainingAttendanceRepository.GetAsync(input.Id);

        return new GetTrainingAttendanceForEditOutput
        {
            TrainingAttendance = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTrainingAttendanceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingAttendances_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _trainingAttendanceRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingAttendances_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTrainingAttendanceDto input)
    {
        var entity = MapToEntity(input, new TrainingAttendance());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _trainingAttendanceRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TrainingManagement_TrainingAttendances_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTrainingAttendanceDto input)
    {
        var entity = await _trainingAttendanceRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _trainingAttendanceRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: DayDate must fall within the parent TrainingSchedule's
    /// Start/EndDate range, and no duplicate Employee+TrainingSchedule+DayDate row.
    /// </summary>
    private async Task ValidateAsync(TrainingAttendance entity)
    {
        var schedule = await _trainingScheduleRepository.GetAsync(entity.TrainingScheduleId);

        if (entity.DayDate.Date < schedule.StartDate.Date || entity.DayDate.Date > schedule.EndDate.Date)
        {
            throw new UserFriendlyException(L(
                "DayDateMustBeWithinTrainingScheduleRange",
                schedule.StartDate.ToString("dd/MM/yyyy"),
                schedule.EndDate.ToString("dd/MM/yyyy")));
        }

        var duplicate = await _trainingAttendanceRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id &&
                           a.EmployeeId == entity.EmployeeId &&
                           a.TrainingScheduleId == entity.TrainingScheduleId &&
                           a.DayDate == entity.DayDate);
        if (duplicate)
        {
            throw new UserFriendlyException(L("TrainingAttendanceAlreadyExists"));
        }
    }

    private static TrainingAttendance MapToEntity(CreateOrEditTrainingAttendanceDto input, TrainingAttendance entity)
    {
        entity.TrainingScheduleId = input.TrainingScheduleId;
        entity.EmployeeId = input.EmployeeId;
        entity.DayDate = input.DayDate;
        entity.Attendance = input.Attendance;

        return entity;
    }

    private static TrainingAttendanceDto ToDto(TrainingAttendance entity)
    {
        return new TrainingAttendanceDto
        {
            Id = entity.Id,
            TrainingScheduleId = entity.TrainingScheduleId,
            TrainingScheduleNameEn = entity.TrainingSchedule?.NameEn,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            DayDate = entity.DayDate,
            Attendance = entity.Attendance
        };
    }

    private static CreateOrEditTrainingAttendanceDto ToCreateOrEditDto(TrainingAttendance entity)
    {
        return new CreateOrEditTrainingAttendanceDto
        {
            Id = entity.Id,
            TrainingScheduleId = entity.TrainingScheduleId,
            EmployeeId = entity.EmployeeId,
            DayDate = entity.DayDate,
            Attendance = entity.Attendance
        };
    }
}
