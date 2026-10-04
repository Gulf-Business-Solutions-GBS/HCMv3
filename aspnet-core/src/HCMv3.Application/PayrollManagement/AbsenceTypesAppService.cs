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
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for the Leave Type master (ported from legacy XAF AbsenceType business object, XAF
/// display name "LeaveType"). Every scalar field is mapped faithfully; see <see cref="AbsenceType"/> class
/// remarks for the FK-related simplifications (WageTypeId has no navigation property; NotificationConfig is
/// not ported).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceTypes)]
public class AbsenceTypesAppService : HCMv3AppServiceBase, IAbsenceTypesAppService
{
    private readonly IRepository<AbsenceType> _absenceTypeRepository;

    public AbsenceTypesAppService(IRepository<AbsenceType> absenceTypeRepository)
    {
        _absenceTypeRepository = absenceTypeRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<AbsenceTypeDto>> GetAll(GetAllAbsenceTypesInput input)
    {
        input.Normalize();

        var query = _absenceTypeRepository.GetAll()
            .Include(a => a.AbsenceViolationRule)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                a.Code.Contains(input.Filter) ||
                a.NameEn.Contains(input.Filter) ||
                a.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AbsenceTypeDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetAbsenceTypeForEditOutput> GetAbsenceTypeForEdit(EntityDto input)
    {
        var entity = await _absenceTypeRepository.GetAsync(input.Id);

        return new GetAbsenceTypeForEditOutput
        {
            AbsenceType = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditAbsenceTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _absenceTypeRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditAbsenceTypeDto input)
    {
        var entity = MapToEntity(input, new AbsenceType());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Legacy AfterConstruction parity: default ResetFrequency to Monthly when not explicitly set.
        if (entity.ResetFrequency == default)
        {
            entity.ResetFrequency = ResetFrequency.Monthly;
        }

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _absenceTypeRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_AbsenceTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditAbsenceTypeDto input)
    {
        var entity = await _absenceTypeRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _absenceTypeRepository.UpdateAsync(entity);
    }

    private static AbsenceType MapToEntity(CreateOrEditAbsenceTypeDto input, AbsenceType entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.IsActive = input.IsActive;
        entity.IsPaid = input.IsPaid;
        entity.IncludeWeekend = input.IncludeWeekend;
        entity.IncludeRestDay = input.IncludeRestDay;
        entity.IncludePublicHoliday = input.IncludePublicHoliday;
        entity.LeaveBasisPeriod = input.LeaveBasisPeriod;
        entity.LeaveBasisFixedDays = input.LeaveBasisFixedDays;
        entity.HaveQuotaAccrual = input.HaveQuotaAccrual;
        entity.AccrualPeriod = input.AccrualPeriod;
        entity.CustomStartMonth = input.CustomStartMonth;
        entity.MinActiveDaysInService = input.MinActiveDaysInService;
        entity.CanApplyAgain = input.CanApplyAgain;
        entity.CanApplyAfterDays = input.CanApplyAfterDays;
        entity.CanApplyBeforeDays = input.CanApplyBeforeDays;
        entity.ReInitialize = input.ReInitialize;
        entity.ReInitializeAfter = input.ReInitializeAfter;
        entity.ApplyLeaveInAdvance = input.ApplyLeaveInAdvance;
        entity.ApplyLeaveInAdvanceDays = input.ApplyLeaveInAdvanceDays;
        entity.ApplyMaxLeave = input.ApplyMaxLeave;
        entity.MaxLimitInDays = input.MaxLimitInDays;
        entity.CreateUnpaidAboveMaxLimitDays = input.CreateUnpaidAboveMaxLimitDays;
        entity.ApplyMinLeave = input.ApplyMinLeave;
        entity.MinLimitInDays = input.MinLimitInDays;
        entity.ResetFrequency = input.ResetFrequency;
        entity.TotalMinutes = input.TotalMinutes;
        entity.MaxMinutes = input.MaxMinutes;
        entity.MaxCount = input.MaxCount;
        entity.WageTypeId = input.WageTypeId;
        entity.PolicyRestrictType = input.PolicyRestrictType;
        entity.IncludeInWorkflow = input.IncludeInWorkflow;
        entity.IsReturnDateRequired = input.IsReturnDateRequired;
        entity.LeaveGender = input.LeaveGender;
        entity.LeaveReligion = input.LeaveReligion;
        entity.ExcludePaidLeaveAccrual = input.ExcludePaidLeaveAccrual;
        entity.QuotaType = input.QuotaType;
        entity.AllowBackDatedDays = input.AllowBackDatedDays;
        entity.IncludeInSalaryProfile = input.IncludeInSalaryProfile;
        entity.TotalDaysAllowed = input.TotalDaysAllowed;
        entity.LeaveEntitlement = input.LeaveEntitlement;
        entity.AllowDuringProbation = input.AllowDuringProbation;
        entity.AllowToReturnEarly = input.AllowToReturnEarly;
        entity.AllowInTerminationNoticeOnly = input.AllowInTerminationNoticeOnly;
        entity.IsAttachmentRequired = input.IsAttachmentRequired;
        entity.AllowPartialLeave = input.AllowPartialLeave;
        entity.LeaveDurationType = input.LeaveDurationType;
        entity.MaxFutureDays = input.MaxFutureDays;
        entity.RequirePolicyAcceptance = input.RequirePolicyAcceptance;
        entity.SendNotification = input.SendNotification;
        entity.DontAllowToStartOnSunday = input.DontAllowToStartOnSunday;
        entity.DontAllowToStartOnMonday = input.DontAllowToStartOnMonday;
        entity.DontAllowToStartOnTuesday = input.DontAllowToStartOnTuesday;
        entity.DontAllowToStartOnWednesday = input.DontAllowToStartOnWednesday;
        entity.DontAllowToStartOnThursday = input.DontAllowToStartOnThursday;
        entity.DontAllowToStartOnFriday = input.DontAllowToStartOnFriday;
        entity.DontAllowToStartOnSaturday = input.DontAllowToStartOnSaturday;
        entity.EnableNegativeBalance = input.EnableNegativeBalance;
        entity.NegativeBalanceLimit = input.NegativeBalanceLimit;
        entity.AbsenceViolationRuleId = input.AbsenceViolationRuleId;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(AbsenceType entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _absenceTypeRepository.GetAll()
            .Where(a => a.Code != null && a.Code != "")
            .Select(a => a.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidations</c> parity: unique Code, unique NameEn/NameAr, ReInitializeAfter &gt; 0
    /// when ReInitialize is enabled, and CustomStartMonth required when AccrualPeriod is Custom.
    /// </summary>
    private async Task ValidateAsync(AbsenceType entity)
    {
        var duplicateCode = await _absenceTypeRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id && a.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _absenceTypeRepository.GetAll()
            .AnyAsync(a => a.Id != entity.Id &&
                           (a.NameEn == entity.NameEn || a.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }

        if (entity.ReInitialize && entity.ReInitializeAfter <= 0)
        {
            throw new UserFriendlyException(L("ReInitializeAfterMustBeGreaterThanZero"));
        }

        if (entity.AccrualPeriod == AccrualPeriod.Custom && entity.CustomStartMonth == CustomStartMonth.None)
        {
            throw new UserFriendlyException(L("CustomStartMonthRequired"));
        }
    }

    private static AbsenceTypeDto ToDto(AbsenceType entity)
    {
        return new AbsenceTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsActive = entity.IsActive,
            IsPaid = entity.IsPaid,
            IncludeWeekend = entity.IncludeWeekend,
            IncludeRestDay = entity.IncludeRestDay,
            IncludePublicHoliday = entity.IncludePublicHoliday,
            LeaveBasisPeriod = entity.LeaveBasisPeriod,
            LeaveBasisFixedDays = entity.LeaveBasisFixedDays,
            HaveQuotaAccrual = entity.HaveQuotaAccrual,
            AccrualPeriod = entity.AccrualPeriod,
            CustomStartMonth = entity.CustomStartMonth,
            MinActiveDaysInService = entity.MinActiveDaysInService,
            CanApplyAgain = entity.CanApplyAgain,
            CanApplyAfterDays = entity.CanApplyAfterDays,
            CanApplyBeforeDays = entity.CanApplyBeforeDays,
            ReInitialize = entity.ReInitialize,
            ReInitializeAfter = entity.ReInitializeAfter,
            ApplyLeaveInAdvance = entity.ApplyLeaveInAdvance,
            ApplyLeaveInAdvanceDays = entity.ApplyLeaveInAdvanceDays,
            ApplyMaxLeave = entity.ApplyMaxLeave,
            MaxLimitInDays = entity.MaxLimitInDays,
            CreateUnpaidAboveMaxLimitDays = entity.CreateUnpaidAboveMaxLimitDays,
            ApplyMinLeave = entity.ApplyMinLeave,
            MinLimitInDays = entity.MinLimitInDays,
            ResetFrequency = entity.ResetFrequency,
            TotalMinutes = entity.TotalMinutes,
            MaxMinutes = entity.MaxMinutes,
            MaxCount = entity.MaxCount,
            WageTypeId = entity.WageTypeId,
            PolicyRestrictType = entity.PolicyRestrictType,
            IncludeInWorkflow = entity.IncludeInWorkflow,
            IsReturnDateRequired = entity.IsReturnDateRequired,
            LeaveGender = entity.LeaveGender,
            LeaveReligion = entity.LeaveReligion,
            ExcludePaidLeaveAccrual = entity.ExcludePaidLeaveAccrual,
            QuotaType = entity.QuotaType,
            AllowBackDatedDays = entity.AllowBackDatedDays,
            IncludeInSalaryProfile = entity.IncludeInSalaryProfile,
            TotalDaysAllowed = entity.TotalDaysAllowed,
            LeaveEntitlement = entity.LeaveEntitlement,
            AllowDuringProbation = entity.AllowDuringProbation,
            AllowToReturnEarly = entity.AllowToReturnEarly,
            AllowInTerminationNoticeOnly = entity.AllowInTerminationNoticeOnly,
            IsAttachmentRequired = entity.IsAttachmentRequired,
            AllowPartialLeave = entity.AllowPartialLeave,
            LeaveDurationType = entity.LeaveDurationType,
            MaxFutureDays = entity.MaxFutureDays,
            RequirePolicyAcceptance = entity.RequirePolicyAcceptance,
            SendNotification = entity.SendNotification,
            DontAllowToStartOnSunday = entity.DontAllowToStartOnSunday,
            DontAllowToStartOnMonday = entity.DontAllowToStartOnMonday,
            DontAllowToStartOnTuesday = entity.DontAllowToStartOnTuesday,
            DontAllowToStartOnWednesday = entity.DontAllowToStartOnWednesday,
            DontAllowToStartOnThursday = entity.DontAllowToStartOnThursday,
            DontAllowToStartOnFriday = entity.DontAllowToStartOnFriday,
            DontAllowToStartOnSaturday = entity.DontAllowToStartOnSaturday,
            EnableNegativeBalance = entity.EnableNegativeBalance,
            NegativeBalanceLimit = entity.NegativeBalanceLimit,
            AbsenceViolationRuleId = entity.AbsenceViolationRuleId,
            AbsenceViolationRuleName = entity.AbsenceViolationRule?.NameEn
        };
    }

    private static CreateOrEditAbsenceTypeDto ToCreateOrEditDto(AbsenceType entity)
    {
        return new CreateOrEditAbsenceTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsActive = entity.IsActive,
            IsPaid = entity.IsPaid,
            IncludeWeekend = entity.IncludeWeekend,
            IncludeRestDay = entity.IncludeRestDay,
            IncludePublicHoliday = entity.IncludePublicHoliday,
            LeaveBasisPeriod = entity.LeaveBasisPeriod,
            LeaveBasisFixedDays = entity.LeaveBasisFixedDays,
            HaveQuotaAccrual = entity.HaveQuotaAccrual,
            AccrualPeriod = entity.AccrualPeriod,
            CustomStartMonth = entity.CustomStartMonth,
            MinActiveDaysInService = entity.MinActiveDaysInService,
            CanApplyAgain = entity.CanApplyAgain,
            CanApplyAfterDays = entity.CanApplyAfterDays,
            CanApplyBeforeDays = entity.CanApplyBeforeDays,
            ReInitialize = entity.ReInitialize,
            ReInitializeAfter = entity.ReInitializeAfter,
            ApplyLeaveInAdvance = entity.ApplyLeaveInAdvance,
            ApplyLeaveInAdvanceDays = entity.ApplyLeaveInAdvanceDays,
            ApplyMaxLeave = entity.ApplyMaxLeave,
            MaxLimitInDays = entity.MaxLimitInDays,
            CreateUnpaidAboveMaxLimitDays = entity.CreateUnpaidAboveMaxLimitDays,
            ApplyMinLeave = entity.ApplyMinLeave,
            MinLimitInDays = entity.MinLimitInDays,
            ResetFrequency = entity.ResetFrequency,
            TotalMinutes = entity.TotalMinutes,
            MaxMinutes = entity.MaxMinutes,
            MaxCount = entity.MaxCount,
            WageTypeId = entity.WageTypeId,
            PolicyRestrictType = entity.PolicyRestrictType,
            IncludeInWorkflow = entity.IncludeInWorkflow,
            IsReturnDateRequired = entity.IsReturnDateRequired,
            LeaveGender = entity.LeaveGender,
            LeaveReligion = entity.LeaveReligion,
            ExcludePaidLeaveAccrual = entity.ExcludePaidLeaveAccrual,
            QuotaType = entity.QuotaType,
            AllowBackDatedDays = entity.AllowBackDatedDays,
            IncludeInSalaryProfile = entity.IncludeInSalaryProfile,
            TotalDaysAllowed = entity.TotalDaysAllowed,
            LeaveEntitlement = entity.LeaveEntitlement,
            AllowDuringProbation = entity.AllowDuringProbation,
            AllowToReturnEarly = entity.AllowToReturnEarly,
            AllowInTerminationNoticeOnly = entity.AllowInTerminationNoticeOnly,
            IsAttachmentRequired = entity.IsAttachmentRequired,
            AllowPartialLeave = entity.AllowPartialLeave,
            LeaveDurationType = entity.LeaveDurationType,
            MaxFutureDays = entity.MaxFutureDays,
            RequirePolicyAcceptance = entity.RequirePolicyAcceptance,
            SendNotification = entity.SendNotification,
            DontAllowToStartOnSunday = entity.DontAllowToStartOnSunday,
            DontAllowToStartOnMonday = entity.DontAllowToStartOnMonday,
            DontAllowToStartOnTuesday = entity.DontAllowToStartOnTuesday,
            DontAllowToStartOnWednesday = entity.DontAllowToStartOnWednesday,
            DontAllowToStartOnThursday = entity.DontAllowToStartOnThursday,
            DontAllowToStartOnFriday = entity.DontAllowToStartOnFriday,
            DontAllowToStartOnSaturday = entity.DontAllowToStartOnSaturday,
            EnableNegativeBalance = entity.EnableNegativeBalance,
            NegativeBalanceLimit = entity.NegativeBalanceLimit,
            AbsenceViolationRuleId = entity.AbsenceViolationRuleId
        };
    }
}
