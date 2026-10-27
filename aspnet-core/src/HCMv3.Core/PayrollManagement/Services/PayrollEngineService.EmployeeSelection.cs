using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Employee-selection half of <see cref="PayrollEngineService"/> -- the port of legacy
/// <c>HCM10.PayrollEngine/Program.GetEmployeeList</c> (legacy <c>Program.cs</c> lines 5346-5373) and of the
/// <c>enableTimeEngineAutorun == true</c> selection branches inside <c>CreateSalaryRegister</c>
/// (legacy lines 418-536).
/// </summary>
public partial class PayrollEngineService
{
    /// <summary>
    /// Legacy <c>CreateSalaryRegister</c>'s "Get Employee List" region (legacy lines 418-536). Two top-level
    /// branches, selected by <c>enableTimeEngineAutorun</c>, exactly as legacy.
    /// </summary>
    private async Task<List<Employee>> GetEmployeeListAsync(
        PayrollEngineRunInput input,
        DateTime payInDate,
        PayrollEngineSettings settings)
    {
        if (!settings.EnableTimeEngineAutorun)
        {
            return await GetEmployeeListLegacyAsync(input.PayrollAreaName, input.EmployeeCodes, payInDate);
        }

        if (input.PayrollRunType == PayrollRunType.Regular)
        {
            return await GetRegularAutorunEmployeeListAsync(input, payInDate);
        }

        return await GetOffCycleAutorunEmployeeListAsync(input, payInDate, settings);
    }

    /// <summary>
    /// Legacy <c>Program.GetEmployeeList</c> (lines 5346-5373), preserved warts and all:
    /// <list type="bullet">
    /// <item><description>When the payroll area is BLANK legacy returns the seed list
    /// <c>Employee.Where(x =&gt; x.Code == "")</c> -- i.e. effectively EMPTY -- regardless of the employee
    /// codes. Preserved: a run with no payroll area processes nobody.</description></item>
    /// <item><description>When an employee-code list IS supplied, legacy drops the payroll-area, the
    /// date-window and the <c>Status == Active</c> filters entirely (line 5364) so that a named, exited
    /// employee can still be run. Preserved.</description></item>
    /// <item><description>The no-code branch orders by <c>Employee.Code</c> (line 5359), while the autorun
    /// branches order by <c>Employee.Oid</c> (lines 436/532). That inconsistency is legacy's.</description></item>
    /// </list>
    /// </summary>
    private Task<List<Employee>> GetEmployeeListLegacyAsync(string payrollAreaName, string employeeCodes, DateTime dayDate)
    {
        if (string.IsNullOrWhiteSpace(payrollAreaName))
        {
            return Task.FromResult(new List<Employee>());
        }

        if (!string.IsNullOrWhiteSpace(employeeCodes))
        {
            var codes = SplitEmployeeCodes(employeeCodes);
            return Task.FromResult(_employeeRepository.GetAll()
                .Where(r => codes.Contains(r.Code))
                .Distinct()
                .ToList());
        }

        return GetActiveEmployeesInPayrollAreaAsync(payrollAreaName, dayDate, orderByCode: true);
    }

    /// <summary>Legacy lines 428-450 (autorun + Regular).</summary>
    private async Task<List<Employee>> GetRegularAutorunEmployeeListAsync(PayrollEngineRunInput input, DateTime payInDate)
    {
        var employees = await GetActiveEmployeesInPayrollAreaAsync(input.PayrollAreaName, payInDate, orderByCode: false);

        if (!string.IsNullOrWhiteSpace(input.EmployeeCodes))
        {
            var codes = SplitEmployeeCodes(input.EmployeeCodes);
            employees = employees.Where(r => codes.Contains(r.Code)).ToList();
        }

        return employees;
    }

