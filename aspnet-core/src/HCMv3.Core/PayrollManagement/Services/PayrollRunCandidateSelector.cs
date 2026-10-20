using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;

namespace HCMv3.PayrollManagement.Services;

/// <summary>One employee the Payroll Engine screen's employee picker offers, and whether it has already been run.</summary>
public class PayrollRunCandidate
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string FullNameEn { get; set; }

    public string FullNameAr { get; set; }

    /// <summary>A salary register already exists for this employee, run type, reason and period (legacy "Processed").</summary>
    public bool IsProcessed { get; set; }
}

/// <summary>
/// Who the Payroll Engine screen's employee picker lists for a payroll area, run type and off-cycle reason.
/// Port of legacy <c>uscPayrollEngine.BuildEmployeePopupList</c> (<c>uscPayrollEngine.ascx.cs</c> lines 788-1020).
///
/// <para>This only GUIDES the operator: it decides what the picker shows, not what the engine accepts. The engine
/// still applies its own selection (<c>PayrollEngineService.GetEmployeeListAsync</c>) to whatever codes are sent.</para>
/// </summary>
public interface IPayrollRunCandidateSelector : IDomainService
{
    /// <param name="payrollAreaName">Matched against <c>PayrollArea.Name</c>, as everywhere else on this screen.</param>
    /// <param name="payrollRunType">Regular or OffCycle.</param>
    /// <param name="offCycleReason">The reason's English name ("Advance Leave" / "Leave Encashment" / "End Of Service"). Ignored for Regular.</param>
    /// <param name="payrollDate">The period end the screen shows -- the control record's <c>CalendarMonthEnd</c> (legacy <c>txtEndDate</c>).</param>
    /// <param name="pickOffcyclePayment">Payroll setting <c>PickOffcyclePayment</c>.</param>
    /// <param name="today">The current date; a parameter so the rule is testable.</param>
    Task<List<PayrollRunCandidate>> GetCandidatesAsync(
        string payrollAreaName,
        PayrollRunType payrollRunType,
        string offCycleReason,
        DateTime payrollDate,
        PickOffcyclePayment pickOffcyclePayment,
        DateTime today);
}

