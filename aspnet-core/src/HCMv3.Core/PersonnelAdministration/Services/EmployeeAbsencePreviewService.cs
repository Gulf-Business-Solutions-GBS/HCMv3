using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;
using HCMv3.TimeManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeeAbsencePreviewService"/>
/// <remarks>
/// Line numbers are in legacy <c>EmployeeAbsence.cs</c> unless stated otherwise. Kept as legacy had them (payroll
/// parity), each pinned by a test in <c>EmployeeAbsencePreviewService_Tests</c>:
/// <list type="bullet">
/// <item><description><c>DeductFromQuota</c> and <c>UnpaidDays</c> are zero until a <c>ReturnDate</c> is entered
/// (<c>ReturnDate.Year != 1</c>, lines 845, 950), while the two calendar-day counts beside them are not
/// (lines 826, 932).</description></item>
/// <item><description><c>DeductFromQuota</c> always excludes days by the Code "1" (annual) type's rules and
/// <c>UnpaidDays</c> by the Code "2" (unpaid) type's, whatever type the leave itself is (lines 847, 953).
/// </description></item>
/// <item><description>Both swallow any error and show zero (empty <c>catch</c>, lines 859-860 / 965-966).</description></item>
/// <item><description><c>CurrentBalance</c> of a non-accrual type counts EVERY leave of that type in the period,
/// including the one being edited (legacy <c>ignoreOid = -1</c>, line 3195).</description></item>
/// <item><description><c>CurrentBalance</c> of an accrual type reads the quota row whose DEDUCTION period covers
/// the whole leave, an annual-group type being swapped for Code "1" (EmployeeAbsenceQuota.cs
/// <c>GetEmployeeAbsenceQuota(emp, type, start, end, session)</c>).</description></item>
/// </list>
/// </remarks>
[UnitOfWork]
public class EmployeeAbsencePreviewService : HCMv3DomainServiceBase, IEmployeeAbsencePreviewService
{
    private const string AnnualLeaveCode = "1";
    private const string UnpaidLeaveCode = "2";
    private const string SickLeaveCode = "3";

    /// <summary>Legacy <c>TimeTransfer.TransferType.Code == "2"</c>: opening sick leave (line 3239).</summary>
    private const string OpeningSickLeaveTransferCode = "2";

    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly IRepository<EmployeeAbsenceQuota> _quotaRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IRepository<TimeTransfer> _timeTransferRepository;
    private readonly IEmployeeAbsenceCalculationService _calculation;
    private readonly ISickLeaveDeductionPreview _sickLeavePreview;
    private readonly IAccrualService _accrualService;

    public EmployeeAbsencePreviewService(
        IRepository<Employee> employeeRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<EmployeeAbsence> absenceRepository,
        IRepository<EmployeeAbsenceQuota> quotaRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IRepository<TimeTransfer> timeTransferRepository,
        IEmployeeAbsenceCalculationService calculation,
        ISickLeaveDeductionPreview sickLeavePreview,
        IAccrualService accrualService)
    {
        _employeeRepository = employeeRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _absenceRepository = absenceRepository;
        _quotaRepository = quotaRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _timeTransferRepository = timeTransferRepository;
        _calculation = calculation;
        _sickLeavePreview = sickLeavePreview;
        _accrualService = accrualService;
    }