    /// <summary>
    /// Legacy lines 454-533 (autorun + OffCycle): the candidate set is driven by the OFF-CYCLE REASON, not by
    /// the payroll area alone.
    ///
    /// <para>Legacy narrows <c>payEndDate</c> to today when
    /// <c>ManagePayrollSettings.RunOffCycleForFutureDate</c> is false (lines 460-461) -- preserved.</para>
    ///
    /// <para>An unrecognised off-cycle reason leaves legacy's <c>dtEmployee</c> NULL and legacy then
    /// NullReferences at line 529/532. This port returns an empty list instead; the run simply processes
    /// nobody. That is the only behavioural change on this path and it is strictly safer.</para>
    /// </summary>
    private async Task<List<Employee>> GetOffCycleAutorunEmployeeListAsync(
        PayrollEngineRunInput input,
        DateTime payInDate,
        PayrollEngineSettings settings)
    {
        var payStartDate = new DateTime(payInDate.Year, payInDate.Month, 1, 0, 0, 0);
        var payEndDate = payInDate;

        if (!settings.RunOffCycleForFutureDate && payEndDate > Abp.Timing.Clock.Now.Date)
        {
            payEndDate = Abp.Timing.Clock.Now.Date;
        }

        var employeeIdsInArea = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(input.PayrollAreaName, payInDate);
        if (!employeeIdsInArea.Any())
        {
            return new List<Employee>();
        }

        var candidateIds = new List<int>();

        if (input.OffCycleReason == PayrollOffCycleReasons.AdvanceLeave)
        {
            // Legacy lines 465-489: advance-leave absences UNION advance-paid annual-leave encashments.
            candidateIds.AddRange(_employeeAbsenceRepository.GetAll()
                .Where(r => r.AbsenceType.IsPaid
                            && r.LeavePaymentType == LeavePaymentType.PayInAdvance
                            && r.PaymentDate >= payStartDate
                            && r.PaymentDate <= payEndDate
                            && r.Employee.Status == EmployeeStatus.Active
                            && employeeIdsInArea.Contains(r.EmployeeId))
                .Select(r => r.EmployeeId)
                .ToList());

            candidateIds.AddRange(GetAdvancePaidEncashmentEmployeeIds(payStartDate, payEndDate, employeeIdsInArea, requireActive: true));
        }
        else if (input.OffCycleReason == PayrollOffCycleReasons.EndOfService)
        {
            // Legacy lines 494-504. NOTE legacy does NOT apply Status == Active here (an exiting employee is
            // typically already inactive). Preserved.
            candidateIds.AddRange(_employeeFnFPaymentRepository.GetAll()
                .Where(r => r.CompensationDate >= payStartDate
                            && r.CompensationDate <= payEndDate
                            && employeeIdsInArea.Contains(r.EmployeeId))
                .Select(r => r.EmployeeId)
                .ToList());
        }
        else if (input.OffCycleReason == PayrollOffCycleReasons.LeaveEncashment)
        {
            // Legacy lines 509-523.
            candidateIds.AddRange(GetAdvancePaidEncashmentEmployeeIds(payStartDate, payEndDate, employeeIdsInArea, requireActive: true));
        }

        var distinctIds = candidateIds.Distinct().ToList();
        if (!distinctIds.Any())
        {
            return new List<Employee>();
        }

        var employees = _employeeRepository.GetAll().Where(r => distinctIds.Contains(r.Id)).ToList();

        // Legacy lines 526-532.
        if (!string.IsNullOrWhiteSpace(input.EmployeeCodes))
        {
            var codes = SplitEmployeeCodes(input.EmployeeCodes);
            employees = employees.Where(r => codes.Contains(r.Code)).ToList();
        }

        return employees.OrderBy(r => r.Id).ToList();
    }

    /// <summary>
    /// The <c>EmployeeAbsAdjustment</c> predicate legacy repeats verbatim at lines 477-486 and 511-520:
    /// annual leave (by <c>AbsenceType.Code == "1"</c> OR <c>QuotaType.AnnualLeave</c> -- legacy's OR, both
    /// arms kept), Encashment/Fractional adjustment, paid in advance, effective inside the period.
    /// </summary>
    private List<int> GetAdvancePaidEncashmentEmployeeIds(
        DateTime payStartDate,
        DateTime payEndDate,
        List<int> employeeIdsInArea,
        bool requireActive)
    {
        return _employeeAbsAdjustmentRepository.GetAll()
            .Where(r => (r.AbsenceType.Code == "1" || r.AbsenceType.QuotaType == QuotaType.AnnualLeave)
                        && r.EffectiveDate >= payStartDate
                        && r.EffectiveDate <= payEndDate
                        && (r.AdjustmentType == AdjustmentType.Encashment || r.AdjustmentType == AdjustmentType.Fractional)
                        && r.AdjustmentPaymentType == AdjustmentPaymentType.PayInAdvance
                        && (!requireActive || r.Employee.Status == EmployeeStatus.Active)
                        && employeeIdsInArea.Contains(r.EmployeeId))
            .Select(r => r.EmployeeId)
            .ToList();
    }

    /// <summary>
    /// Legacy's <c>EmployeeOrgAssignment.Where(... &amp;&amp; r.PayrollArea.Name == payrollArea)</c>
    /// selection, routed through <see cref="IEmployeePayrollAreaResolver"/> because this port's
    /// <see cref="EmployeeOrgAssignment"/> carries no <c>PayrollAreaId</c> FK -- see that interface for the
    /// substitute lookup and its two documented deltas.
    /// </summary>
    private async Task<List<Employee>> GetActiveEmployeesInPayrollAreaAsync(string payrollAreaName, DateTime asOnDate, bool orderByCode)
    {
        var employeeIds = await _payrollAreaResolver.GetEmployeeIdsInPayrollAreaAsync(payrollAreaName, asOnDate);
        if (!employeeIds.Any())
        {
            return new List<Employee>();
        }

        var query = _employeeRepository.GetAll()
            .Where(r => employeeIds.Contains(r.Id) && r.Status == EmployeeStatus.Active);

        return orderByCode
            ? query.OrderBy(r => r.Code).ToList()
            : query.OrderBy(r => r.Id).ToList();
    }

    /// <summary>Legacy <c>Program.GetEmpCodeList</c> (lines 5374-5383): split on ',' and Trim each entry.</summary>
    private static List<string> SplitEmployeeCodes(string employeeCodes)
    {
        return employeeCodes.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
    }
}
