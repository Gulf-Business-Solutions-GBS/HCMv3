using System;
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
using HCMv3.GeneralMasterInfo.Dto;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HCMv3.Workflow.Services;
using HCMv3.PersonnelAdministration.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// HR's own leave screen for <see cref="EmployeeAbsence"/> (ported from the legacy XAF EmployeeAbsence business
/// object).
///
/// <para>
/// It goes through the SAME rules as an ESS request -- <see cref="Services.ILeaveRuleValidator"/> -- and moves
/// the quota through the same ledger (<see cref="Services.ILeaveQuotaLedger"/>): charged on create, moved on
/// edit, refunded on delete, exactly as legacy's <c>UpdateQuotaInformation</c> and <c>OnDeleting</c> did. A row
/// HR keys in is effective immediately (no workflow status), so it counts at once; a row still going through
/// approval is charged by the workflow handler when it is approved, not here.
/// </para>
///
/// <para>
/// The form's calculated read-only fields (calendar days, total leave days, current balance, paid / unpaid split,
/// sick-leave deduction info) are served by <see cref="GetAbsencePreview"/>.
/// Still NOT ported here: the notification side effects -- see class remarks on <see cref="EmployeeAbsence"/>.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences)]
public class EmployeeAbsencesAppService : HCMv3AppServiceBase, IEmployeeAbsencesAppService
{
    private readonly IRepository<EmployeeAbsence> _repository;
    private readonly IAdminTransactionSubmitter _transactionSubmitter;
    private readonly Services.ILeaveRuleValidator _leaveRuleValidator;
    private readonly Services.ILeaveQuotaLedger _quotaLedger;
    private readonly Services.IEmployeeAbsenceCalculationService _calculationService;
    private readonly Services.ILeaveReturnProcessor _returnProcessor;
    private readonly Services.IProbationEndDateService _probation;
    private readonly Services.IEmployeeAbsencePreviewService _previewService;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<HCMv3.GeneralMasterInfo.Religion> _religionRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<EmployeeSubGroupAbsenceType> _subGroupAbsenceTypeRepository;
    private readonly HCMv3.SystemSettings.Services.ISysSettingsAccessor _sysSettingsAccessor;
    private readonly HCMv3.Storage.IAttachmentAppService _attachmentAppService;
    private readonly Services.ILeaveUnpaidSplitPlanner _splitPlanner;
    private readonly Services.IAdditionalUnpaidLeaveService _additionalUnpaidLeave;
    private readonly IRepository<AbsenceTypeException> _absenceTypeExceptionRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _absAdjustmentRepository;
    private readonly Services.IEmployeeAbsAdjustmentQuotaService _absAdjustmentQuotaService;
    private readonly HCMv3.PayrollManagement.Services.IAccrualService _accrualService;

    public EmployeeAbsencesAppService(
        IRepository<EmployeeAbsence> repository,
        Services.ILeaveRuleValidator leaveRuleValidator,
        Services.ILeaveQuotaLedger quotaLedger,
        Services.IEmployeeAbsenceCalculationService calculationService,
        Services.ILeaveReturnProcessor returnProcessor,
        Services.IProbationEndDateService probation,
        IAdminTransactionSubmitter transactionSubmitter,
        Services.IEmployeeAbsencePreviewService previewService,
        IRepository<Employee> employeeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<HCMv3.GeneralMasterInfo.Religion> religionRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<EmployeeSubGroupAbsenceType> subGroupAbsenceTypeRepository,
        HCMv3.SystemSettings.Services.ISysSettingsAccessor sysSettingsAccessor,
        HCMv3.Storage.IAttachmentAppService attachmentAppService,
        Services.ILeaveUnpaidSplitPlanner splitPlanner,
        Services.IAdditionalUnpaidLeaveService additionalUnpaidLeave,
        IRepository<AbsenceTypeException> absenceTypeExceptionRepository,
        IRepository<EmployeeAbsAdjustment> absAdjustmentRepository,
        HCMv3.PayrollManagement.Services.IAccrualService accrualService,
        Services.IEmployeeAbsAdjustmentQuotaService absAdjustmentQuotaService)
    {
        _splitPlanner = splitPlanner;
        _additionalUnpaidLeave = additionalUnpaidLeave;
        _absenceTypeExceptionRepository = absenceTypeExceptionRepository;
        _absAdjustmentRepository = absAdjustmentRepository;
        _absAdjustmentQuotaService = absAdjustmentQuotaService;
        _accrualService = accrualService;
        _employeeRepository = employeeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _religionRepository = religionRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _subGroupAbsenceTypeRepository = subGroupAbsenceTypeRepository;
        _sysSettingsAccessor = sysSettingsAccessor;
        _attachmentAppService = attachmentAppService;
        _transactionSubmitter = transactionSubmitter;
        _repository = repository;
        _leaveRuleValidator = leaveRuleValidator;
        _quotaLedger = quotaLedger;
        _calculationService = calculationService;
        _returnProcessor = returnProcessor;
        _probation = probation;
        _previewService = previewService;
    }

    /// <summary>
    /// The read-only panel on HR's leave form -- legacy <c>EmployeeAbsence</c> detail view's calculated fields,
    /// worked out by <see cref="Services.IEmployeeAbsencePreviewService"/> from the same calculation services the
    /// save uses. Nothing is saved.
    ///
    /// <para>
    /// <c>[HttpPost]</c> because the input is a custom DTO (ABP would otherwise expose it as a GET with every field
    /// in the query string and NSwag would not generate the input class).
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<EmployeeAbsencePreviewDto> GetAbsencePreview(GetEmployeeAbsencePreviewInput input)
    {
        Services.EmployeeAbsencePreview preview;
        try
        {
            preview = await _previewService.CalculateAsync(new Services.EmployeeAbsencePreviewRequest
            {
                EmployeeId = input.EmployeeId,
                AbsenceTypeId = input.AbsenceTypeId,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                StartTime = input.StartTime,
                EndTime = input.EndTime,
                ReturnDate = input.ReturnDate,
                PaidStartDate = input.PaidStartDate,
                PaidEndDate = input.PaidEndDate,
                UnpaidStartDate = input.UnpaidStartDate,
                UnpaidEndDate = input.UnpaidEndDate,
                HalfdayType = input.HalfdayType,
                PaymentDate = input.PaymentDate,
                ExtStartDate = input.ExtStartDate,
                ExtEndDate = input.ExtEndDate
            });
        }
        catch (InvalidOperationException ex)
        {
            // The engine's "duty / duty period / contract not found" refusals -- legacy surfaced the same failure on
            // the form as the user typed.
            throw new UserFriendlyException(L("LeaveCalculationPreviewNotAvailable"), ex.Message);
        }

        return new EmployeeAbsencePreviewDto
        {
            CalendarDays = preview.CalendarDays,
            AbsenceHours = preview.AbsenceHours,
            TotalLeaveDays = preview.TotalLeaveDays,
            CurrentBalance = preview.CurrentBalance,
            PaidCalendarDays = preview.PaidCalendarDays,
            DeductFromQuota = preview.DeductFromQuota,
            UnpaidCalendarDays = preview.UnpaidCalendarDays,
            UnpaidDays = preview.UnpaidDays,
            SickLeaveUnpaidInfo = preview.SickLeaveUnpaidInfo,
            DaysDifference = preview.DaysDifference,
            ExtCalendarDays = preview.ExtCalendarDays,
            ExtTotalLeaveDays = preview.ExtTotalLeaveDays,
            PaymentDate = preview.PaymentDate,
            StartTime = preview.StartTime,
            EndTime = preview.EndTime,
            SickLeaveSegments = preview.SickLeaveSegments.Select(s => new HCMv3.Ess.Dto.SickLeaveDeductionSegmentDto
            {
                FromDate = s.FromDate,
                ToDate = s.ToDate,
                DeductionNumber = s.DeductionNumber,
                DeductionUnit = s.DeductionUnit,
                IsBeyondTheLastSlab = s.IsBeyondTheLastSlab
            }).ToList()
        };
    }

