using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSecurity;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeAbsenceQuota"/> (the employee "Leave Quota" tab), ported from legacy
/// XAF <c>EmployeeAbsenceQuota</c>. A manual save runs the legacy <c>OnSaving</c> steps in the same order:
/// round the four quota figures to 6 decimals, <c>UpdateQuotaAccrued</c> (track <c>ActualQuotaAccrued</c> and cap
/// the balance at the sub group's <c>MaxLimit</c> when <c>ForfeitRule == None</c>), then
/// <c>UpdateNotificationStatus</c> (re-arm the max-limit notification once the balance drops below its threshold).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas)]
public class EmployeeAbsenceQuotasAppService : HCMv3AppServiceBase, IEmployeeAbsenceQuotasAppService
{
    /// <summary>Legacy system notification that <see cref="EmployeeAbsenceQuota.NotificationSent"/> belongs to.</summary>
    public const string AnnualLeaveMaxLimitNotificationName = "ANNUALLEAVEMAXLIMITREACHED_NOTIFICATION";

    private readonly IRepository<EmployeeAbsenceQuota> _repository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSubGroupAbsence> _subGroupAbsenceRepository;
    private readonly IRepository<SystemNotification> _systemNotificationRepository;
    private readonly ISysSettingsAccessor _sysSettingsAccessor;