    public async Task<EmployeeAbsencePreview> CalculateAsync(EmployeeAbsencePreviewRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var start = request.StartDate.Date;
        var end = request.EndDate.Date;

        var preview = new EmployeeAbsencePreview
        {
            // UpdateCalendarDays, lines 2911-2917.
            CalendarDays = start != default && end != default ? (end - start).TotalDays + 1 : 0,

            // UpdateAbsenceHours, lines 2935-2941.
            AbsenceHours = request.StartTime != default && request.EndTime != default
                ? Math.Round((request.EndTime - request.StartTime).TotalHours, 3)
                : 0
        };

        // RecalculateTotalLeaveDays, lines 2942-2969 -- the service the save itself uses.
        preview.TotalLeaveDays = await _calculation.CalculateTotalLeaveDaysAsync(
            request.EmployeeId, request.AbsenceTypeId, start, end, preview.AbsenceHours);

        var employee = _employeeRepository.GetAll().FirstOrDefault(e => e.Id == request.EmployeeId);
        var absenceType = _absenceTypeRepository.GetAll().FirstOrDefault(t => t.Id == request.AbsenceTypeId);

        // CurrentBalance, lines 408-437.
        preview.CurrentBalance = employee == null || absenceType == null
            ? 0
            : absenceType.HaveQuotaAccrual
                ? AccrualQuotaBalance(employee.Id, absenceType, start, end)
                : await GetOtherLeaveQuotaAsync(employee, absenceType, start, end);

        var returnDateEntered = request.ReturnDate.Year != 1;

        // Paid_CalendarDays (820-835) / DeductFromQuota (839-866).
        var paidStart = request.PaidStartDate?.Date ?? default;
        var paidEnd = request.PaidEndDate?.Date ?? default;
        if (paidStart != default && paidEnd != default)
        {
            preview.PaidCalendarDays = (paidEnd - paidStart).TotalDays + 1;

            if (employee != null && returnDateEntered)
            {
                preview.DeductFromQuota = await DaysLessExcludedAsync(employee.Id, paidStart, paidEnd, AnnualLeaveCode);
            }
        }

        // Unpaid_CalendarDays (926-941) / UnpaidDays (944-972).
        var unpaidStart = request.UnpaidStartDate?.Date ?? default;
        var unpaidEnd = request.UnpaidEndDate?.Date ?? default;
        if (unpaidStart != default && unpaidEnd != default)
        {
            preview.UnpaidCalendarDays = (unpaidEnd - unpaidStart).TotalDays + 1;

            if (employee != null && returnDateEntered)
            {
                preview.UnpaidDays = await DaysLessExcludedAsync(employee.Id, unpaidStart, unpaidEnd, UnpaidLeaveCode);
            }
        }

        // SickLeaveUnpaidInfo, lines 980-1100.
        if (employee != null && absenceType != null && start != default && end != default)
        {
            preview.SickLeaveSegments = await _sickLeavePreview.GetSegmentsAsync(employee.Id, absenceType.Id, start, end);
            preview.SickLeaveUnpaidInfo = FormatSickLeaveInfo(preview.SickLeaveSegments);
        }

        return preview;
    }

