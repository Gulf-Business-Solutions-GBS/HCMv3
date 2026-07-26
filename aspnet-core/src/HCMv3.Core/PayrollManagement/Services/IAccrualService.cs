using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Leave/ticket/health-reimbursement accrual and forfeiture calculation engine (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.AccrualService</c>, a static-methods-only class). See
/// <see cref="AccrualService"/> for the full list of adaptations made necessary by this port's shallower
/// Employee-satellite entity shape (no bidirectional <c>Employee.OrgAssignment</c>/<c>Employee.Absence</c>/
/// etc. collections, and <c>EmployeeOrgAssignment</c> carrying no <c>EmployeeGroupId</c>/
/// <c>EmployeeSubGroupId</c> FK yet -- same gap already documented for
/// <c>TravelManagement.CalculateExpectedPerDiemAsync</c>). Every method that legacy derived the employee's
/// current EmployeeGroup/EmployeeSubGroup from <c>OrgAssignment</c> instead takes it as an explicit,
/// caller-supplied nullable id parameter here.
/// </summary>
public interface IAccrualService
{
    /// <summary>Ported from legacy <c>AccrualService.GetAsOnDateQuota</c>.</summary>
    Task<double> GetAsOnDateQuotaAsync(int employeeId, int? employeeSubGroupId, int absenceTypeId, DateTime asOnDate, bool includeFutureTrans);

    /// <summary>Ported from legacy <c>AccrualService.GetAsOnDateProvision</c>.</summary>
    Task<double> GetAsOnDateProvisionAsync(int employeeId, int absenceTypeId, DateTime asOnDate, double asOnDateQuota);

    /// <summary>
    /// Ported from legacy <c>AccrualService.GetActualWeekOffs(session, emp, empTimeRegister, payDate, salaryRegisterDetail)</c>
    /// (the "weekly-off wage-type amount, day-by-day duty fallback" overload).
    /// </summary>
    Task<double> GetActualWeekOffsAsync(int employeeId, DateTime payDate, int salaryRegisterDetailId);

    /// <summary>
    /// Ported from legacy <c>AccrualService.GetActualWeekOffs(session, emp, payDate)</c> (the simpler
    /// "TimeRegister.WeeklyOff column, day-by-day duty fallback" overload).
    /// </summary>
    Task<double> GetActualWeekOffsAsync(int employeeId, DateTime payDate);

    /// <summary>Ported from legacy <c>AccrualService.GetMaxIndemnityLimit</c>.</summary>
    Task<double> GetMaxIndemnityLimitAsync(int employeeId, DateTime payDate, int? employeeGroupId, int? indemnityPayRuleId, bool enableUnpaidDedSlabWise, List<IndemnitySlab> indemnitySlabs);

    /// <summary>
    /// Ported from legacy <c>AccrualService.GetLeaveAccrualStartEnd</c> (the <c>ref</c> out-params become a
    /// tuple return).
    /// </summary>
    Task<(DateTime DeductionStartDate, DateTime DeductionEndDate)> GetLeaveAccrualStartEndAsync(int employeeId, int absenceTypeId, DateTime endDate);

    /// <summary>
    /// Ported from legacy <c>AccrualService.GetAirTicketAccrualStartEnd</c> (the <c>ref</c> out-params become
    /// a tuple return).
    /// </summary>
    Task<(DateTime DeductionStartDate, DateTime DeductionEndDate)> GetAirTicketAccrualStartEndAsync(int employeeId, int ticketTypeId, AccrualPeriod accrualPeriod, DateTime endDate);

    /// <summary>
    /// Ported from legacy <c>AccrualService.GetHealthReimbAccrualStartEnd</c> (the <c>ref</c> out-params
    /// become a tuple return). Pure computation, no DB access -- kept synchronous.
    /// <paramref name="healthInsurancePeriod"/> is <see cref="EmployeeSubGroup.HealthInsurancePeriod"/>
    /// (reuses <see cref="CustomStartMonth"/>, see that entity's remarks).
    /// </summary>
    (DateTime DeductionStartDate, DateTime DeductionEndDate) GetHealthReimbAccrualStartEnd(DateTime joiningDate, CustomStartMonth healthInsurancePeriod, DateTime endDate);