/// <inheritdoc cref="IPayrollRunCandidateSelector"/>
/// <remarks>
/// Synchronous LINQ over <c>IRepository.GetAll()</c>, wrapped in a task, the same way
/// <see cref="EmployeePayrollAreaResolver"/> and the engine's own employee selection are written (Core has no EF Core
/// reference).
/// </remarks>
public class PayrollRunCandidateSelector : HCMv3DomainServiceBase, IPayrollRunCandidateSelector
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<SalaryRegister> _salaryRegisterRepository;
    private readonly IRepository<PayrollReasonType> _payrollReasonTypeRepository;
    private readonly IRepository<EmployeeAbsence> _employeeAbsenceRepository;
    private readonly IRepository<EmployeeAbsAdjustment> _employeeAbsAdjustmentRepository;
    private readonly IRepository<EmployeeFnFPayment> _employeeFnFPaymentRepository;
    private readonly IRepository<EmployeeSeparation> _employeeSeparationRepository;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;

    public PayrollRunCandidateSelector(
        IRepository<Employee> employeeRepository,
        IRepository<SalaryRegister> salaryRegisterRepository,
        IRepository<PayrollReasonType> payrollReasonTypeRepository,
        IRepository<EmployeeAbsence> employeeAbsenceRepository,
        IRepository<EmployeeAbsAdjustment> employeeAbsAdjustmentRepository,
        IRepository<EmployeeFnFPayment> employeeFnFPaymentRepository,
        IRepository<EmployeeSeparation> employeeSeparationRepository,
        IEmployeePayrollAreaResolver payrollAreaResolver)
    {
        _employeeRepository = employeeRepository;
        _salaryRegisterRepository = salaryRegisterRepository;
        _payrollReasonTypeRepository = payrollReasonTypeRepository;
        _employeeAbsenceRepository = employeeAbsenceRepository;
        _employeeAbsAdjustmentRepository = employeeAbsAdjustmentRepository;
        _employeeFnFPaymentRepository = employeeFnFPaymentRepository;
        _employeeSeparationRepository = employeeSeparationRepository;
        _payrollAreaResolver = payrollAreaResolver;
    }

    public async Task<List<PayrollRunCandidate>> GetCandidatesAsync(
        string payrollAreaName,
        PayrollRunType payrollRunType,
        string offCycleReason,
        DateTime payrollDate,
        PickOffcyclePayment pickOffcyclePayment,
        DateTime today)
    {
        // Legacy guard checks (lines 791-793): no area or no period date => nothing to list.
        if (string.IsNullOrWhiteSpace(payrollAreaName) || payrollDate == default(DateTime))
        {
            return new List<PayrollRunCandidate>();
        }

        payrollDate = payrollDate.Date;
        var reasonType = (offCycleReason ?? string.Empty).Trim();

        // Legacy lines 833-847: employees whose org assignment covering the period end is in this payroll area and who
        // had joined by then. The area lookup goes through the resolver, as the engine's does.
        var employeeIdsInArea = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName, payrollDate);
        if (!employeeIdsInArea.Any())
        {
            return new List<PayrollRunCandidate>();
        }

        var employees = _employeeRepository.GetAll()
            .Where(e => employeeIdsInArea.Contains(e.Id) && e.JoiningDate <= payrollDate)
            .Select(e => new { e.Id, e.Code, e.FullNameEn, e.FullNameAr, e.Status })
            .ToList();

        if (!employees.Any())
        {
            return new List<PayrollRunCandidate>();
        }

        var activeIds = employees.Where(e => e.Status == EmployeeStatus.Active).Select(e => e.Id).ToList();
        var inactiveIds = employees.Where(e => e.Status == EmployeeStatus.Inactive).Select(e => e.Id).ToList();

        HashSet<int> eligible;
        HashSet<int> processed;

        if (payrollRunType == PayrollRunType.Regular)
        {
            // Legacy lines 871-885: every active employee; processed = a regular register for this very period.
            eligible = new HashSet<int>(activeIds);
            processed = new HashSet<int>(_salaryRegisterRepository.GetAll()
                .Where(sr => activeIds.Contains(sr.EmployeeId)
                             && sr.PayrollRunType == PayrollRunType.Regular
                             && sr.ForPeriod == sr.InPeriod
                             && sr.ForPeriod == payrollDate)
                .Select(sr => sr.EmployeeId)
                .ToList());
        }
        else
        {
            // Legacy lines 891-904: an off-cycle is booked into the NEXT period, and payment records are picked up to
            // today (when the setting says so and today is inside that period) or to that period's end.
            var offCycleDate = PayrollControlRecordGuard.MonthEnd(payrollDate.AddMonths(1));
            var periodStart = new DateTime(offCycleDate.Year, offCycleDate.Month, 1);
            var pickUpTo = PickUpToDate(pickOffcyclePayment, periodStart, offCycleDate, today);

            // Legacy lines 810-816: the reason row is found by its English name.
            var reasonTypeId = string.IsNullOrEmpty(reasonType)
                ? 0
                : _payrollReasonTypeRepository.GetAll()
                    .Where(r => r.NameEn == reasonType)
                    .Select(r => r.Id)
                    .FirstOrDefault();

            if (reasonType == PayrollOffCycleReasons.AdvanceLeave || reasonType == PayrollOffCycleReasons.LeaveEncashment)
            {
                processed = OffCycleProcessed(activeIds, reasonTypeId, offCycleDate);

                var encashment = AdvancePaidEncashmentIds(activeIds, periodStart, pickUpTo);

                if (reasonType == PayrollOffCycleReasons.AdvanceLeave)
                {
                    // Legacy lines 922-934 + 958-960: paid leave set to Pay In Advance, plus the encashments.
                    var advanceLeave = _employeeAbsenceRepository.GetAll()
                        .Where(a => activeIds.Contains(a.EmployeeId)
                                    && a.AbsenceType.IsPaid
                                    && a.LeavePaymentType == LeavePaymentType.PayInAdvance
                                    && a.PaymentDate >= periodStart
                                    && a.PaymentDate <= pickUpTo)
                        .Select(a => a.EmployeeId)
                        .ToList();

                    eligible = new HashSet<int>(advanceLeave.Union(encashment));
                }
                else
                {
                    eligible = new HashSet<int>(encashment);
                }
            }
            else if (reasonType == PayrollOffCycleReasons.EndOfService)
            {
                // Legacy lines 963-1003: INACTIVE employees with a full-and-final payment this period AND a separation
                // that has not been revoked.
                processed = OffCycleProcessed(inactiveIds, reasonTypeId, offCycleDate);

                var fnf = _employeeFnFPaymentRepository.GetAll()
                    .Where(f => inactiveIds.Contains(f.EmployeeId)
                                && f.CompensationDate >= periodStart
                                && f.CompensationDate <= pickUpTo)
                    .Select(f => f.EmployeeId)
                    .ToList();

                var separated = _employeeSeparationRepository.GetAll()
                    .Where(s => inactiveIds.Contains(s.EmployeeId) && s.RevokeDate == null)
                    .Select(s => s.EmployeeId)
                    .ToList();

                eligible = new HashSet<int>(fnf.Intersect(separated));
            }
            else
            {
                // Legacy builds no list for any other reason.
                eligible = new HashSet<int>();
                processed = new HashSet<int>();
            }
        }

        // Legacy lines 1009-1019.
        return employees
            .Where(e => eligible.Contains(e.Id))
            .Select(e => new PayrollRunCandidate
            {
                EmployeeId = e.Id,
                EmployeeCode = e.Code,
                FullNameEn = e.FullNameEn,
                FullNameAr = e.FullNameAr,
                IsProcessed = processed.Contains(e.Id)
            })
            .ToList();
    }

    /// <summary>
    /// Legacy lines 898-903. Note the "current month" check uses the off-cycle period (the month after the control
    /// record), not the control record's own month.
    /// </summary>
    public static DateTime PickUpToDate(PickOffcyclePayment pickOffcyclePayment, DateTime periodStart, DateTime periodEnd, DateTime today)
    {
        today = today.Date;

        return pickOffcyclePayment == PickOffcyclePayment.CurrentDate && periodStart <= today && periodEnd >= today
            ? today
            : periodEnd;
    }

    private HashSet<int> OffCycleProcessed(List<int> employeeIds, int reasonTypeId, DateTime offCycleDate)
    {
        return new HashSet<int>(_salaryRegisterRepository.GetAll()
            .Where(sr => employeeIds.Contains(sr.EmployeeId)
                         && sr.PayrollRunType == PayrollRunType.OffCycle
                         && sr.PayrollReasonTypeId != null
                         && sr.PayrollReasonTypeId == reasonTypeId
                         && sr.ForPeriod == sr.InPeriod
                         && sr.ForPeriod == offCycleDate)
            .Select(sr => sr.EmployeeId)
            .ToList());
    }

    /// <summary>Legacy lines 937-952: annual-leave encashment / fractional adjustments paid in advance this period.</summary>
    private List<int> AdvancePaidEncashmentIds(List<int> employeeIds, DateTime periodStart, DateTime pickUpTo)
    {
        return _employeeAbsAdjustmentRepository.GetAll()
            .Where(a => employeeIds.Contains(a.EmployeeId)
                        && (a.AbsenceType.Code == "1" || a.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                        && a.EffectiveDate >= periodStart
                        && a.EffectiveDate <= pickUpTo
                        && (a.AdjustmentType == AdjustmentType.Encashment || a.AdjustmentType == AdjustmentType.Fractional)
                        && a.AdjustmentPaymentType == AdjustmentPaymentType.PayInAdvance)
            .Select(a => a.EmployeeId)
            .ToList();
    }
}
