using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement.Services;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ILeaveReturnProcessor"/>
/// <remarks>
/// Ported from legacy <c>EmployeeAbsence.SaveDiffInformation</c> (2428-2596), the matching branches of
/// <c>AbsenceRequest</c>'s approval (1457-1819) and <c>UpdateReturnInfoInConsecutiveLeave</c> (2597-2680).
/// Legacy applied the same routine whether HR typed the return date onto the leave or an approver granted a
/// return request; this is that routine, and both callers use it.
/// </remarks>
[UnitOfWork]
public class LeaveReturnProcessor : HCMv3DomainServiceBase, ILeaveReturnProcessor
{
    /// <summary>Legacy hard-codes these: "1" annual (paid late days are measured against it), "2" unpaid.</summary>
    public const string AnnualLeaveTypeCode = "1";

    /// <inheritdoc cref="AnnualLeaveTypeCode"/>
    public const string UnpaidLeaveTypeCode = "2";

    private readonly IRepository<EmployeeAbsence> _absences;
    private readonly IRepository<AbsenceType> _absenceTypes;
    private readonly IRepository<Employee> _employees;
    private readonly IEmployeeAbsenceCalculationService _calculation;
    private readonly ILeaveQuotaLedger _quotaLedger;
    private readonly ISysSettingsAccessor _settings;
    private readonly ITimeDataService _timeDataService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LeaveReturnProcessor(
        IRepository<EmployeeAbsence> absences,
        IRepository<AbsenceType> absenceTypes,
        IRepository<Employee> employees,
        IEmployeeAbsenceCalculationService calculation,
        ILeaveQuotaLedger quotaLedger,
        ISysSettingsAccessor settings,
        ITimeDataService timeDataService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _absences = absences;
        _absenceTypes = absenceTypes;
        _employees = employees;
        _calculation = calculation;
        _quotaLedger = quotaLedger;
        _settings = settings;
        _timeDataService = timeDataService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public Task<double> GetDaysDifferenceAsync(EmployeeAbsence leave, DateTime returnDate)
    {
        // With additional unpaid leave attached the leave really ends when THAT ends (legacy AbsenceRequest
        // 380-383), so lateness is measured from there.
        var lastDate = leave.ExtEndDate?.Date ?? leave.EndDate.Date;

        return _calculation.GetReturnDaysDifferenceAsync(leave.EmployeeId, leave.AbsenceTypeId, lastDate, returnDate.Date);
    }

    public async Task ValidateLateReturnAsync(LeaveReturnInstruction instruction)
    {
        var leave = instruction.Leave;
        var returnDate = instruction.ReturnDate.Date;
        var lastDate = leave.ExtEndDate?.Date ?? leave.EndDate.Date;

        var daysDifference = await GetDaysDifferenceAsync(leave, returnDate);

        if (daysDifference <= 0)
        {
            throw new UserFriendlyException(L("ThisReturnIsNotLate"));
        }

        var paidCalendarDays = CalendarDaysOf(instruction.PaidStartDate, instruction.PaidEndDate);
        var unpaidCalendarDays = CalendarDaysOf(instruction.UnpaidStartDate, instruction.UnpaidEndDate);

        if (paidCalendarDays + unpaidCalendarDays != daysDifference)
        {
            throw new UserFriendlyException(L(
                "LateReturnDaysMustEqualTheGap",
                $"{daysDifference:0.##}",
                $"{paidCalendarDays:0.##}",
                $"{unpaidCalendarDays:0.##}"));
        }

        var dayAfterLeave = lastDate.AddDays(1).ToString("dd/MM/yyyy");
        var dayBeforeReturn = returnDate.AddDays(-1).ToString("dd/MM/yyyy");

        if (paidCalendarDays > 0)
        {
            var paidStart = instruction.PaidStartDate!.Value.Date;
            var paidEnd = instruction.PaidEndDate!.Value.Date;

            if (!(lastDate < paidStart && paidStart < returnDate) || !(lastDate < paidEnd && paidEnd < returnDate))
            {
                throw new UserFriendlyException(L("LateReturnPaidDatesMustBeBetweenEndAndReturn"));
            }

            if (paidStart != lastDate.AddDays(1))
            {
                throw new UserFriendlyException(L("LateReturnPaidStartDateShouldBe", dayAfterLeave));
            }

            if (unpaidCalendarDays == 0 && paidEnd != returnDate.AddDays(-1))
            {
                throw new UserFriendlyException(L("LateReturnPaidEndDateShouldBe", dayBeforeReturn));
            }
        }

        if (unpaidCalendarDays > 0)
        {
            var unpaidStart = instruction.UnpaidStartDate!.Value.Date;
            var unpaidEnd = instruction.UnpaidEndDate!.Value.Date;

            if (!(lastDate <= unpaidStart && unpaidStart <= returnDate) || !(lastDate <= unpaidEnd && unpaidEnd <= returnDate))
            {
                throw new UserFriendlyException(L("LateReturnUnpaidDatesMustBeBetweenEndAndReturn"));
            }

            if (unpaidEnd != returnDate.AddDays(-1))
            {
                throw new UserFriendlyException(L("LateReturnUnpaidEndDateShouldBe", dayBeforeReturn));
            }

            if (paidCalendarDays == 0 && unpaidStart != lastDate.AddDays(1))
            {
                throw new UserFriendlyException(L("LateReturnUnpaidStartDateShouldBe", dayAfterLeave));
            }

            if (paidCalendarDays > 0)
            {
                var paidStart = instruction.PaidStartDate!.Value.Date;
                var paidEnd = instruction.PaidEndDate!.Value.Date;

                if ((paidStart <= unpaidStart && unpaidStart <= paidEnd) || (paidStart <= unpaidEnd && unpaidEnd <= paidEnd)
                    || (unpaidStart <= paidStart && paidStart <= unpaidEnd) || (unpaidStart <= paidEnd && paidEnd <= unpaidEnd))
                {
                    throw new UserFriendlyException(L("LateReturnPaidAndUnpaidDatesOverlap"));
                }
            }

            if (FindAbsenceTypeByCode(UnpaidLeaveTypeCode) == null)
            {
                throw new UserFriendlyException(L("UnpaidLeaveTypeNotConfigured", UnpaidLeaveTypeCode));
            }
        }

        EnsureNoLeaveStartsInLateGap(leave, returnDate, lastDate);
    }

    public async Task ApplyAsync(LeaveReturnInstruction instruction)
    {
        var leave = instruction.Leave;
        var returnDate = instruction.ReturnDate.Date;

        var daysDifference = await GetDaysDifferenceAsync(leave, returnDate);

        if (daysDifference > 0)
        {
            await ApplyLateReturnAsync(instruction, returnDate);
        }
        else
        {
            await ApplyAdditionalLeaveActionAsync(instruction);
        }

        await MarkPreviousConsecutiveLeaveReturnedAsync(leave);
    }

    /// <summary>
    /// Legacy's late-joining branch: the late gap becomes up to two NEW approved leave rows -- paid of the
    /// leave's own type up to the balance, then unpaid (code "2") -- and the original leave is only marked
    /// returned, keeping its dates and, as legacy left it, no rejoining date of its own.
    ///
    /// <para>
    /// <b>One deliberate difference from legacy.</b> Legacy reset its cursor to the original leave's end before
    /// placing the unpaid row (AbsenceRequest line 1572), so with BOTH ranges the unpaid leave overlapped the
    /// paid one and the tail of the gap was left uncovered. Here the unpaid row starts after the paid row; with
    /// only one of the two ranges -- the usual case -- the result is identical to legacy.
    /// </para>
    /// </summary>
    private async Task ApplyLateReturnAsync(LeaveReturnInstruction instruction, DateTime returnDate)
    {
        await ValidateLateReturnAsync(instruction);

        var leave = instruction.Leave;
        var cursor = leave.ExtEndDate?.Date ?? leave.EndDate.Date;

        var deductFromQuota = await PaidLateDaysToDeductAsync(instruction);
        var unpaidType = FindAbsenceTypeByCode(UnpaidLeaveTypeCode);
        var unpaidDays = unpaidType == null ? 0 : await UnpaidLateDaysAsync(instruction, unpaidType.Id);
        var unpaidCalendarDays = CalendarDaysOf(instruction.UnpaidStartDate, instruction.UnpaidEndDate);

        if (deductFromQuota > 0 && CalendarDaysOf(instruction.PaidStartDate, instruction.PaidEndDate) != 0)
        {
            var paid = await InsertLateReturnLeaveAsync(
                instruction, leave.AbsenceTypeId, cursor, returnDate, deductFromQuota,
                stampReturn: unpaidDays <= 0);

            if (paid != null)
            {
                cursor = paid.EndDate.Date;
            }
        }

        if (unpaidDays > 0)
        {
            await InsertLateReturnLeaveAsync(
                instruction, unpaidType.Id, cursor, returnDate, unpaidCalendarDays, stampReturn: true);
        }

        leave.IsReturned = true;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await _absences.UpdateAsync(leave);
        }
    }

