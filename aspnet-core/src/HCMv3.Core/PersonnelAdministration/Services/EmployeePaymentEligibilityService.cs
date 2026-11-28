using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Timing;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IEmployeePaymentEligibilityService"/>
/// <remarks>
/// Legacy steps, kept in order:
/// <list type="number">
/// <item><description>All active employees (also the fallback if anything below fails).</description></item>
/// <item><description>For inactive employees, the latest non-revoked separation by submission date.</description></item>
/// <item><description>Drop anyone who rejoined after that separation's submission date.</description></item>
/// <item><description>The latest final settlement (FnF) with a compensation date on or after the submission
/// date. None: allowed.</description></item>
/// <item><description>Otherwise allowed only when the compensation date falls inside the payroll period being
/// processed for the employee's payroll area: the control record's own month while it is not closed, the
/// following month once it is closed. No payroll area or no control record: not allowed.</description></item>
/// </list>
/// <para><b>Adaptations.</b> Legacy took "the" control record per payroll area (one rolling row); this port
/// keeps one row per month, so the latest one (by calendar month start) is used. Legacy read
/// <c>Employee.CurrentPayrollArea</c>; this uses <see cref="IEmployeePayrollAreaResolver"/> as of the
/// compensation date, then as of today, because a separated employee's assignment may already have ended.</para>
/// </remarks>
public class EmployeePaymentEligibilityService : HCMv3DomainServiceBase, IEmployeePaymentEligibilityService
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeSeparation> _separationRepository;
    private readonly IRepository<EmployeeRejoin> _rejoinRepository;
    private readonly IRepository<EmployeeFnFPayment> _fnfPaymentRepository;
    private readonly IRepository<PayrollControlRecord> _payrollControlRecordRepository;
    private readonly IEmployeePayrollAreaResolver _payrollAreaResolver;

    public EmployeePaymentEligibilityService(
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeSeparation> separationRepository,
        IRepository<EmployeeRejoin> rejoinRepository,
        IRepository<EmployeeFnFPayment> fnfPaymentRepository,
        IRepository<PayrollControlRecord> payrollControlRecordRepository,
        IEmployeePayrollAreaResolver payrollAreaResolver)
    {
        _employeeRepository = employeeRepository;
        _separationRepository = separationRepository;
        _rejoinRepository = rejoinRepository;
        _fnfPaymentRepository = fnfPaymentRepository;
        _payrollControlRecordRepository = payrollControlRecordRepository;
        _payrollAreaResolver = payrollAreaResolver;
    }

    public async Task<List<int>> GetAllowedEmployeeIdsAsync()
    {
        // Step 1.
        var activeEmployeeIds = _employeeRepository.GetAll()
            .Where(e => e.Status == EmployeeStatus.Active)
            .Select(e => e.Id)
            .ToList();

        try
        {
            var inactiveEmployeeIds = _employeeRepository.GetAll()
                .Where(e => e.Status == EmployeeStatus.Inactive)
                .Select(e => e.Id)
                .ToList();

            // Step 2.
            var latestSeparationByEmployee = _separationRepository.GetAll()
                .Where(s => s.RevokeDate == null && inactiveEmployeeIds.Contains(s.EmployeeId))
                .Select(s => new { s.EmployeeId, s.SubmissionDate })
                .ToList()
                .GroupBy(s => s.EmployeeId)
                .ToDictionary(g => g.Key, g => g.Max(s => s.SubmissionDate));

            if (latestSeparationByEmployee.Count == 0)
            {
                return activeEmployeeIds.Distinct().ToList();
            }

            var separatedIds = latestSeparationByEmployee.Keys.ToList();

            // Step 3.
            var rejoinsByEmployee = _rejoinRepository.GetAll()
                .Where(r => separatedIds.Contains(r.EmployeeId))
                .Select(r => new { r.EmployeeId, r.RejoinDate })
                .ToList()
                .GroupBy(r => r.EmployeeId)
                .ToDictionary(g => g.Key, g => g.Select(r => r.RejoinDate).ToList());

            var finalSeparations = latestSeparationByEmployee
                .Where(kvp => !rejoinsByEmployee.TryGetValue(kvp.Key, out var rejoinDates)
                              || !rejoinDates.Any(d => d > kvp.Value))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (finalSeparations.Count == 0)
            {
                return activeEmployeeIds.Distinct().ToList();
            }

            var finalIds = finalSeparations.Keys.ToList();

            // Step 4.
            var fnfByEmployee = _fnfPaymentRepository.GetAll()
                .Where(f => finalIds.Contains(f.EmployeeId))
                .Select(f => new { f.EmployeeId, f.CompensationDate })
                .ToList()
                .GroupBy(f => f.EmployeeId)
                .ToDictionary(g => g.Key, g => g.Select(f => f.CompensationDate).ToList());

            var allowed = new HashSet<int>(activeEmployeeIds);

            // Step 5.
            foreach (var (employeeId, submissionDate) in finalSeparations)
            {
                DateTime? latestCompensationDate = fnfByEmployee.TryGetValue(employeeId, out var compensationDates)
                    ? compensationDates.Where(d => d >= submissionDate).OrderByDescending(d => d).Cast<DateTime?>().FirstOrDefault()
                    : null;

                if (latestCompensationDate == null)
                {
                    allowed.Add(employeeId);
                    continue;
                }

                if (await IsInCurrentPayrollPeriodAsync(employeeId, latestCompensationDate.Value))
                {
                    allowed.Add(employeeId);
                }
            }

            return allowed.ToList();
        }
        catch (Exception ex)
        {
            // Legacy: "Return active employees only as safe fallback".
            Logger.Warn("Payment employee eligibility fell back to active employees only: " + ex.Message, ex);
            return activeEmployeeIds.Distinct().ToList();
        }
    }

    private async Task<bool> IsInCurrentPayrollPeriodAsync(int employeeId, DateTime compensationDate)
    {
        var payrollArea = await _payrollAreaResolver.GetEmployeePayrollAreaAsync(employeeId, compensationDate)
                          ?? await _payrollAreaResolver.GetEmployeePayrollAreaAsync(employeeId, Clock.Now.Date);
        if (payrollArea == null)
        {
            return false;
        }

        var control = _payrollControlRecordRepository.GetAll()
            .Where(r => r.PayrollAreaId == payrollArea.Id)
            .OrderByDescending(r => r.CalendarMonthStart)
            .FirstOrDefault();
        if (control == null)
        {
            return false;
        }

        if (!control.PeriodClosed)
        {
            return compensationDate >= control.CalendarMonthStart && compensationDate <= control.CalendarMonthEnd;
        }

        return compensationDate >= control.CalendarMonthStart.AddMonths(1)
               && compensationDate <= control.CalendarMonthEnd.AddMonths(1);
    }
}