    public EmployeeAbsenceQuotasAppService(
        IRepository<EmployeeAbsenceQuota> repository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSubGroupAbsence> subGroupAbsenceRepository,
        IRepository<SystemNotification> systemNotificationRepository,
        ISysSettingsAccessor sysSettingsAccessor)
    {
        _repository = repository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _subGroupAbsenceRepository = subGroupAbsenceRepository;
        _systemNotificationRepository = systemNotificationRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAbsenceQuotaDto>> GetAll(GetAllEmployeeAbsenceQuotasInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(q => q.Employee)
            .Include(q => q.AbsenceType)
            .WhereIf(input.EmployeeId.HasValue, q => q.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAbsenceQuotaDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAbsenceQuotaForEditOutput> GetEmployeeAbsenceQuotaForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAbsenceQuotaForEditOutput
        {
            EmployeeAbsenceQuota = new CreateOrEditEmployeeAbsenceQuotaDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AbsenceTypeId = entity.AbsenceTypeId,
                EntitlementFrom = entity.EntitlementFrom,
                EntitlementTo = entity.EntitlementTo,
                DeductionFrom = entity.DeductionFrom,
                DeductionTo = entity.DeductionTo,
                QuotaAccrued = entity.QuotaAccrued,
                QuotaUsed = entity.QuotaUsed,
                QuotaAdjusted = entity.QuotaAdjusted,
                QuotaForfeited = entity.QuotaForfeited,
                NotificationSent = entity.NotificationSent
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAbsenceQuotaDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAbsenceQuotaDto input)
    {
        var entity = MapToEntity(input, new EmployeeAbsenceQuota());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateEntity(entity);
        // Legacy OldQuotaAccured is 0 for a new record, so ActualQuotaAccrued starts at the entered QuotaAccrued.
        await ApplySaveCalculationsAsync(entity, previousQuotaAccrued: 0);
        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAbsenceQuotaDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        // Legacy OldQuotaAccured = the QuotaAccrued as loaded (i.e. as last saved, after any cap).
        var previousQuotaAccrued = entity.QuotaAccrued;
        MapToEntity(input, entity);

        ValidateEntity(entity);
        await ApplySaveCalculationsAsync(entity, previousQuotaAccrued);
        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsenceQuotas_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    /// <summary>Legacy: <c>QuotaAccrued - (QuotaUsed + QuotaAdjusted + QuotaForfeited)</c>, rounded to 2 decimals.</summary>
    public static double CalculateQuotaBalance(EmployeeAbsenceQuota entity)
    {
        return Math.Round(entity.QuotaAccrued - (entity.QuotaUsed + entity.QuotaAdjusted + entity.QuotaForfeited), 2);
    }

    /// <summary>Legacy <c>UpdateQuotaAccrued</c> followed by <c>UpdateNotificationStatus</c>.</summary>
    private async Task ApplySaveCalculationsAsync(EmployeeAbsenceQuota entity, double previousQuotaAccrued)
    {
        // UpdateQuotaAccrued: ActualQuotaAccrued follows every manual change to QuotaAccrued, before any cap.
        entity.ActualQuotaAccrued += entity.QuotaAccrued - previousQuotaAccrued;

        var subGroupAbsence = await FindCurrentSubGroupAbsenceAsync(entity);

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var forfeitRule = timeSettings?.ForfeitRule ?? ForfeitRule.None;
        if (forfeitRule == ForfeitRule.None && subGroupAbsence != null && subGroupAbsence.MaxLimit != 0)
        {
            var balance = CalculateQuotaBalance(entity);
            if (balance > subGroupAbsence.MaxLimit)
            {
                entity.QuotaAccrued -= balance - subGroupAbsence.MaxLimit;
            }
        }

        // UpdateNotificationStatus: once the balance falls below the warning threshold, allow the notification again.
        if (entity.NotificationSent && subGroupAbsence != null && subGroupAbsence.MaxLimitReachNotificationInDays != 0)
        {
            var notificationActive = await _systemNotificationRepository.GetAll()
                .AnyAsync(n => n.NotificationName == AnnualLeaveMaxLimitNotificationName && n.IsActive);

            if (notificationActive && CalculateQuotaBalance(entity) < subGroupAbsence.MaxLimitReachNotificationInDays)
            {
                entity.NotificationSent = false;
            }
        }
    }

    /// <summary>
    /// Legacy: the employee's org assignment covering today gives the sub group; the first
    /// <c>EmployeeSubGroupAbsence</c> row for that sub group and absence type holds MaxLimit and the notification days.
    /// </summary>
    private async Task<EmployeeSubGroupAbsence> FindCurrentSubGroupAbsenceAsync(EmployeeAbsenceQuota entity)
    {
        var today = Clock.Now.Date;
        var subGroupId = await _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == entity.EmployeeId && a.StartDate <= today && a.EndDate >= today)
            .OrderBy(a => a.Id)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefaultAsync();

        if (!subGroupId.HasValue)
        {
            return null;
        }

        return await _subGroupAbsenceRepository.GetAll()
            .Where(a => a.EmployeeSubGroupId == subGroupId.Value && a.AbsenceTypeId == entity.AbsenceTypeId)
            .OrderBy(a => a.Id)
            .FirstOrDefaultAsync();
    }

    private void ValidateEntity(EmployeeAbsenceQuota entity)
    {
        if (entity.EntitlementTo < entity.EntitlementFrom)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static EmployeeAbsenceQuota MapToEntity(CreateOrEditEmployeeAbsenceQuotaDto input, EmployeeAbsenceQuota entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.EntitlementFrom = input.EntitlementFrom.Date;
        entity.EntitlementTo = input.EntitlementTo.Date;
        entity.DeductionFrom = input.DeductionFrom.Date;
        entity.DeductionTo = input.DeductionTo.Date;
        entity.QuotaAccrued = Math.Round(input.QuotaAccrued, 6);
        entity.QuotaUsed = Math.Round(input.QuotaUsed, 6);
        entity.QuotaAdjusted = Math.Round(input.QuotaAdjusted, 6);
        entity.QuotaForfeited = Math.Round(input.QuotaForfeited, 6);
        // NotificationSent is system-managed (hidden on the legacy screen): set by the daily max-limit
        // notification and reset by the save calculations above, never typed in.
        return entity;
    }

    private static EmployeeAbsenceQuotaDto ToDto(EmployeeAbsenceQuota entity)
    {
        return new EmployeeAbsenceQuotaDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            EntitlementFrom = entity.EntitlementFrom,
            EntitlementTo = entity.EntitlementTo,
            DeductionFrom = entity.DeductionFrom,
            DeductionTo = entity.DeductionTo,
            QuotaAccrued = entity.QuotaAccrued,
            ActualQuotaAccrued = entity.ActualQuotaAccrued,
            QuotaUsed = entity.QuotaUsed,
            QuotaAdjusted = entity.QuotaAdjusted,
            QuotaForfeited = entity.QuotaForfeited,
            NotificationSent = entity.NotificationSent,
            QuotaBalance = CalculateQuotaBalance(entity)
        };
    }
}