    /// <summary>
    /// Places one generated late-return leave: the first countable day after <paramref name="lastDate"/>,
    /// ending on the <paramref name="dayCount"/>-th countable day.
    /// </summary>
    private async Task<EmployeeAbsence> InsertLateReturnLeaveAsync(
        LeaveReturnInstruction instruction,
        int absenceTypeId,
        DateTime lastDate,
        DateTime returnDate,
        double dayCount,
        bool stampReturn)
    {
        var original = instruction.Leave;

        var startDate = await _calculation.CalcLeaveStartDateAsync(original.EmployeeId, absenceTypeId, lastDate, returnDate);
        var endDate = await _calculation.CalcLeaveEndDateAsync(original.EmployeeId, absenceTypeId, lastDate, dayCount);

        if (startDate == null || endDate == null)
        {
            if (dayCount >= 1)
            {
                throw new UserFriendlyException(L("CannotGenerateLateReturnLeave"));
            }

            return null;
        }

        var totalLeaveDays = await _calculation.CalculateTotalLeaveDaysAsync(
            original.EmployeeId, absenceTypeId, startDate.Value, endDate.Value, 0);

        var generated = new EmployeeAbsence
        {
            TenantId = original.TenantId,
            EmployeeId = original.EmployeeId,
            AbsenceTypeId = absenceTypeId,
            StartDate = startDate.Value,
            EndDate = endDate.Value,
            InitialEndDate = endDate.Value,
            CalendarDays = (endDate.Value - startDate.Value).TotalDays + 1,
            TotalLeaveDays = totalLeaveDays,
            LeavePaymentType = LeavePaymentType.PayWithPayroll,
            Comments = original.Comments,

            // Born approved -- whoever agreed to the return agreed to these days -- and linked to the request
            // that approved them when there was one, so the history can say where they came from.
            WorkflowStatus = RequestStatus.Approved,
            WorkflowRequestId = instruction.WorkflowRequestId
        };

        if (stampReturn)
        {
            generated.ReturnDate = returnDate;
            generated.IsReturned = true;
        }

        await _absences.InsertAsync(generated);

        // Flushed before the next placement: the date walker skips days on which another leave starts, and it
        // can only see this one once it is in the database.
        await _unitOfWorkManager.Current.SaveChangesAsync();

        await _quotaLedger.ConsumeAsync(original.EmployeeId, absenceTypeId, totalLeaveDays, generated.StartDate);

        return generated;
    }

