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
using HCMv3.PersonnelAdministration.Services;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.Workflow.Services;
using HCMv3.PersonnelAdministration.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeAttendance"/> (ported from legacy XAF EmployeeAttendance
/// business object, Wave 1 deferred bucket). Ports the essential active validations only (required fields,
/// EndDate not earlier than StartDate, EndTime not earlier than StartTime, no-duplicate-attendance-period
/// for the same employee for <see cref="AttendanceUnit.Period"/> entries).
///
/// <para>
/// <c>TotalAttendDays</c> for the Period and Hours units (derived from hours through the employee's duty /
/// roster) and the accrual into the attendance type's linked <see cref="EmployeeAbsenceQuota"/> are legacy
/// <c>EmployeeAttendance.UpdateAttendanceQuota</c> / <c>OnDeleting</c>, ported in
/// <see cref="IEmployeeAttendanceCalculationService"/>. As with leave, only an EFFECTIVE row (entered by HR, or
/// approved) moves the quota; a row awaiting approval is accrued by the workflow handler when it is approved.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances)]
public class EmployeeAttendancesAppService : HCMv3AppServiceBase, IEmployeeAttendancesAppService
{
    private readonly IRepository<EmployeeAttendance> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;
    private readonly IEmployeeAbsenceCalculationService _absenceCalculationService;
    private readonly IEmployeeAttendanceCalculationService _attendanceCalculationService;

    public EmployeeAttendancesAppService(
        IRepository<EmployeeAttendance> repository,
        ISysSettingsAccessor sysSettingsAccessor,
        IEmployeeAbsenceCalculationService absenceCalculationService,
        IEmployeeAttendanceCalculationService attendanceCalculationService,
        IAdminTransactionSubmitter transactionSubmitter)
    {
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _absenceCalculationService = absenceCalculationService;
        _attendanceCalculationService = attendanceCalculationService;
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
                TotalAttendDays = entity.TotalAttendDays,
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

        // Legacy OnSaving order (lines 177-178): PerformValidation, then UpdateAttendanceQuota.
        await ValidateAsync(entity);
        await CalculateTotalAttendDaysAsync(entity);

        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            AttendanceWorkflowTransactionHandler.AttendanceRequestType,
            "ATT",
            entity.AttendanceTypeId,
            input.RecordDirectly);

        // Legacy lines 411-436. A new row has nothing to give back (legacy Oid == -1, line 385).
        if (IsEffective(entity))
        {
            await _attendanceCalculationService.AccrueAttendanceQuotaAsync(entity);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAttendanceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        // Legacy OnLoaded (lines 155-163): what the quota currently carries for this row.
        var previous = AttendanceQuotaSnapshot.Of(entity);
        var wasEffective = IsEffective(entity);

        MapToEntity(input, entity);

        await ValidateAsync(entity);
        await CalculateTotalAttendDaysAsync(entity);
        await _repository.UpdateAsync(entity);

        // Legacy lines 383-436: give back what the row accrued before, then accrue what it accrues now.
        if (wasEffective)
        {
            await _attendanceCalculationService.ReleaseAttendanceQuotaAsync(previous);
        }

        if (IsEffective(entity))
        {
            await _attendanceCalculationService.AccrueAttendanceQuotaAsync(entity);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAttendances_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.FirstOrDefaultAsync(input.Id);
        if (entity == null)
        {
            return;
        }

        // Legacy OnDeleting (lines 309-343): the accrued days come off the quota.
        if (IsEffective(entity))
        {
            await _attendanceCalculationService.ReleaseAttendanceQuotaAsync(AttendanceQuotaSnapshot.Of(entity));
        }

        await _repository.DeleteAsync(entity);
    }

    /// <summary>
    /// Whether the row counts today: entered by HR (no workflow status) or approved -- the same test the leave
    /// screen uses. A Draft, Open or Rejected row has accrued nothing.
    /// </summary>
    private static bool IsEffective(EmployeeAttendance entity)
    {
        return entity.WorkflowStatus is null
               or HCMv3.Workflow.RequestStatus.Approved
               or HCMv3.Workflow.RequestStatus.Closed;
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

        await EnsureStartDayAllowsAttendanceAsync(entity);

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

    /// <summary>
    /// Legacy <c>EmployeeAttendance.GetPlannedminutes</c> (AMK ticket #2267): attendance may not start on a
    /// day whose duty is a weekly off unless <c>ManageTimeSettings.EnableAttendanceOnWeekOff</c> is on, nor on
    /// a rest day unless <c>EnableAttendanceOnRestDay</c> is on. A day with no resolvable duty is left to the
    /// rest of the validation, as legacy's throw-on-missing-duty belonged to the minutes calculation.
    /// </summary>
    private async Task EnsureStartDayAllowsAttendanceAsync(EmployeeAttendance entity)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings == null || (timeSettings.EnableAttendanceOnWeekOff && timeSettings.EnableAttendanceOnRestDay))
        {
            return;
        }

        Duty duty;
        try
        {
            duty = await _absenceCalculationService.GetEmployeeDutyAsync(entity.EmployeeId, entity.StartDate.Date);
        }
        catch (System.InvalidOperationException)
        {
            return;
        }

        if (duty == null)
        {
            return;
        }

        if (duty.IsWeeklyOff && !timeSettings.EnableAttendanceOnWeekOff)
        {
            throw new UserFriendlyException(L("AttendanceCannotStartOnWeekOff",
                entity.StartDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture), duty.NameEn));
        }

        if (duty.IsRestDay && !timeSettings.EnableAttendanceOnRestDay)
        {
            throw new UserFriendlyException(L("AttendanceCannotStartOnRestDay",
                entity.StartDate.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture), duty.NameEn));
        }
    }

    private async Task CalculateTotalAttendDaysAsync(EmployeeAttendance entity)
    {
        if (entity.AttendanceUnit == AttendanceUnit.Day)
        {
            // HCMv3's existing rule for the Day unit, left unchanged by this port. (Legacy kept the TotalAttendDays
            // the user typed for Day and forced EndDate = StartDate, lines 239-242 / 368-371.)
            entity.TotalAttendDays = entity.EndDate.Subtract(entity.StartDate).TotalDays + 1;
            return;
        }

        // Period and Hours: legacy UpdateAttendanceQuota lines 348-381.
        await _attendanceCalculationService.ApplyTotalAttendDaysAsync(entity);
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
