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

    public EmployeeAbsencesAppService(
        IRepository<EmployeeAbsence> repository,
        Services.ILeaveRuleValidator leaveRuleValidator,
        Services.ILeaveQuotaLedger quotaLedger,
        Services.IEmployeeAbsenceCalculationService calculationService,
        Services.ILeaveReturnProcessor returnProcessor,
        Services.IProbationEndDateService probation,
        IAdminTransactionSubmitter transactionSubmitter,
        Services.IEmployeeAbsencePreviewService previewService)
    {
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
                UnpaidEndDate = input.UnpaidEndDate
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
                .WhereIf(input.EmployeeId.HasValue, a => a.EmployeeId == input.EmployeeId.Value);

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
            }
        };
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
        entity.CalendarDays = CalculateCalendarDays(entity.StartDate, entity.EndDate);

        // Calendar days less the week-offs, holidays and rest days the type excludes -- what the quota is
        // charged, as legacy's UpdateQuotaInformation computed it. CalendarDays keeps the raw span.
        entity.TotalLeaveDays = await _calculationService.CalculateTotalLeaveDaysAsync(entity);
        entity.InitialEndDate = entity.EndDate;

        await ValidateAsync(entity);
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
        await ApplyReturnAsync(entity, input, previousReturnDate: default);

        // Unpaid leave during probation pushes its end date out (legacy Employee.GetProbationEndDate, working-days mode).
        await _probation.RecalculateAfterLeaveChangeAsync(entity.EmployeeId, entity.AbsenceTypeId);
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

        // EARLY: the leave now ends the day before they came back, and its days go back to the quota. Legacy
        // kept the granted end date in InitialEndDate.
        if (daysDifference < 0)
        {
            var previousTotal = entity.TotalLeaveDays;

            entity.InitialEndDate = entity.EndDate;
            entity.EndDate = entity.ReturnDate.Date.AddDays(-1);
            entity.CalendarDays = CalculateCalendarDays(entity.StartDate, entity.EndDate);
            entity.TotalLeaveDays = await _calculationService.CalculateTotalLeaveDaysAsync(entity);
            entity.IsReturned = true;

            await _repository.UpdateAsync(entity);

            if (IsEffective(entity))
            {
                await _quotaLedger.ReleaseAsync(entity.EmployeeId, entity.AbsenceTypeId, previousTotal, entity.StartDate);
                await _quotaLedger.ConsumeAsync(entity.EmployeeId, entity.AbsenceTypeId, entity.TotalLeaveDays, entity.StartDate);
            }

            // The additional unpaid leave, when the return lands before or inside it.
            if (entity.ExtStartDate != null)
            {
                instruction.AdditionalLeaveAction = entity.ReturnDate.Date <= entity.ExtStartDate.Value.Date
                    ? Workflow.AdditionalLeaveAction.Delete
                    : Workflow.AdditionalLeaveAction.Shorten;
                instruction.AdditionalLeaveEndDate = entity.ReturnDate.Date.AddDays(-1);
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
        var wasEffective = IsEffective(entity);
        var previousReturnDate = entity.ReturnDate;

        MapToEntity(input, entity);
        entity.CalendarDays = CalculateCalendarDays(entity.StartDate, entity.EndDate);
        entity.TotalLeaveDays = await _calculationService.CalculateTotalLeaveDaysAsync(entity);

        await ValidateAsync(entity);
        await _repository.UpdateAsync(entity);

        await ApplyReturnAsync(entity, input, previousReturnDate);

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

        await _probation.RecalculateAfterLeaveChangeAsync(entity.EmployeeId, previousTypeId);
        if (entity.AbsenceTypeId != previousTypeId)
        {
            await _probation.RecalculateAfterLeaveChangeAsync(entity.EmployeeId, entity.AbsenceTypeId);
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
    }

    private Task ChargeQuotaAsync(EmployeeAbsence entity)
    {
        return IsEffective(entity)
            ? _quotaLedger.ConsumeAsync(entity.EmployeeId, entity.AbsenceTypeId, entity.TotalLeaveDays, entity.StartDate)
            : Task.CompletedTask;
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

    private async Task ValidateAsync(EmployeeAbsence entity)
    {
        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }

        // Pending leave counts as a conflict, so the filter is disabled for this check. Without it HR could
        // book leave over a request currently sitting with the employee's manager -- the pending row is
        // invisible to an ordinary query, so the overlap would simply not be found. Rejected and
        // sent-back rows do not block, since neither is a claim on the employee's time.
        bool overlaps;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            overlaps = await _repository.GetAll()
                .AnyAsync(a => a.Id != entity.Id
                               && a.EmployeeId == entity.EmployeeId
                               && a.StartDate.Date <= entity.EndDate.Date
                               && a.EndDate.Date >= entity.StartDate.Date
                               && (a.WorkflowStatus == null
                                   || a.WorkflowStatus == HCMv3.Workflow.RequestStatus.Approved
                                   || a.WorkflowStatus == HCMv3.Workflow.RequestStatus.Closed
                                   || a.WorkflowStatus == HCMv3.Workflow.RequestStatus.Open));
        }

        if (overlaps)
        {
            throw new UserFriendlyException(L("OverlapConflictFound"));
        }

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
        });
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
        entity.IsReturned = input.IsReturned;
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
