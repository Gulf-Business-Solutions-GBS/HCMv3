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
using HCMv3.Storage;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for <see cref="TimeEvent"/> (ported from legacy XAF TimeEvent business object,
/// Wave 4). See the entity class remarks for the significant simplifications: no <c>Photo</c>/geo-selfie
/// resolution, no <c>AllowDuplicateInTimeEvent</c>-setting-driven duplicate-check branching, and no
/// <c>OnDeleting</c> EmployeeAttendance/retro-calc cascade.
///
/// Ports the legacy <c>PerformValidation</c>: Employee/TransType/TransDate required, no duplicate
/// (Employee, TransType, TransDate, TransTime), and (when TransSource == Machine) RecordSource required +
/// unique among Machine-sourced rows.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEvents)]
public class TimeEventsAppService : HCMv3AppServiceBase, ITimeEventsAppService
{
    private readonly IRepository<TimeEvent> _repository;
    private readonly IBinaryObjectManager _binaryObjectManager;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public TimeEventsAppService(
        IRepository<TimeEvent> repository,
        IBinaryObjectManager binaryObjectManager,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _repository = repository;
        _binaryObjectManager = binaryObjectManager;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [HttpPost]
    public async Task<PagedResultDto<TimeEventDto>> GetAll(GetAllTimeEventsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .Include(e => e.ActualTransType)
            .Include(e => e.TransType)
            .Include(e => e.AccessUnit)
            .Include(e => e.WorkLocation)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value)
            .WhereIf(input.TransDateFrom.HasValue, e => e.TransDate >= input.TransDateFrom.Value)
            .WhereIf(input.TransDateTo.HasValue, e => e.TransDate <= input.TransDateTo.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TimeEventDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetTimeEventForEditOutput> GetTimeEventForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetTimeEventForEditOutput
        {
            TimeEvent = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTimeEventDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEvents_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTimeEventDto input)
    {
        var entity = MapToEntity(input, new TimeEvent());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEvents_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTimeEventDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimeEvents_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// The selfie taken with a geo punch, for HR checking who actually punched. Stored on disk under the
    /// tenant's folder (<c>TimeEvent.GeoSelfiePath</c> holds its id) and returned as a data URL for an
    /// <c>img</c> tag, or with no URL when the punch has none.
    /// </summary>
    public async Task<TimeEventSelfieDto> GetTimeEventSelfie(EntityDto input)
    {
        var path = await _repository.GetAll()
            .Where(e => e.Id == input.Id)
            .Select(e => e.GeoSelfiePath)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(path) || !Guid.TryParse(path, out var selfieId))
        {
            return new TimeEventSelfieDto();
        }

        var file = await _binaryObjectManager.GetOrNullAsync(selfieId);
        if (file?.Bytes == null || file.Bytes.Length == 0)
        {
            // The punch names a selfie that is no longer in the file store. Reporting "none" is the honest
            // answer; an empty data URL would render as a broken image.
            return new TimeEventSelfieDto();
        }

        // The geo punch stores "GeoPunchSelfie|<content type>" as the description.
        var separator = file.Description?.IndexOf('|') ?? -1;
        var contentType = separator >= 0 ? file.Description.Substring(separator + 1) : "image/jpeg";

        return new TimeEventSelfieDto { DataUrl = $"data:{contentType};base64,{Convert.ToBase64String(file.Bytes)}" };
    }

    private static TimeEvent MapToEntity(CreateOrEditTimeEventDto input, TimeEvent entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.PIN = input.PIN;
        entity.ActualTransTypeId = input.ActualTransTypeId;
        entity.TransTypeId = input.TransTypeId;
        entity.TransDate = input.TransDate.Date;
        entity.TransTime = input.TransTime;
        entity.AccessUnitId = input.AccessUnitId;
        entity.IPAddress = input.IPAddress;
        entity.TransSource = input.TransSource;
        entity.RecordSource = input.RecordSource;
        entity.DeviceAgentString = input.DeviceAgentString;
        entity.Latitude = input.Latitude;
        entity.Longitude = input.Longitude;
        entity.RangeDifference = input.RangeDifference;
        entity.PunchInRange = input.PunchInRange;
        entity.Temperature = input.Temperature;
        entity.WorkLocationId = input.WorkLocationId;
        entity.GeoSelfiePath = input.GeoSelfiePath;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity (simplified -- see class remarks): Employee/TransType/TransDate
    /// required; no duplicate (Employee, TransType, TransDate, TransTime); and, when TransSource == Machine,
    /// RecordSource required and unique among Machine-sourced rows.
    /// </summary>
    private async Task ValidateAsync(TimeEvent entity)
    {
        if (entity.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (entity.TransTypeId <= 0)
        {
            throw new UserFriendlyException(L("TransTypeCannotBeBlank"));
        }

        // NOTE: this check runs with the ApprovedOnly filter ON, so it does not see a punch an employee has
        // requested but nobody has granted yet. That is the deliberate reading everywhere else on this
        // screen -- HR should not be shown, or blocked by, a row that is not a punch yet -- but it does
        // leave one narrow window: HR enters the same punch while the request is pending, both pass, and
        // approving the request then leaves the day with two identical events. Closing it here needs a
        // filter opt-out, which needs a unit of work, which this service's tests do not give it; the
        // employee-facing half of the race IS closed, in TimeEventWorkflowTransactionHandler.
        // Legacy TimeEvent.PerformValidation (#4164): with ManageTimeSettings.AllowDuplicateInTimeEvent on, the
        // same punch may arrive from two sources (a machine and a manual entry, say) and only an exact repeat
        // from the SAME source is a duplicate; off, the source is not part of the key.
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var allowDuplicateAcrossSources = timeSettings != null && timeSettings.AllowDuplicateInTimeEvent;

        var duplicateEvent = await _repository.GetAll()
            .AnyAsync(e => e.Id != entity.Id
                           && e.EmployeeId == entity.EmployeeId
                           && e.TransTypeId == entity.TransTypeId
                           && e.TransDate == entity.TransDate
                           && e.TransTime == entity.TransTime
                           && (!allowDuplicateAcrossSources || e.TransSource == entity.TransSource));

        if (duplicateEvent)
        {
            throw new UserFriendlyException(allowDuplicateAcrossSources
                ? L("TimeEventAlreadyExistsForSource",
                    entity.TransDate.ToString("dd/MM/yyyy"), entity.TransTime.ToString("HH:mm:ss"), entity.TransSource)
                : L("TimeEventAlreadyExists",
                    entity.TransDate.ToString("dd/MM/yyyy"), entity.TransTime.ToString("HH:mm:ss")));
        }

        if (entity.TransSource == TransSource.Machine)
        {
            if (entity.RecordSource == 0)
            {
                throw new UserFriendlyException(L("RecordSourceCannotBeBlank"));
            }

            var duplicateRecordSource = await _repository.GetAll()
                .AnyAsync(e => e.Id != entity.Id
                               && e.RecordSource == entity.RecordSource
                               && e.TransSource == TransSource.Machine);
            if (duplicateRecordSource)
            {
                throw new UserFriendlyException(L("RecordSourceAlreadyExists", entity.RecordSource));
            }
        }
    }

    private static TimeEventDto ToDto(TimeEvent entity)
    {
        return new TimeEventDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            PIN = entity.PIN,
            ActualTransTypeId = entity.ActualTransTypeId,
            ActualTransTypeName = entity.ActualTransType?.NameEn,
            TransTypeId = entity.TransTypeId,
            TransTypeName = entity.TransType?.NameEn,
            TransDate = entity.TransDate,
            TransTime = entity.TransTime,
            AccessUnitId = entity.AccessUnitId,
            AccessUnitName = entity.AccessUnit?.NameEn,
            IPAddress = entity.IPAddress,
            TransSource = entity.TransSource,
            RecordSource = entity.RecordSource,
            DeviceAgentString = entity.DeviceAgentString,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            RangeDifference = entity.RangeDifference,
            PunchInRange = entity.PunchInRange,
            Temperature = entity.Temperature,
            WorkLocationId = entity.WorkLocationId,
            WorkLocationName = entity.WorkLocation?.NameEn,
            GeoSelfiePath = entity.GeoSelfiePath
        };
    }

    private static CreateOrEditTimeEventDto ToCreateOrEditDto(TimeEvent entity)
    {
        return new CreateOrEditTimeEventDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            PIN = entity.PIN,
            ActualTransTypeId = entity.ActualTransTypeId,
            TransTypeId = entity.TransTypeId,
            TransDate = entity.TransDate,
            TransTime = entity.TransTime,
            AccessUnitId = entity.AccessUnitId,
            IPAddress = entity.IPAddress,
            TransSource = entity.TransSource,
            RecordSource = entity.RecordSource,
            DeviceAgentString = entity.DeviceAgentString,
            Latitude = entity.Latitude,
            Longitude = entity.Longitude,
            RangeDifference = entity.RangeDifference,
            PunchInRange = entity.PunchInRange,
            Temperature = entity.Temperature,
            WorkLocationId = entity.WorkLocationId,
            GeoSelfiePath = entity.GeoSelfiePath
        };
    }
}