    /// <summary>
    /// Ported from legacy <c>AccrualService.GetProRataBasedBalanceQuota</c>. Pure computation, no DB access --
    /// kept synchronous.
    /// </summary>
    double GetProRataBasedBalanceQuota(DateTime employeeJoiningDate, AccrualPeriod accrualPeriod, LeaveEntitlement leaveEntitlement, double balanceQuota, DateTime deductionStartDate, DateTime deductionEndDate);

    /// <summary>Ported from legacy <c>AccrualService.GetAsOnDateQuotaWithProjection</c>.</summary>
    Task<double> GetAsOnDateQuotaWithProjectionAsync(int employeeId, int? employeeSubGroupId, int absenceTypeId, DateTime asOnDate);

    /// <summary>
    /// Ported from legacy <c>AccrualService.CalculateMonthlyForfeit</c>. Unlike the legacy <c>void</c>
    /// method (which mutated XPO objects in-place under an implicit session), this persists the
    /// create/update/delete of the <c>EmployeeAbsQuotaForfeit</c> row and the <c>EmployeeAbsenceQuota.QuotaForfeited</c>
    /// update directly via injected repositories, returning a <see cref="ForfeitCalculationResult"/> summary.
    /// </summary>
    Task<ForfeitCalculationResult> CalculateMonthlyForfeitAsync(int employeeId, int? employeeSubGroupId, DateTime monthStartDate, DateTime monthEndDate);

    /// <summary>
    /// Ported from legacy <c>AccrualService.CalculateForfeitForLeave</c>. The legacy <c>empAbsence</c>
    /// parameter (a possibly not-yet-committed, in-memory <c>EmployeeAbsence</c> XPO object representing the
    /// leave request currently being saved) is dropped -- callers must persist the <c>EmployeeAbsence</c> row
    /// first, then invoke this method; <paramref name="prevEmployeeAbsenceId"/> replaces the legacy
    /// <c>prevempAbsenceOid</c> exclusion parameter (used when recalculating after an edit, to exclude the
    /// row's OLD state from the overlap query).
    /// </summary>
    Task<ForfeitCalculationResult> CalculateForfeitForLeaveAsync(int employeeId, int? employeeSubGroupId, DateTime startDate, DateTime endDate, int? prevEmployeeAbsenceId);

    /// <summary>
    /// Ported from legacy <c>AccrualService.CalculateForfeitForPublicHoliday</c>. <paramref name="payrollAreaId"/>
    /// replaces the legacy <c>emp.CurrentPayrollArea</c> derived property (not ported -- see
    /// <see cref="AccrualService"/> remarks).
    /// </summary>
    Task<ForfeitCalculationResult> CalculateForfeitForPublicHolidayAsync(int employeeId, int? employeeSubGroupId, int payrollAreaId, DateTime phDate, bool deleted);

    /// <summary>
    /// Ported from legacy <c>AccrualService.GetTotalIndemnityDays</c>. <paramref name="employeeSubGroupId"/>
    /// is an explicit caller-supplied parameter (see interface remarks) -- legacy derived it internally from
    /// <c>emp.OrgAssignment</c>.
    /// </summary>
    Task<double> GetTotalIndemnityDaysAsync(int employeeId, DateTime payDate, int? employeeGroupId, int? employeeSubGroupId, int? indemnityPayRuleId);

    /// <summary>
    /// Ported from legacy <c>AccrualService.GetTotalIndemnityTable</c>. <paramref name="employeeSubGroupId"/>
    /// is an explicit caller-supplied parameter (see interface remarks) -- legacy derived it internally from
    /// <c>emp.OrgAssignment</c>.
    /// </summary>
    Task<List<IndemnitySlab>> GetTotalIndemnityTableAsync(int employeeId, DateTime payDate, int? employeeSubGroupId);

    /// <summary>Ported from legacy <c>AccrualService.GetAvailableNegativeBalance</c> (bonus method, not in the priority list but small/self-contained).</summary>
    Task<double> GetAvailableNegativeBalanceAsync(int employeeId, bool enableNegativeBalance, double negativeBalanceLimit, double yearlyQuota, DateTime leaveStartDate);
}