    /// <summary>
    /// Legacy <c>DeductFromQuota</c> / <c>UnpaidDays</c> getter body: calendar days less the days the
    /// <paramref name="typeCode"/> type excludes. The empty <c>catch</c> is legacy's (lines 859-860, 965-966).
    /// </summary>
    private async Task<double> DaysLessExcludedAsync(int employeeId, DateTime from, DateTime to, string typeCode)
    {
        try
        {
            var typeId = _absenceTypeRepository.GetAll().Where(t => t.Code == typeCode).Select(t => (int?)t.Id).FirstOrDefault();
            var totalDays = (to - from).TotalDays + 1;
            var excluded = (double)await _calculation.ActualLeaveDaysAsync(employeeId, from, to, typeId);
            return totalDays - excluded;
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Legacy <c>EmployeeAbsenceQuota.GetEmployeeAbsenceQuota(emp, type, start, end, session)</c> then
    /// <c>QuotaBalance</c> (EmployeeAbsenceQuota.cs line 123, rounded to 2).
    /// </summary>
    private double AccrualQuotaBalance(int employeeId, AbsenceType absenceType, DateTime start, DateTime end)
    {
        var quotaTypeId = absenceType.QuotaType != QuotaType.AnnualLeave
            ? (int?)absenceType.Id
            : _absenceTypeRepository.GetAll().Where(t => t.Code == AnnualLeaveCode).Select(t => (int?)t.Id).FirstOrDefault();

        if (quotaTypeId == null)
        {
            return 0;
        }

        var quota = _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId
                        && q.AbsenceTypeId == quotaTypeId.Value
                        && q.DeductionFrom <= start
                        && q.DeductionTo >= end)
            .OrderBy(q => q.Id)
            .FirstOrDefault();

        return quota == null
            ? 0
            : Math.Round(quota.QuotaAccrued - (quota.QuotaUsed + quota.QuotaAdjusted + quota.QuotaForfeited), 2);
    }

    /// <summary>Legacy <c>EmployeeAbsence.GetOtherLeaveQuota(Employee, DateTime, DateTime, AbsenceType, Session)</c>, lines 3158-3258.</summary>
    private async Task<double> GetOtherLeaveQuotaAsync(Employee employee, AbsenceType absenceType, DateTime startDate, DateTime endDate)
    {
        // Lines 3163-3164: the sub-group of an org assignment overlapping the leave, then its salary-profile row.
        var subGroupId = _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employee.Id && !(a.StartDate > endDate || a.EndDate < startDate))
            .OrderBy(a => a.Id)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefault();
        var subGroupLeave = _accrualService.GetEmployeeSubGroupLeave(subGroupId, employee.JoiningDate, absenceType.Id, startDate);

        // Line 3180.
        double balanceQuota = subGroupLeave != null ? subGroupLeave.TotalDays : absenceType.TotalDaysAllowed;

        if (balanceQuota == 0)
        {
            return balanceQuota;
        }

        // Lines 3184-3189.
        var (deductionStartDate, deductionEndDate) = await _accrualService.GetLeaveAccrualStartEndAsync(employee.Id, absenceType.Id, endDate);

        // Lines 3191-3193.
        balanceQuota = _accrualService.GetProRataBasedBalanceQuota(
            employee.JoiningDate, absenceType.AccrualPeriod, absenceType.LeaveEntitlement, balanceQuota, deductionStartDate, deductionEndDate);

        // Lines 3195-3200: every leave of the type touching the period -- nothing excluded (ignoreOid = -1).
        var leaves = _absenceRepository.GetAll().Where(a => a.EmployeeId == employee.Id && a.AbsenceTypeId == absenceType.Id);

        var usedLeavesCount = leaves
            .Where(a => !(a.StartDate > deductionEndDate || a.EndDate < deductionStartDate))
            .Sum(a => (double?)a.TotalLeaveDays) ?? 0;

        // Lines 3203-3220: the part of a leave straddling the period start that falls before it.
        var overlapStartDate = leaves
            .Where(a => a.StartDate < deductionStartDate && a.EndDate >= deductionStartDate)
            .OrderBy(a => a.Id)
            .Select(a => a.StartDate)
            .FirstOrDefault();

        if (overlapStartDate != default)
        {
            var overEndDate = deductionStartDate.AddDays(-1);
            var totalDaysOverlap1 = (overEndDate - overlapStartDate).TotalDays + 1;
            var leaveDaysOverlap1 = (double)await _calculation.ActualLeaveDaysAsync(employee.Id, overlapStartDate, overEndDate, absenceType.Id);
            usedLeavesCount -= totalDaysOverlap1 - leaveDaysOverlap1;
        }

        // Lines 3222-3238: the part of a leave straddling the period end that falls after it.
        var overlapEndDate = leaves
            .Where(a => a.StartDate <= deductionEndDate && a.EndDate > deductionEndDate)
            .OrderBy(a => a.Id)
            .Select(a => a.EndDate)
            .FirstOrDefault();

        if (overlapEndDate != default)
        {
            var overStartDate = deductionEndDate.AddDays(1);
            var totalDaysOverlap2 = (overlapEndDate - overStartDate).TotalDays + 1;
            var leaveDaysOverlap2 = (double)await _calculation.ActualLeaveDaysAsync(employee.Id, overStartDate, overlapEndDate, absenceType.Id);
            usedLeavesCount -= totalDaysOverlap2 - leaveDaysOverlap2;
        }

        // Lines 3241-3251: opening sick leave carried in as a time transfer.
        if (absenceType.Code == SickLeaveCode)
        {
            usedLeavesCount += _timeTransferRepository.GetAll()
                .Where(t => t.EmployeeId == employee.Id
                            && t.TransferType.Code == OpeningSickLeaveTransferCode
                            && t.TransferDate >= deductionStartDate
                            && t.TransferDate <= deductionEndDate)
                .Sum(t => (double?)t.TransferCount) ?? 0;
        }

        // Line 3254.
        return balanceQuota - usedLeavesCount;
    }

    /// <summary>
    /// Legacy <c>GetSickLeaveUnpaidInfo</c>'s text (lines 1053, 1072-1073, 1080, 1089-1090): one
    /// <c>AppendLine</c> per stretch, "From : dd/MM/yyyy - To : dd/MM/yyyy (Deduction : 25%)", the tail past the last
    /// slab reading 100%.
    /// </summary>
    private static string FormatSickLeaveInfo(System.Collections.Generic.IEnumerable<SickLeaveDeductionSegment> segments)
    {
        var sb = new StringBuilder();
        foreach (var segment in segments)
        {
            var deduction = segment.IsBeyondTheLastSlab
                ? "100%"
                : segment.DeductionNumber.ToString(CultureInfo.InvariantCulture)
                  + (segment.DeductionUnit == DeductionUnit.Percentage ? "%" : " Days");

            sb.AppendLine($"From : {segment.FromDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} - To : " +
                          $"{segment.ToDate.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)} (Deduction : {deduction})");
        }

        return sb.ToString();
    }
}