    /// <summary>
    /// What an early return does to additional unpaid leave booked with this one (legacy <c>EarlyJoinType</c>):
    /// removed when the employee came back before it started, cut short when they came back during it.
    /// </summary>
    private async Task ApplyAdditionalLeaveActionAsync(LeaveReturnInstruction instruction)
    {
        var leave = instruction.Leave;

        if (instruction.AdditionalLeaveAction == AdditionalLeaveAction.None
            || leave.ExtAbsenceTypeId == null
            || leave.ExtStartDate == null
            || leave.ExtEndDate == null)
        {
            return;
        }

        var extStart = leave.ExtStartDate.Value.Date;
        var extEnd = leave.ExtEndDate.Value.Date;

        EmployeeAbsence unpaid;
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            unpaid = _absences.GetAll().FirstOrDefault(a => a.EmployeeId == leave.EmployeeId
                                                            && a.AbsenceTypeId == leave.ExtAbsenceTypeId.Value
                                                            && a.StartDate == extStart
                                                            && a.EndDate == extEnd);
        }

        if (unpaid == null)
        {
            return;
        }

        if (instruction.AdditionalLeaveAction == AdditionalLeaveAction.Delete)
        {
            using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                await _absences.DeleteAsync(unpaid);
            }

            await _quotaLedger.ReleaseAsync(unpaid.EmployeeId, unpaid.AbsenceTypeId, unpaid.TotalLeaveDays, unpaid.StartDate);

