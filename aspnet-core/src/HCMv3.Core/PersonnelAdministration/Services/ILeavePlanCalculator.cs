using System;
using System.Threading.Tasks;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The figures and the rules of a <see cref="LeavePlan"/>, in one place so the self-service form, the workflow
/// handler and HR's screens cannot work them out differently.
///
/// <para>
/// Nothing is calculated here that already has a home. The projected balance is
/// <c>IAccrualService.GetAsOnDateQuotaWithProjectionAsync</c> (the port of legacy
/// <c>AccrualService.GetAsOnDateQuotaWithProjection</c>, which is exactly what legacy's plan called), and the
/// leave-day count shown beside it is <c>IEmployeeAbsenceCalculationService.CalculateTotalLeaveDaysAsync</c>.
/// This service only decides which leave type and which salary profile to hand them, the way legacy did.
/// </para>
/// </summary>
public interface ILeavePlanCalculator
{
    /// <summary>
    /// Legacy <c>LeavePlanner.PlannedDays</c>: calendar days, start and end included. Zero while either date is
    /// missing or the end is before the start.
    /// </summary>
    double GetPlannedDays(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Legacy <c>LeavePlanner.ProjectedLeaveBalance</c>: the annual leave (code "1") balance projected to
    /// <paramref name="asOnDate"/>, with the salary profile of the org assignment covering that date. Null when
    /// the date is missing or the tenant has no annual leave type.
    /// </summary>
    Task<double?> GetProjectedLeaveBalanceAsync(int employeeId, DateTime asOnDate);

    /// <summary>
    /// How many days of annual leave the planned dates would actually charge -- the calendar days less the
    /// weekly-offs and holidays that type excludes. Shown beside the planned days so the employee can compare
    /// the plan with the balance; never stored, because a plan is not a leave. Null when it cannot be worked out.
    /// </summary>
    Task<double?> GetAnnualLeaveDaysAsync(int employeeId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// Legacy <c>LeavePlanner.PerformValidations</c>, in its order: start date, end date, end not before start.
    /// The employee check is not repeated -- every caller already holds one.
    /// </summary>
    void Validate(DateTime startDate, DateTime endDate);

    /// <summary>
    /// Writes <see cref="LeavePlan.PlannedDays"/> and <see cref="LeavePlan.ProjectedLeaveBalance"/> from the
    /// plan's own dates. Legacy did the planned-days half at submit (<c>lpobj._plannedDays = lpobj.PlannedDays</c>
    /// in <c>WorkFlowServices.SaveWorkflowRequest</c>).
    /// </summary>
    Task RefreshFiguresAsync(LeavePlan plan);
}
