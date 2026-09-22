using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.TimeManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ISickLeaveDeductionPreview"/>
/// <remarks>
/// Ported from legacy <c>EmployeeAbsence.GetSickLeaveUnpaidInfo</c> (lines 1014-1100). Legacy built a string;
/// this returns the segments, so the screen can render them and a test can assert them.
///
/// <para>
/// The walk is legacy's: step through the requested days, skip the ones the absence type excludes (they are not
/// sick days), add one to the running total of sick days taken this accrual year, and look up the first slab
/// whose ceiling still covers that total. Each time the slab changes, the stretch that just ended becomes a
/// segment. Past the last slab the rest of the leave is a flat 100% deduction.
/// </para>
/// <para>
/// Legacy gated this on absence type code "5" (sick leave unpaid), which is the type the employee applies for;
/// the slabs themselves hang off the sub-group's code-"3" sick leave entitlement. Both are kept.
/// </para>
/// </remarks>
[UnitOfWork]
public class SickLeaveDeductionPreview : HCMv3DomainServiceBase, ISickLeaveDeductionPreview
{
    /// <summary>Legacy hard-codes these: "3" paid sick leave (carries the slabs), "5" the unpaid sick leave applied for.</summary>
    public const string SickLeaveTypeCode = "3";

    /// <inheritdoc cref="SickLeaveTypeCode"/>
    public const string SickLeaveUnpaidTypeCode = "5";

    private readonly IRepository<AbsenceType> _absenceTypes;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignments;
    private readonly IRepository<EmployeeSubGroupAbsence> _subGroupAbsences;
    private readonly IRepository<AbsenceDedRuleDetail> _slabs;
    private readonly IRepository<TimeRegister> _timeRegisters;
    private readonly IAccrualService _accrualService;
    private readonly IEmployeeAbsenceCalculationService _calculation;

    public SickLeaveDeductionPreview(
        IRepository<AbsenceType> absenceTypes,
        IRepository<EmployeeOrgAssignment> orgAssignments,
        IRepository<EmployeeSubGroupAbsence> subGroupAbsences,
        IRepository<AbsenceDedRuleDetail> slabs,
        IRepository<TimeRegister> timeRegisters,
        IAccrualService accrualService,
        IEmployeeAbsenceCalculationService calculation)
    {
        _absenceTypes = absenceTypes;
        _orgAssignments = orgAssignments;
        _subGroupAbsences = subGroupAbsences;
        _slabs = slabs;
        _timeRegisters = timeRegisters;
        _accrualService = accrualService;
        _calculation = calculation;
    }

    public async Task<List<SickLeaveDeductionSegment>> GetSegmentsAsync(
        int employeeId, int absenceTypeId, DateTime startDate, DateTime endDate)
    {
        var segments = new List<SickLeaveDeductionSegment>();

        var start = startDate.Date;
        var end = endDate.Date;

        if (start == default || end == default || end < start)
        {
            return segments;
        }

        var absenceType = _absenceTypes.GetAll().FirstOrDefault(t => t.Id == absenceTypeId);
        if (absenceType == null || absenceType.Code != SickLeaveUnpaidTypeCode)
        {
            return segments;
        }

        var subGroupId = _orgAssignments.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= start && a.EndDate >= start)
            .OrderByDescending(a => a.StartDate)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefault();

        if (subGroupId == null)
        {
            return segments;
        }

        // The slabs live on the sub-group's PAID sick leave entitlement, whatever type the employee applied for.
        var entitlement = _subGroupAbsences.GetAll()
            .Where(a => a.EmployeeSubGroupId == subGroupId.Value
                        && a.StartDate <= start
                        && a.EndDate >= start
                        && (a.AbsenceType.Code == SickLeaveTypeCode || a.AbsenceType.QuotaType == QuotaType.SickLeave))
            .Select(a => new { a.AbsenceDeductionRuleId, a.AbsenceTypeId })
            .FirstOrDefault();

        if (entitlement?.AbsenceDeductionRuleId == null)
        {
            return segments;
        }

        var slabs = _slabs.GetAll()
            .Where(d => d.AbsenceDeductionRuleId == entitlement.AbsenceDeductionRuleId.Value
                        && d.StartDate <= start
                        && d.EndDate >= start)
            .OrderBy(d => d.DaysCount)
            .Select(d => new Slab { DaysCount = d.DaysCount, DeductionNumber = d.DeductionNumber, DeductionUnit = d.DeductionUnit })
            .ToList();

        if (slabs.Count == 0)
        {
            return segments;
        }

        var (deductionStart, _) = await _accrualService.GetLeaveAccrualStartEndAsync(
            employeeId, entitlement.AbsenceTypeId, start);

        // Sick days already taken this accrual year -- where in the slabs this request begins.
        var alreadyTaken = _timeRegisters.GetAll()
            .Where(r => r.EmployeeId == employeeId && r.DayDate >= deductionStart && r.DayDate < start)
            .Select(r => (double?)r.SickLeaveUnpaidDay)
            .Sum() ?? 0;

        var currentSlab = slabs.FirstOrDefault(s => s.DaysCount > alreadyTaken);

        if (currentSlab == null)
        {
            // The employee is already past the last slab: nothing here is paid.
            segments.Add(FullDeduction(start, end));
            return segments;
        }

        var segmentStart = start;

        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var excluded = (double)await _calculation.ActualLeaveDaysAsync(employeeId, day, day, absenceTypeId);
            if (excluded != 0)
            {
                // A week-off or holiday inside the leave is not a sick day and does not move the slabs.
                continue;
            }

            alreadyTaken++;

            var nextSlab = slabs.FirstOrDefault(s => s.DaysCount >= alreadyTaken);

            if (nextSlab == currentSlab)
            {
                continue;
            }

            segments.Add(new SickLeaveDeductionSegment
            {
                FromDate = segmentStart,
                ToDate = day.AddDays(-1),
                DeductionNumber = currentSlab.DeductionNumber,
                DeductionUnit = currentSlab.DeductionUnit,
            });

            currentSlab = nextSlab;
            segmentStart = day;

            if (nextSlab == null)
            {
                segments.Add(FullDeduction(day, end));
                return segments;
            }
        }

        if (segmentStart <= end)
        {
            segments.Add(new SickLeaveDeductionSegment
            {
                FromDate = segmentStart,
                ToDate = end,
                DeductionNumber = currentSlab.DeductionNumber,
                DeductionUnit = currentSlab.DeductionUnit,
            });
        }

        return segments;
    }

    private static SickLeaveDeductionSegment FullDeduction(DateTime from, DateTime to) => new()
    {
        FromDate = from,
        ToDate = to,
        DeductionNumber = 100,
        DeductionUnit = DeductionUnit.Percentage,
        IsBeyondTheLastSlab = true,
    };

    private sealed class Slab
    {
        public double DaysCount { get; init; }
        public double DeductionNumber { get; init; }
        public DeductionUnit DeductionUnit { get; init; }
    }
}