    /// <summary>
    /// Legacy <c>EmployeeAbsenceController.CheckQuota</c> -> <c>SetAdditionalUnpaidLeave</c> (365-858), asked by the
    /// form before it saves. Where legacy cancelled the commit and popped up "Proceed / Cancel", this answers with
    /// the proposal and the form shows it; on Proceed the form saves the shortened leave with the <c>Ext*</c>
    /// values, and the save books the unpaid row.
    ///
    /// <para>
    /// Triggers, as legacy: an ACCRUAL type (a quota row) whose leave exceeds the bookable balance plus the allowed
    /// negative balance -- capped at <c>MaxLimitInDays</c> when <c>ApplyMaxLeave</c> and
    /// <c>CreateUnpaidAboveMaxLimitDays</c> are on -- and a SICK type (code "3" or quota type Sick Leave) with a
    /// salary-profile allowance for the period. Only a full-day leave is split, and only whole days stay paid.
    /// The available figure is the save's own (<see cref="Services.ILeaveRuleValidator.GetQuotaAvailabilityAsync"/>),
    /// so the shortened leave passes the balance rule.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<EmployeeAbsenceUnpaidSplitDto> GetUnpaidSplitProposal(GetEmployeeAbsencePreviewInput input)
    {
        var result = new EmployeeAbsenceUnpaidSplitDto();
        var start = input.StartDate.Date;
        var end = input.EndDate.Date;

        if (start == default || end == default || end < start)
        {
            return result;
        }

        var absenceType = await _absenceTypeRepository.FirstOrDefaultAsync(input.AbsenceTypeId);
        if (absenceType == null || IsPartialLeave(absenceType, input))
        {
            // SetAdditionalUnpaidLeave: "if (empAbsence.AbsenceHours == 0)" -- an hourly leave is never split.
            return result;
        }

        EmployeeAbsence existing = null;
        if (input.EmployeeAbsenceId.HasValue)
        {
            using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                existing = await _repository.FirstOrDefaultAsync(input.EmployeeAbsenceId.Value);
            }
        }