            leave.ExtAbsenceTypeId = null;
            leave.ExtStartDate = null;
            leave.ExtEndDate = null;
            leave.ExtTotalLeaveDays = null;
            return;
        }

        var newEnd = instruction.AdditionalLeaveEndDate?.Date ?? unpaid.EndDate.Date;
        var previousDays = unpaid.TotalLeaveDays;

        unpaid.InitialEndDate = unpaid.EndDate;
        unpaid.EndDate = newEnd;
        unpaid.CalendarDays = (newEnd - unpaid.StartDate.Date).TotalDays + 1;
        unpaid.TotalLeaveDays = await _calculation.CalculateTotalLeaveDaysAsync(
            unpaid.EmployeeId, unpaid.AbsenceTypeId, unpaid.StartDate.Date, newEnd, 0);

        // The return belongs to the LAST row the employee was actually on, as legacy stamped it.
        unpaid.ReturnDate = instruction.ReturnDate.Date;
        unpaid.IsReturned = true;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            await _absences.UpdateAsync(unpaid);
        }

        leave.ExtEndDate = newEnd;
        leave.ExtTotalLeaveDays = unpaid.TotalLeaveDays;

        await _quotaLedger.ReleaseAsync(unpaid.EmployeeId, unpaid.AbsenceTypeId, previousDays, unpaid.StartDate);
        await _quotaLedger.ConsumeAsync(unpaid.EmployeeId, unpaid.AbsenceTypeId, unpaid.TotalLeaveDays, unpaid.StartDate);
    }

    /// <summary>
    /// Legacy <c>UpdateReturnInfoInConsecutiveLeave</c> (2597-2680), with
    /// <c>TimeSettings.EnableAutoReturnInConsecutiveLeave</c> on: leave taken back-to-back before this one is
    /// marked returned too, so payroll does not hold those employees as "never came back". The walk steps over
    /// week-offs and public holidays between two stretches of leave, and stops at the first working day the
    /// employee was actually present.
    ///
    /// <para>Only IsReturned is set, not a rejoining date -- the employee did not come back on those days.</para>
    /// </summary>
    private async Task MarkPreviousConsecutiveLeaveReturnedAsync(EmployeeAbsence leave)
    {
        var timeSettings = await _settings.GetTimeSettingsAsync();
        if (timeSettings?.EnableAutoReturnInConsecutiveLeave != true)
        {
            return;
        }

        var employee = _employees.GetAll().FirstOrDefault(e => e.Id == leave.EmployeeId);
        if (employee == null)
        {
            return;
        }

        var earliest = timeSettings.EarliestEvaluationDate.Date;
        var day = leave.StartDate.Date.AddDays(-1);

        // Ten years of calendar is far past any run of back-to-back leave; legacy had no bound at all.
        for (var walked = 0; walked < 3650 && day >= earliest; walked++)
        {
            EmployeeAbsence previous;
            using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
            {
                previous = _absences.GetAll()
                    .Where(a => a.EmployeeId == leave.EmployeeId
                                && a.AbsenceHours == 0
                                && !a.IsReturned
                                && a.ReturnDate == default(DateTime)
                                && a.EndDate == day
                                && a.StartDate >= earliest
                                && (a.WorkflowStatus == null
                                    || a.WorkflowStatus == RequestStatus.Approved
                                    || a.WorkflowStatus == RequestStatus.Closed))
                    .OrderByDescending(a => a.StartDate)
                    .FirstOrDefault();
            }

            if (previous != null)
            {
                previous.IsReturned = true;

                using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
                {
                    await _absences.UpdateAsync(previous);
                }

                day = previous.StartDate.Date.AddDays(-1);
                continue;
            }

            var isHoliday = await _timeDataService.IsHolidayAsync(day, null, employee);
            var isWeekOff = !isHoliday && await _timeDataService.IsWeekEndAsync(day, null, employee);

            if (!isHoliday && !isWeekOff)
            {
                return;
            }

            day = day.AddDays(-1);
        }
    }

    /// <summary>
    /// Legacy "Leave entry exists": another leave starting after this one ends and on or before the rejoining
    /// date means the gap is not really a gap, and booking it would double-count those days.
    /// </summary>
    private void EnsureNoLeaveStartsInLateGap(EmployeeAbsence leave, DateTime returnDate, DateTime lastDate)
    {
        var rejoin = returnDate.Date.AddDays(1);

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var other = _absences.GetAll()
                .Where(a => a.Id != leave.Id
                            && a.EmployeeId == leave.EmployeeId
                            && a.StartDate > lastDate
                            && a.StartDate < rejoin
                            && (a.WorkflowStatus == null
                                || a.WorkflowStatus == RequestStatus.Approved
                                || a.WorkflowStatus == RequestStatus.Closed
                                || a.WorkflowStatus == RequestStatus.Open))
                .Select(a => new { a.StartDate, a.EndDate })
                .FirstOrDefault();

            if (other != null)
            {
                throw new UserFriendlyException(L(
                    "LeaveEntryExistsInLateReturnGap",
                    other.StartDate.ToString("dd/MM/yyyy"),
                    other.EndDate.ToString("dd/MM/yyyy")));
            }
        }
    }

    /// <summary>
    /// Legacy <c>DeductFromQuota</c>: the paid range's calendar days less the days the ANNUAL leave type
    /// (code "1") excludes -- legacy measures against annual leave whatever the leave's own type is. Falls back
    /// to the leave's own type when no code-"1" type exists.
    /// </summary>
    private async Task<double> PaidLateDaysToDeductAsync(LeaveReturnInstruction instruction)
    {
        if (!instruction.PaidStartDate.HasValue || !instruction.PaidEndDate.HasValue)
        {
            return 0;
        }

        var leave = instruction.Leave;
        var start = instruction.PaidStartDate.Value.Date;
        var end = instruction.PaidEndDate.Value.Date;
        var measuringTypeId = FindAbsenceTypeByCode(AnnualLeaveTypeCode)?.Id ?? leave.AbsenceTypeId;

        var excluded = (double)await _calculation.ActualLeaveDaysAsync(leave.EmployeeId, start, end, measuringTypeId);

        return (end - start).TotalDays + 1 - excluded;
    }

    /// <summary>Legacy <c>UnpaidDays</c>: the unpaid range less what the unpaid type (code "2") excludes.</summary>
    private async Task<double> UnpaidLateDaysAsync(LeaveReturnInstruction instruction, int unpaidTypeId)
    {
        if (!instruction.UnpaidStartDate.HasValue || !instruction.UnpaidEndDate.HasValue)
        {
            return 0;
        }

        var start = instruction.UnpaidStartDate.Value.Date;
        var end = instruction.UnpaidEndDate.Value.Date;

        var excluded = (double)await _calculation.ActualLeaveDaysAsync(instruction.Leave.EmployeeId, start, end, unpaidTypeId);

        return (end - start).TotalDays + 1 - excluded;
    }

    private static double CalendarDaysOf(DateTime? start, DateTime? end)
    {
        return start.HasValue && end.HasValue ? (end.Value.Date - start.Value.Date).TotalDays + 1 : 0;
    }

    private AbsenceType FindAbsenceTypeByCode(string code)
    {
        return _absenceTypes.GetAll().FirstOrDefault(t => t.Code == code);
    }
}