        try
        {
            var applied = await _calculationService.CalculateTotalLeaveDaysAsync(input.EmployeeId, absenceType.Id, start, end, 0);
            result.AppliedDays = applied;

            var availability = await _leaveRuleValidator.GetQuotaAvailabilityAsync(new Services.LeaveRuleContext
            {
                EmployeeId = input.EmployeeId,
                AbsenceTypeId = absenceType.Id,
                StartDate = start,
                EndDate = end,
                TotalLeaveDays = applied,
                ExcludeEmployeeAbsenceId = existing?.Id ?? 0,
                EmployeeAbsenceId = existing?.Id ?? 0,
                IsNew = existing == null,

                // Legacy added prevTotalLeaveDays back when the same quota row is charged (controller 646-647).
                DaysAlreadyCounted = existing != null && IsEffective(existing) && existing.AbsenceTypeId == absenceType.Id
                    ? existing.TotalLeaveDays
                    : 0
            });

            var isSick = absenceType.Code == Services.LeaveUnpaidSplitPlanner.SickLeaveCode
                         || absenceType.QuotaType == QuotaType.SickLeave;

            double limit;
            if (availability.Kind == Services.LeaveQuotaKind.QuotaRow)
            {
                limit = availability.Available;

                // Controller 667-677.
                if (absenceType.ApplyMaxLeave && absenceType.CreateUnpaidAboveMaxLimitDays && limit > absenceType.MaxLimitInDays)
                {
                    limit = absenceType.MaxLimitInDays;
                    result.IsMaxLimitSplit = true;
                }
            }
            else if (availability.Kind == Services.LeaveQuotaKind.PeriodAllowance && isSick && availability.FromSalaryProfile
                     && availability.Available != 0)
            {
                // Controller 391-446 ("balanceQuota != 0 && totalLeaveDays > balanceQuota").
                limit = availability.Available;
            }
            else
            {
                return result;
            }

            result.AvailableDays = Math.Round(limit, 6);

            if (applied <= limit)
            {
                return result;
            }

            var plan = await _splitPlanner.PlanAsync(input.EmployeeId, absenceType.Id, start, end, limit, existing?.Id ?? 0);

            // Not a single whole day left: legacy offered nothing (the save was simply refused), and so does this --
            // the save refuses with the balance rule's own message.
            if (plan.FitsWithinBalance || plan.PaidEndDate == null)
            {
                return result;
            }

            result.NeedsSplit = true;
            result.EndDate = plan.PaidEndDate;
            result.TotalLeaveDays = plan.PaidLeaveDays;
            result.CalendarDays = plan.PaidCalendarDays;
            result.ExtStartDate = plan.UnpaidStartDate;
            result.ExtEndDate = plan.UnpaidEndDate;
            result.ExtTotalLeaveDays = plan.UnpaidLeaveDays;
            result.ExtCalendarDays = plan.UnpaidCalendarDays;

            var unpaidType = _splitPlanner.FindAdditionalUnpaidLeaveType(absenceType);
            if (unpaidType == null)
            {
                result.UnpaidLeaveTypeMissing = true;
                result.Message = L("AdditionalUnpaidLeaveTypeNotConfigured", absenceType.NameEn, Days(limit), Days(applied));
                return result;
            }

            result.ExtAbsenceTypeId = unpaidType.Id;
            result.ExtAbsenceTypeName = unpaidType.NameEn;

            var paidEnd = plan.PaidEndDate.Value.ToString("dd/MM/yyyy");
            var extStart = plan.UnpaidStartDate!.Value.ToString("dd/MM/yyyy");
            var extEnd = plan.UnpaidEndDate!.Value.ToString("dd/MM/yyyy");

            result.Message = result.IsMaxLimitSplit
                ? L("LeaveMaxLimitExceededSplitProposal", absenceType.NameEn, absenceType.MaxLimitInDays, Days(applied),
                    paidEnd, Days(plan.UnpaidLeaveDays), extStart, extEnd, unpaidType.NameEn)
                : L("LeaveQuotaExceededSplitProposal", absenceType.NameEn, Days(limit), Days(applied),
                    paidEnd, Days(plan.UnpaidLeaveDays), extStart, extEnd, unpaidType.NameEn);

            return result;
        }
        catch (InvalidOperationException ex)
        {
            throw new UserFriendlyException(L("LeaveCalculationPreviewNotAvailable"), ex.Message);
        }
    }

    private static string Days(double days) => days.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Whether the leave as entered is partial (hourly / half day), the way the save will see it.</summary>
    private static bool IsPartialLeave(AbsenceType absenceType, GetEmployeeAbsencePreviewInput input)
    {
        if (!AllowsPartialLeave(absenceType))
        {
            return false;
        }

        return input.HalfdayType != HalfdayType.None
               || (input.StartTime != default && input.EndTime != default && input.EndTime > input.StartTime);
    }

    /// <summary>Partial leave allowed: a Partial-only type, or the flag on a type that is not full-day only.</summary>
    private static bool AllowsPartialLeave(AbsenceType absenceType)
    {
        return absenceType.LeaveDurationType == LeaveDurationType.Partial
               || (absenceType.AllowPartialLeave && absenceType.LeaveDurationType != LeaveDurationType.FullDay);
    }

    /// <summary>
    /// Lists leave for HR, INCLUDING rows still going through approval.
    ///
    /// <para>
    /// The <c>ApprovedOnly</c> filter is disabled here deliberately. That filter exists to stop pending
    /// leave reaching payroll, quota accrual, attendance and the reports -- not to blind HR. An HR user
    /// looking at an employee's leave needs to see "three approved, one waiting with the manager"; hiding
    /// in-flight rows from them would lose exactly the information legacy lost by keeping requests in a
    /// separate table. Each row carries <c>WorkflowStatus</c> so the list can badge the ones not yet
    /// effective.
    /// </para>
    /// </summary>
    [HttpPost]
    public async Task<PagedResultDto<EmployeeAbsenceDto>> GetAll(GetAllEmployeeAbsencesInput input)
    {
        input.Normalize();

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var query = _repository.GetAll()
                .Include(a => a.Employee)
                .Include(a => a.AbsenceType)
                .Include(a => a.ExtAbsenceType)
                .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value)
                .WhereIf(!input.Filter.IsNullOrWhiteSpace(), a =>
                    a.Employee.Code.Contains(input.Filter) ||
                    a.Employee.FullNameEn.Contains(input.Filter) ||
                    a.Employee.FullNameAr.Contains(input.Filter) ||
                    a.AbsenceType.NameEn.Contains(input.Filter) ||
                    a.AbsenceType.NameAr.Contains(input.Filter));

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(input.Sorting)
                .PageBy(input)
                .ToListAsync();

            return new PagedResultDto<EmployeeAbsenceDto>(totalCount, items.Select(ToDto).ToList());
        }
    }

    public async Task<GetEmployeeAbsenceForEditOutput> GetEmployeeAbsenceForEdit(EntityDto input)
    {
        // Same reasoning as GetAll: a row listed for HR must also be openable by them.
        EmployeeAbsence entity;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            entity = await _repository.GetAsync(input.Id);
        }

        var employee = await _employeeRepository.FirstOrDefaultAsync(entity.EmployeeId);

        return new GetEmployeeAbsenceForEditOutput
        {
            EmployeeAbsence = new CreateOrEditEmployeeAbsenceDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AbsenceTypeId = entity.AbsenceTypeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                HalfdayType = entity.HalfdayType,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime,
                IsTravelling = entity.IsTravelling,
                LeavePaymentType = entity.LeavePaymentType,
                PaymentDate = entity.PaymentDate,
                AdvanceLeavePaid = entity.AdvanceLeavePaid,
                Comments = entity.Comments,
                ReturnDate = entity.ReturnDate,
                IsReturned = entity.IsReturned,
                ExtAbsenceTypeId = entity.ExtAbsenceTypeId,
                ExtStartDate = entity.ExtStartDate,
                ExtEndDate = entity.ExtEndDate
            },
            EmployeeCode = employee?.Code,
            EmployeeFullNameEn = employee?.FullNameEn
        };
    }

    [HttpPost]
    public async Task<ListResultDto<SimpleLookupDto>> GetEmployeesForLookup(GetEmployeeAbsenceEmployeesInput input)
    {
        var filter = input.Filter?.Trim();

        var items = await _employeeRepository.GetAll()
            .WhereIf(!filter.IsNullOrWhiteSpace(), e =>
                e.Code.Contains(filter) ||
                e.FullNameEn.Contains(filter) ||
                e.FullNameAr.Contains(filter))
            .OrderBy(e => e.Code.Length)
            .ThenBy(e => e.Code)
            .Select(e => new SimpleLookupDto
            {
                Id = e.Id,
                Code = e.Code,
                NameEn = e.FullNameEn,
                NameAr = e.FullNameAr
            })
            .ToListAsync();

        return new ListResultDto<SimpleLookupDto>(items);
    }

    /// <summary>
    /// Legacy <c>EmployeeAbsence.EmpAbsenceType</c> (lines 73-127): the Leave Type drop-down offers only active
    /// types that suit the employee's gender and religion, and -- when the employee has a sub group -- only the
    /// salary-profile types among those that require it. The same test the save refuses with
    /// (<see cref="Services.LeaveRuleValidator.LeaveTypeSuits"/>), so the list never offers a type the save rejects.
    /// </summary>
    public async Task<ListResultDto<EmployeeAbsenceLeaveTypeDto>> GetLeaveTypesForEmployee(EntityDto input)
    {
        var today = Abp.Timing.Clock.Now.Date;

        var employee = await _employeeRepository.GetAll()
            .Where(e => e.Id == input.Id)
            .Select(e => new { e.Gender, e.ReligionId })
            .FirstOrDefaultAsync();
        if (employee == null)
        {
            return new ListResultDto<EmployeeAbsenceLeaveTypeDto>();
        }

        var religionName = employee.ReligionId.HasValue
            ? await _religionRepository.GetAll()
                .Where(r => r.Id == employee.ReligionId.Value)
                .Select(r => r.NameEn)
                .FirstOrDefaultAsync()
            : null;

        var subGroupId = await _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == input.Id && a.StartDate <= today && a.EndDate >= today)
            .OrderByDescending(a => a.StartDate)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefaultAsync();

        var profileTypeIds = subGroupId.HasValue
            ? await _subGroupAbsenceTypeRepository.GetAll()
                .Where(a => a.EmployeeSubGroupId == subGroupId.Value)
                .Select(a => a.AbsenceTypeId)
                .ToListAsync()
            : new System.Collections.Generic.List<int>();

        var types = await _absenceTypeRepository.GetAll()
            .Where(t => t.IsActive)
            .OrderBy(t => t.NameEn)
            .ToListAsync();

        var items = types
            .Where(t => Services.LeaveRuleValidator.LeaveTypeSuits(
                t.LeaveGender, t.LeaveReligion, employee.Gender, employee.ReligionId.HasValue, religionName))
            .Where(t => !(subGroupId.HasValue && t.IncludeInSalaryProfile && !profileTypeIds.Contains(t.Id)))
            .Select(t => new EmployeeAbsenceLeaveTypeDto
            {
                Id = t.Id,
                Code = t.Code,
                NameEn = t.NameEn,
                NameAr = t.NameAr,
                AllowPartialLeave = t.LeaveDurationType == LeaveDurationType.Partial
                                    || (t.AllowPartialLeave && t.LeaveDurationType != LeaveDurationType.FullDay),
                LeaveDurationType = t.LeaveDurationType
            })
            .ToList();

        return new ListResultDto<EmployeeAbsenceLeaveTypeDto>(items);
    }

    public async Task<EmployeeAbsenceFormSettingsDto> GetLeaveFormSettings()
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        var payrollSettings = await _sysSettingsAccessor.GetPayrollSettingsAsync();

        return new EmployeeAbsenceFormSettingsDto
        {
            EnableAutoHalfDayLeave = timeSettings?.EnableAutoHalfDayLeave ?? false,
            EnablePayFullWithPayroll = timeSettings?.EnablePayFullWithPayroll ?? false,
            EnableSettleFractionalLeaveBalance = timeSettings?.EnableSettleFractionalLeaveBalance ?? false,
            DefaultLeavePaymentType = payrollSettings?.DefaultLeavePaymentType ?? LeavePaymentType.None
        };
    }

    /// <summary>Moves files staged on the form onto the new leave, through the shared attachment store.</summary>
    private async Task AttachStagedFilesAsync(int leaveId, System.Collections.Generic.List<string> fileTokens)
    {
        if (fileTokens == null || fileTokens.Count == 0)
        {
            return;
        }

        foreach (var token in fileTokens.Where(t => !string.IsNullOrWhiteSpace(t)))
        {
            await _attachmentAppService.AddAttachment(new HCMv3.Storage.Dto.AddAttachmentInput
            {
                EntityType = Services.LeaveRuleValidator.LeaveAttachmentEntityType,
                EntityId = leaveId.ToString(System.Globalization.CultureInfo.InvariantCulture),
                FileToken = token,
            });
        }

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAbsenceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAbsenceDto input)
    {
        var entity = MapToEntity(input, new EmployeeAbsence());
        entity.TenantId = AbpSession.TenantId ?? 0;
        await ApplyLegacyDerivedFieldsAsync(entity, wasReturned: false);
        entity.CalendarDays = CalculateCalendarDays(entity.StartDate, entity.EndDate);

        // Calendar days less the week-offs, holidays and rest days the type excludes -- what the quota is
        // charged, as legacy's UpdateQuotaInformation computed it. CalendarDays keeps the raw span.
        entity.TotalLeaveDays = await _calculationService.CalculateTotalLeaveDaysAsync(entity);
        entity.InitialEndDate = entity.EndDate;

        await ValidateAsync(entity, daysAlreadyCounted: 0, previousExtra: null);
        // Routed for approval when the catalog says so, and simply filed when it does not -- the same
        // question the self-service form asks, so the answer no longer depends on which screen was used.
        // recordDirectly is refused unless the type permits a bypass AND the user holds the permission.
        await _transactionSubmitter.CreateAndSubmitAsync(
            _repository,
            entity,
            LeaveWorkflowTransactionHandler.LeaveRequestType,
            "LV",
            entity.AbsenceTypeId,
            input.RecordDirectly);

        await ChargeQuotaAsync(entity);
        await AttachStagedFilesAsync(entity.Id, input.AttachmentFileTokens);

        // Legacy SaveAdditionalUnpaidLeave (EA 4612-4640). A leave routed for approval gets its unpaid row from
        // the workflow handler when it is approved, exactly as an ESS application does.
        if (IsEffective(entity))
        {
            await _additionalUnpaidLeave.SyncAsync(entity, previous: null);
        }

        await ApplyReturnAsync(entity, input, previousReturnDate: default);

        // Unpaid leave during probation pushes its end date out (legacy Employee.GetProbationEndDate, working-days mode).
        await _probation.RecalculateAfterLeaveChangeAsync(entity.EmployeeId, entity.AbsenceTypeId);
        if (entity.ExtAbsenceTypeId.HasValue && IsEffective(entity))
        {
            await _probation.RecalculateAfterLeaveChangeAsync(entity.EmployeeId, entity.ExtAbsenceTypeId.Value);
        }

        if (IsEffective(entity))
        {
            await RecalculateMonthlyForfeitAsync(entity.EmployeeId, entity.StartDate, entity.EndDate, entity.AbsenceTypeId);
            await SettleFractionalBalanceAsync(entity, input.SettleFractionalBalance);
        }
    }

    /// <summary>
    /// HR recording that the employee came back -- legacy <c>EmployeeAbsence.SaveDiffInformation</c>, which ran
    /// whenever a return date was typed onto the leave.
    ///
    /// <para>
    /// It goes through the same processor an approved ESS return does: a LATE return books the gap as further
    /// paid and/or unpaid leave, an EARLY one shortens the leave (and its additional unpaid leave), and either
    /// way back-to-back leave before it is marked returned. Only a NEWLY entered return date does anything --
    /// re-saving a leave that was already returned from must not generate the days twice.
    /// </para>
    /// </summary>
    private async Task ApplyReturnAsync(EmployeeAbsence entity, CreateOrEditEmployeeAbsenceDto input, DateTime previousReturnDate)
    {
        if (entity.ReturnDate == default || entity.ReturnDate.Date == previousReturnDate.Date)
        {
            return;
        }

        var instruction = new Services.LeaveReturnInstruction
        {
            Leave = entity,
            ReturnDate = entity.ReturnDate.Date,
            PaidStartDate = input.PaidStartDate,
            PaidEndDate = input.PaidEndDate,
            UnpaidStartDate = input.UnpaidStartDate,
            UnpaidEndDate = input.UnpaidEndDate,
        };

        var daysDifference = await _returnProcessor.GetDaysDifferenceAsync(entity, entity.ReturnDate.Date);

        // EARLY: the leave now ends on the last regular day before they came back, and its days go back to the
        // quota. Legacy kept the granted end date in InitialEndDate.
        if (daysDifference < 0)
        {
            // Legacy GetRegularDate (EA 3231-3265): when another absence already starts on the day this leave
            // would now end, the return date cannot be right.
            var regularDate = await _calculationService.GetRegularDateAsync(
                entity.EmployeeId, entity.ReturnDate.Date, excludeEmployeeAbsenceId: entity.Id);
            if (regularDate == null)
            {
                throw new UserFriendlyException(L(
                    "ReturnDateConflictsWithAnotherLeave", entity.ReturnDate.Date.AddDays(-1).ToString("dd/MM/yyyy")));
            }

            var previousTotal = entity.TotalLeaveDays;
            var newEndDate = regularDate.Value;

            // The additional unpaid leave, when attached (legacy EarlyJoinType, as the ESS return does): back
            // before it starts removes it and shortens the paid leave; back during it leaves the paid leave alone
            // and cuts the unpaid row back.
            if (entity.ExtStartDate != null)
            {
                if (entity.ReturnDate.Date <= entity.ExtStartDate.Value.Date)
                {
                    instruction.AdditionalLeaveAction = Workflow.AdditionalLeaveAction.Delete;
                }
                else
                {
                    instruction.AdditionalLeaveAction = Workflow.AdditionalLeaveAction.Shorten;
                    instruction.AdditionalLeaveEndDate = regularDate.Value;
                    newEndDate = entity.EndDate.Date;
                }
            }

            if (newEndDate != entity.EndDate.Date)
            {
                entity.InitialEndDate = entity.EndDate;
                entity.EndDate = newEndDate;
                entity.CalendarDays = CalculateCalendarDays(entity.StartDate, entity.EndDate);
                entity.TotalLeaveDays = await _calculationService.CalculateTotalLeaveDaysAsync(entity);
            }

            entity.IsReturned = true;

            await _repository.UpdateAsync(entity);

            if (IsEffective(entity))
            {
                await _quotaLedger.ReleaseAsync(entity.EmployeeId, entity.AbsenceTypeId, previousTotal, entity.StartDate);
                await _quotaLedger.ConsumeAsync(entity.EmployeeId, entity.AbsenceTypeId, entity.TotalLeaveDays, entity.StartDate);
            }
        }
        else if (daysDifference == 0)
        {
            entity.IsReturned = true;
            await _repository.UpdateAsync(entity);
        }

        await _returnProcessor.ApplyAsync(instruction);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAbsenceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        // What the quota currently carries for this leave, captured before the row is overwritten.
        var previousTypeId = entity.AbsenceTypeId;
        var previousDays = entity.TotalLeaveDays;
        var previousStart = entity.StartDate;
        var previousEnd = entity.EndDate;
        var wasEffective = IsEffective(entity);
        var previousReturnDate = entity.ReturnDate;
        var wasReturned = entity.IsReturned;
        var previousExtra = Services.AdditionalUnpaidLeaveSnapshot.Of(entity);

        MapToEntity(input, entity);
        await ApplyLegacyDerivedFieldsAsync(entity, wasReturned);
        entity.CalendarDays = CalculateCalendarDays(entity.StartDate, entity.EndDate);
        entity.TotalLeaveDays = await _calculationService.CalculateTotalLeaveDaysAsync(entity);

        // The days this leave already holds in the same quota are credited back before the balance is compared --
        // legacy added prevTotalLeaveDays back for the same quota row (UpdateQuotaInformation).
        var daysAlreadyCounted = wasEffective && previousTypeId == entity.AbsenceTypeId ? previousDays : 0;

        await ValidateAsync(entity, daysAlreadyCounted, previousExtra);
        await _repository.UpdateAsync(entity);

        // The quota moves BEFORE the return is applied: an early return releases and recharges from the figure
        // the row holds at that point, so the two must not both start from the original days.
        if (wasEffective && IsEffective(entity))
        {
            await _quotaLedger.MoveAsync(
                entity.EmployeeId, previousTypeId, previousDays, previousStart,
                entity.AbsenceTypeId, entity.TotalLeaveDays, entity.StartDate);
        }
        else if (IsEffective(entity))
        {
            await ChargeQuotaAsync(entity);
        }

        if (IsEffective(entity))
        {
            await _additionalUnpaidLeave.SyncAsync(entity, previousExtra);
        }

        await ApplyReturnAsync(entity, input, previousReturnDate);

        await _probation.RecalculateAfterLeaveChangeAsync(entity.EmployeeId, previousTypeId);
        if (entity.AbsenceTypeId != previousTypeId)
        {
            await _probation.RecalculateAfterLeaveChangeAsync(entity.EmployeeId, entity.AbsenceTypeId);
        }

        foreach (var extraTypeId in new[] { previousExtra.AbsenceTypeId, entity.ExtAbsenceTypeId }
                     .Where(t => t.HasValue).Select(t => t.Value).Distinct())
        {
            await _probation.RecalculateAfterLeaveChangeAsync(entity.EmployeeId, extraTypeId);
        }

        if (wasEffective || IsEffective(entity))
        {
            await RecalculateMonthlyForfeitAsync(
                entity.EmployeeId,
                previousStart < entity.StartDate ? previousStart : entity.StartDate,
                previousEnd > entity.EndDate ? previousEnd : entity.EndDate,
                previousTypeId, entity.AbsenceTypeId);
        }

        if (IsEffective(entity))
        {
            await SettleFractionalBalanceAsync(entity, input.SettleFractionalBalance);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsences_Delete)]
    public async Task Delete(EntityDto input)
    {
        EmployeeAbsence entity;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            entity = await _repository.FirstOrDefaultAsync(input.Id);
        }

        if (entity == null)
        {
            return;
        }

        // Legacy EmployeeAbsence.OnDeleting: the days go back to the quota. Only for a leave that was counted
        // in the first place -- one still awaiting approval never moved the quota.
        if (IsEffective(entity))
        {
            await _quotaLedger.ReleaseAsync(
                entity.EmployeeId, entity.AbsenceTypeId, entity.TotalLeaveDays, entity.StartDate);
        }

        await _repository.DeleteAsync(entity);

        await _probation.RecalculateAfterLeaveChangeAsync(entity.EmployeeId, entity.AbsenceTypeId);

        // OnDeleting 1340-1343: the monthly forfeit is worked out again without this leave.
        if (IsEffective(entity))
        {
            await RecalculateMonthlyForfeitAsync(entity.EmployeeId, entity.StartDate, entity.EndDate, entity.AbsenceTypeId);
        }
    }

    private Task ChargeQuotaAsync(EmployeeAbsence entity)
    {
        return IsEffective(entity)
            ? _quotaLedger.ConsumeAsync(entity.EmployeeId, entity.AbsenceTypeId, entity.TotalLeaveDays, entity.StartDate)
            : Task.CompletedTask;
    }

    /// <summary>
    /// Legacy <c>UpdateQuotaInformation</c> (EA 2336-2339, 2413-2416) and <c>OnDeleting</c> (1340-1343): with
    /// <c>ForfeitRule == Monthly</c>, a change to an accrual-type leave makes the monthly forfeit be worked out
    /// again from the leave's start, through <see cref="HCMv3.PayrollManagement.Services.IAccrualService.CalculateForfeitForLeaveAsync"/>
    /// (which itself handles the annual leave quota only, as legacy's did).
    /// </summary>
    private async Task RecalculateMonthlyForfeitAsync(int employeeId, DateTime from, DateTime to, params int[] absenceTypeIds)
    {
        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings?.ForfeitRule != HCMv3.SystemSettings.ForfeitRule.Monthly)
        {
            return;
        }

        var typeIds = absenceTypeIds.Distinct().ToList();
        var anyAccrual = await _absenceTypeRepository.GetAll().AnyAsync(t => typeIds.Contains(t.Id) && t.HaveQuotaAccrual);
        if (!anyAccrual)
        {
            return;
        }

        // The forfeit reads the leave rows from the database, so the change has to be there first.
        await CurrentUnitOfWork.SaveChangesAsync();

        var subGroupId = await _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= from.Date && a.EndDate >= from.Date)
            .OrderByDescending(a => a.StartDate)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefaultAsync();

        await _accrualService.CalculateForfeitForLeaveAsync(employeeId, subGroupId, from.Date, to.Date, prevEmployeeAbsenceId: null);
    }

    /// <summary>
    /// Legacy <c>OnSaved</c> -> <c>SaveFractionAdjustment</c> (EA 4688-4747): with "Settle Fractional Balance"
    /// ticked (and <c>EnableSettleFractionalLeaveBalance</c> on), the fraction left in the balance once this leave
    /// is charged is written off as a <see cref="AdjustmentType.Fractional"/> leave adjustment on the leave's end
    /// date. The balance is the form's own <c>CurrentBalance</c>.
    ///
    /// <para>
    /// The adjustment row is recorded exactly as the Leave Abs Adjustment screen records one
    /// (<c>EmployeeAbsAdjustmentsAppService</c>): through <c>IEmployeeAbsAdjustmentQuotaService</c>, which adds
    /// it to the annual quota's <c>QuotaAdjusted</c> as legacy's <c>Save()</c> did.
    /// </para>
    /// </summary>
    private async Task SettleFractionalBalanceAsync(EmployeeAbsence entity, bool requested)
    {
        if (!requested)
        {
            return;
        }

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();
        if (timeSettings?.EnableSettleFractionalLeaveBalance != true)
        {
            return;
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        double balance;
        try
        {
            var preview = await _previewService.CalculateAsync(new Services.EmployeeAbsencePreviewRequest
            {
                EmployeeId = entity.EmployeeId,
                AbsenceTypeId = entity.AbsenceTypeId,
                StartDate = entity.StartDate,
                EndDate = entity.EndDate,
                StartTime = entity.StartTime,
                EndTime = entity.EndTime
            });
            balance = preview.CurrentBalance;
        }
        catch (InvalidOperationException ex)
        {
            throw new UserFriendlyException(L("LeaveCalculationPreviewNotAvailable"), ex.Message);
        }

        var fraction = Math.Round(balance - Math.Floor(balance), 6);
        if (fraction == 0)
        {
            return;
        }

        var adjustment = new EmployeeAbsAdjustment
        {
            TenantId = entity.TenantId,
            EmployeeId = entity.EmployeeId,
            AbsenceTypeId = entity.AbsenceTypeId,
            AdjustmentType = AdjustmentType.Fractional,
            EffectiveDate = entity.EndDate.Date,
            QuotaAdjusted = fraction
        };

        // Legacy empAbsAdjustment.Save() runs the adjustment's own OnSaving, which moves the annual quota.
        await _absAdjustmentQuotaService.ApplyAsync(adjustment, previous: null);

        await _absAdjustmentRepository.InsertAsync(adjustment);
    }

    /// <summary>
    /// Whether the leave counts today: entered by HR (no workflow status) or approved. A Draft, Open or Rejected
    /// row is not in anybody's quota, and the workflow handler charges it when it is approved.
    /// </summary>
    private static bool IsEffective(EmployeeAbsence entity)
    {
        return entity.WorkflowStatus is null
               or HCMv3.Workflow.RequestStatus.Approved
               or HCMv3.Workflow.RequestStatus.Closed;
    }

    private async Task ValidateAsync(EmployeeAbsence entity, double daysAlreadyCounted, Services.AdditionalUnpaidLeaveSnapshot previousExtra)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        var timeSettings = await _sysSettingsAccessor.GetTimeSettingsAsync();

        // EA 1605-1614: a leave may not straddle the earliest evaluation date -- quota, return and continuation
        // logic only run from that date on.
        var earliestEvaluationDate = timeSettings?.EarliestEvaluationDate.Date ?? default;
        if (earliestEvaluationDate.Year > 1
            && entity.StartDate.Date < earliestEvaluationDate
            && entity.EndDate.Date >= earliestEvaluationDate)
        {
            throw new UserFriendlyException(L(
                "LeaveCannotSpanEarliestEvaluationDate",
                earliestEvaluationDate.ToString("dd/MM/yyyy"),
                entity.StartDate.ToString("dd/MM/yyyy"),
                entity.EndDate.ToString("dd/MM/yyyy")));
        }

        EnsureAdditionalUnpaidLeaveDatesAreValid(entity);

        await EnsureNoOverlapAsync(entity, previousExtra);
        await EnsureNoExceptionTypeAdjacentAsync(entity);
        await ValidateReturnDateAsync(entity, timeSettings);

        // The same leave rules the ESS and the approver go through -- legacy applied them on the row as well as
        // on the request, so HR keying a leave in directly could not bypass probation, notice period, min and
        // max length, repeat rules, blocked start days, quota or the separation rules.
        await _leaveRuleValidator.ValidateAsync(new Services.LeaveRuleContext
        {
            EmployeeId = entity.EmployeeId,
            AbsenceTypeId = entity.AbsenceTypeId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            TotalLeaveDays = entity.TotalLeaveDays,
            AbsenceHours = entity.AbsenceHours,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            HalfdayType = entity.HalfdayType,
            ExcludeEmployeeAbsenceId = entity.Id,
            EmployeeAbsenceId = entity.Id,
            IsNew = entity.Id == 0,
            CreatedOn = entity.CreationTime,
            Comments = entity.Comments,
            LeavePaymentType = entity.LeavePaymentType,
            PaymentDate = entity.PaymentDate,
            DaysAlreadyCounted = daysAlreadyCounted,
        });
    }

    /// <summary>EA 1940-1953: the additional unpaid leave needs both dates, in order, clear of the leave itself.</summary>
    private void EnsureAdditionalUnpaidLeaveDatesAreValid(EmployeeAbsence entity)
    {
        if (entity.ExtAbsenceTypeId == null)
        {
            return;
        }

        if (entity.ExtStartDate == null || entity.ExtEndDate == null)
        {
            throw new UserFriendlyException(L("AdditionalUnpaidLeaveDatesRequired"));
        }

        var extStart = entity.ExtStartDate.Value.Date;
        var extEnd = entity.ExtEndDate.Value.Date;

        if (extEnd < extStart)
        {
            throw new UserFriendlyException(L("AdditionalUnpaidLeaveEndDateBeforeStartDate"));
        }

        if ((entity.StartDate.Date <= extStart && entity.EndDate.Date >= extStart)
            || (entity.StartDate.Date <= extEnd && entity.EndDate.Date >= extEnd))
        {
            throw new UserFriendlyException(L("AdditionalUnpaidLeaveOverlapsMainLeave"));
        }
    }

    /// <summary>
    /// No two claims on the same time. Pending leave counts as a conflict, so the filter is disabled for this
    /// check: without it HR could book leave over a request currently sitting with the employee's manager.
    /// Rejected and sent-back rows do not block, since neither is a claim on the employee's time.
    ///
    /// <para>
    /// Two PARTIAL leaves on the same day are allowed when their times do not overlap -- legacy EA 1815-1833
    /// compared times for hourly leave rather than refusing the day. A full-day leave conflicts with anything.
    /// The additional unpaid leave's own dates are checked too, since they become a leave row of their own; its
    /// existing row (and the leave itself) are not counted against it.
    /// </para>
    /// </summary>
    private async Task EnsureNoOverlapAsync(EmployeeAbsence entity, Services.AdditionalUnpaidLeaveSnapshot previousExtra)
    {
        var start = entity.StartDate.Date;
        var end = entity.EndDate.Date;
        var hasExtra = entity.ExtAbsenceTypeId != null && entity.ExtStartDate != null && entity.ExtEndDate != null;
        var rangeStart = hasExtra && entity.ExtStartDate.Value.Date < start ? entity.ExtStartDate.Value.Date : start;
        var rangeEnd = hasExtra && entity.ExtEndDate.Value.Date > end ? entity.ExtEndDate.Value.Date : end;

        System.Collections.Generic.List<EmployeeAbsence> candidates;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            candidates = await _repository.GetAll()
                .Where(a => a.Id != entity.Id
                            && a.EmployeeId == entity.EmployeeId
                            && a.StartDate.Date <= rangeEnd
                            && a.EndDate.Date >= rangeStart
                            && (a.WorkflowStatus == null
                                || a.WorkflowStatus == HCMv3.Workflow.RequestStatus.Approved
                                || a.WorkflowStatus == HCMv3.Workflow.RequestStatus.Closed
                                || a.WorkflowStatus == HCMv3.Workflow.RequestStatus.Open))
                .ToListAsync();
        }

        // The unpaid row this leave already booked is its own, not a conflict.
        candidates = candidates
            .Where(a => !(previousExtra?.AbsenceTypeId != null
                          && a.AbsenceTypeId == previousExtra.AbsenceTypeId.Value
                          && a.StartDate.Date == previousExtra.StartDate?.Date
                          && a.EndDate.Date == previousExtra.EndDate?.Date))
            .ToList();

        foreach (var other in candidates.Where(a => a.StartDate.Date <= end && a.EndDate.Date >= start))
        {
            if (IsSingleDayPartial(entity) && IsSingleDayPartial(other))
            {
                if (!TimesOverlap(entity, other))
                {
                    continue;
                }

                throw new UserFriendlyException(L(
                    "PartialLeaveTimesOverlap",
                    other.StartDate.ToString("dd/MM/yyyy"),
                    other.StartTime.ToString("HH:mm"),
                    other.EndTime.ToString("HH:mm")));
            }

            throw new UserFriendlyException(L("OverlapConflictFound"));
        }

        if (hasExtra)
        {
            var extStart = entity.ExtStartDate.Value.Date;
            var extEnd = entity.ExtEndDate.Value.Date;
            if (candidates.Any(a => a.StartDate.Date <= extEnd && a.EndDate.Date >= extStart))
            {
                throw new UserFriendlyException(L("OverlapConflictFound"));
            }
        }
    }

    private static bool IsSingleDayPartial(EmployeeAbsence leave)
    {
        return leave.AbsenceHours > 0 && leave.StartDate.Date == leave.EndDate.Date;
    }

    /// <summary>
    /// Whether two partial leaves' times overlap, by time of day and duration (so the stored date part of the
    /// times does not matter, and a night shift running past midnight still compares correctly). Touching is not
    /// overlapping: 09:00-11:00 and 11:00-13:00 are both allowed.
    /// </summary>
    private static bool TimesOverlap(EmployeeAbsence first, EmployeeAbsence second)
    {
        static (double From, double To) Minutes(EmployeeAbsence leave)
        {
            var from = leave.StartTime.TimeOfDay.TotalMinutes;
            return (from, from + (leave.EndTime - leave.StartTime).TotalMinutes);
        }

        var a = Minutes(first);
        var b = Minutes(second);
        return a.From < b.To && b.From < a.To;
    }

    /// <summary>
    /// EA 1957-1992 "Leave Exception violated": a full-day leave may not touch -- overlap, follow or precede by a
    /// day -- a leave of a type listed against it in Absence Type Exceptions, in either direction of the list.
    /// </summary>
    private async Task EnsureNoExceptionTypeAdjacentAsync(EmployeeAbsence entity)
    {
        if (entity.AbsenceHours != 0)
        {
            return;
        }

        var typeId = entity.AbsenceTypeId;
        var exceptionTypeIds = await _absenceTypeExceptionRepository.GetAll()
            .Where(x => x.AbsenceTypeId == typeId || x.ExceptAbsenceTypeId == typeId)
            .Select(x => x.AbsenceTypeId == typeId ? x.ExceptAbsenceTypeId : x.AbsenceTypeId)
            .Distinct()
            .ToListAsync();

        if (exceptionTypeIds.Count == 0)
        {
            return;
        }

        var from = entity.StartDate.Date.AddDays(-1);
        var to = entity.EndDate.Date.AddDays(1);

        EmployeeAbsence conflict;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            conflict = await _repository.GetAll()
                .Include(a => a.AbsenceType)
                .Where(a => a.Id != entity.Id
                            && a.EmployeeId == entity.EmployeeId
                            && exceptionTypeIds.Contains(a.AbsenceTypeId)
                            && !(a.StartDate.Date > to || a.EndDate.Date < from)
                            && (a.WorkflowStatus == null
                                || a.WorkflowStatus == HCMv3.Workflow.RequestStatus.Approved
                                || a.WorkflowStatus == HCMv3.Workflow.RequestStatus.Closed
                                || a.WorkflowStatus == HCMv3.Workflow.RequestStatus.Open))
                .OrderBy(a => a.StartDate)
                .FirstOrDefaultAsync();
        }

        if (conflict == null)
        {
            return;
        }

        var typeName = await _absenceTypeRepository.GetAll().Where(t => t.Id == typeId).Select(t => t.NameEn).FirstOrDefaultAsync();

        throw new UserFriendlyException(L(
            "LeaveTypeExceptionViolated",
            typeName,
            conflict.AbsenceType?.NameEn,
            conflict.StartDate.ToString("dd/MM/yyyy"),
            conflict.EndDate.ToString("dd/MM/yyyy")));
    }

    /// <summary>
    /// EA 1622-1634 and 1753-1757, for a leave with a return date: not in the future unless the time settings
    /// allow it, after the start, not early for a type that must be taken in full, and -- when the employee came
    /// back on or after the end date -- on a regular day.
    /// </summary>
    private async Task ValidateReturnDateAsync(EmployeeAbsence entity, HCMv3.SystemSettings.ManageTimeSettings timeSettings)
    {
        if (entity.ReturnDate.Year <= 1)
        {
            return;
        }

        var returnDate = entity.ReturnDate.Date;

        if (returnDate > Abp.Timing.Clock.Now.Date && timeSettings?.AllowFutureDatedReturnFromLeave != true)
        {
            throw new UserFriendlyException(L("RejoiningDateCannotBeInTheFuture"));
        }

        if (returnDate <= entity.StartDate.Date)
        {
            throw new UserFriendlyException(L("ReturnDateMustBeAfterStartDate"));
        }

        if (await _returnProcessor.GetDaysDifferenceAsync(entity, returnDate) < 0)
        {
            _leaveRuleValidator.EnsureEarlyReturnIsAllowed(entity.AbsenceTypeId);
        }

        if (entity.EndDate.Date <= returnDate)
        {
            await _leaveRuleValidator.EnsureRejoiningDayIsRegularAsync(entity.EmployeeId, returnDate);
        }
    }

    /// <summary>
    /// The fields legacy's <c>EmployeeAbsence</c> worked out itself rather than taking from the form, applied after
    /// <see cref="MapToEntity"/> and before the day counts and the rules:
    /// <list type="bullet">
    /// <item><description>A type that does not allow partial leave (or is full-day only) clears the half day and the
    /// times (controller lines 261-330).</description></item>
    /// <item><description>A half day takes its times from the duty (<c>RecalculateDutyTimes</c>, 3370-3386).</description></item>
    /// <item><description><c>AbsenceHours</c> from the times (<c>UpdateAbsenceHours</c>, 3387-3393); a full-day leave
    /// has its times reset (1716-1720). Without this an hourly leave was saved and charged as whole days.</description></item>
    /// <item><description>An empty <c>PaymentDate</c> filled from the start date (<c>CalculatePaymentDate</c>, 1172-1176).</description></item>
    /// <item><description><c>ExtTotalLeaveDays</c> recomputed from the Ext dates (4621-4625).</description></item>
    /// <item><description><c>IsReturned</c> is the system's, never the form's: kept as it was, and false once the
    /// return date is cleared (1708-1712). The return processing sets it true.</description></item>
    /// </list>
    /// </summary>
    private async Task ApplyLegacyDerivedFieldsAsync(EmployeeAbsence entity, bool wasReturned)
    {
        var absenceType = await _absenceTypeRepository.FirstOrDefaultAsync(entity.AbsenceTypeId);
        // A Partial-only type keeps its times even without the flag: they are mandatory for it (LeaveRuleValidator).
        if (absenceType != null
            && (absenceType.LeaveDurationType == LeaveDurationType.FullDay
                || (!absenceType.AllowPartialLeave && absenceType.LeaveDurationType != LeaveDurationType.Partial)))
        {
            entity.HalfdayType = HalfdayType.None;
            entity.StartTime = default;
            entity.EndTime = default;
        }

        var halfDay = await _calculationService.ResolveHalfDayTimesAsync(entity.EmployeeId, entity.StartDate, entity.HalfdayType);
        if (halfDay.HasValue)
        {
            (entity.StartTime, entity.EndTime) = halfDay.Value;
        }

        entity.AbsenceHours = entity.StartTime != default && entity.EndTime != default
            ? Math.Round((entity.EndTime - entity.StartTime).TotalHours, 3)
            : 0;
        if (entity.AbsenceHours <= 0)
        {
            entity.AbsenceHours = 0;
            entity.StartTime = default;
            entity.EndTime = default;
        }

        if (entity.PaymentDate.Year <= 1)
        {
            entity.PaymentDate = await _calculationService.DefaultPaymentDateAsync(entity.StartDate);
        }

        entity.ExtTotalLeaveDays = entity.ExtStartDate.HasValue && entity.ExtEndDate.HasValue
                                   && entity.ExtEndDate.Value.Date >= entity.ExtStartDate.Value.Date
            ? (entity.ExtEndDate.Value.Date - entity.ExtStartDate.Value.Date).TotalDays + 1
              - (double)await _calculationService.ActualLeaveDaysAsync(
                  entity.EmployeeId, entity.ExtStartDate.Value.Date, entity.ExtEndDate.Value.Date, entity.AbsenceTypeId)
            : null;

        entity.IsReturned = entity.ReturnDate.Year > 1 && wasReturned;
    }

    private static double CalculateCalendarDays(System.DateTime startDate, System.DateTime endDate)
    {
        if (startDate == default || endDate == default)
        {
            return 0;
        }

        return (endDate - startDate).TotalDays + 1;
    }

    private static EmployeeAbsence MapToEntity(CreateOrEditEmployeeAbsenceDto input, EmployeeAbsence entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.HalfdayType = input.HalfdayType;
        entity.StartTime = input.StartTime;
        entity.EndTime = input.EndTime;
        entity.IsTravelling = input.IsTravelling;
        entity.LeavePaymentType = input.LeavePaymentType;
        entity.PaymentDate = input.PaymentDate;
        entity.AdvanceLeavePaid = input.AdvanceLeavePaid;
        entity.Comments = input.Comments;
        entity.ReturnDate = input.ReturnDate;
        entity.ExtAbsenceTypeId = input.ExtAbsenceTypeId;
        entity.ExtStartDate = input.ExtStartDate;
        entity.ExtEndDate = input.ExtEndDate;
        return entity;
    }

    private static EmployeeAbsenceDto ToDto(EmployeeAbsence entity)
    {
        return new EmployeeAbsenceDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            HalfdayType = entity.HalfdayType,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime,
            AbsenceHours = entity.AbsenceHours,
            IsTravelling = entity.IsTravelling,
            CalendarDays = entity.CalendarDays,
            TotalLeaveDays = entity.TotalLeaveDays,
            LeavePaymentType = entity.LeavePaymentType,
            PaymentDate = entity.PaymentDate,
            AdvanceLeavePaid = entity.AdvanceLeavePaid,
            Comments = entity.Comments,
            ReturnDate = entity.ReturnDate,
            IsReturned = entity.IsReturned,
            InitialEndDate = entity.InitialEndDate,
            ExtAbsenceTypeId = entity.ExtAbsenceTypeId,
            ExtAbsenceTypeName = entity.ExtAbsenceType?.NameEn,
            ExtStartDate = entity.ExtStartDate,
            ExtEndDate = entity.ExtEndDate,
            ExtTotalLeaveDays = entity.ExtTotalLeaveDays,

            // Non-null WorkflowRequestId is what puts the Workflow History button on the row; WorkflowStatus
            // badges rows that are still in flight.
            WorkflowRequestId = entity.WorkflowRequestId,
            WorkflowStatus = entity.WorkflowStatus
        };
    }
}
