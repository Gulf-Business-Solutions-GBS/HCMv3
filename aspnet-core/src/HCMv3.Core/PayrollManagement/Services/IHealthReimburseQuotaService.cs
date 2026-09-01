using System;
using System.Threading.Tasks;

namespace HCMv3.PayrollManagement.Services;

/// <summary>
/// Answers "how much of their health-reimbursement entitlement can this employee still claim" -- the
/// CLAIMABLE amount, which is not the same as the remaining amount.
///
/// <para>
/// Modelled deliberately on <c>ILeaveBalanceService</c>, and for the same reason. Nothing here is stored:
/// there is no "reimbursement used" column to increment on approval, so payroll, the health-reimbursement
/// wage type and every report keep seeing precisely what they saw before workflow existed -- the sum of the
/// claims the <c>ApprovedOnly</c> filter lets them see. A claim awaiting approval consumes nothing, because
/// it may still be rejected. But it must reduce what the employee can go on to claim, or they could file the
/// same 500 three times in an afternoon and each would pass its own balance check independently. That is the
/// <see cref="HealthReimburseBalance.Remaining"/> versus <see cref="HealthReimburseBalance.Claimable"/>
/// distinction, and it is computable only because the pending claim sits in <c>EmployeeHealthReimburses</c>
/// alongside the approved ones. In the legacy two-table design the pending rows lived in
/// <c>HealthReimburseRequest</c>, where the balance arithmetic never looked.
/// </para>
///
/// <para>
/// <b>Two legacy computations of the same quantity.</b> Legacy computed the base entitlement in two
/// different ways depending on which screen you used, and this service ports both rather than picking a
/// winner:
/// <list type="bullet">
/// <item><description><see cref="GetBalanceAsync"/> uses the PLAIN SUM of
/// <c>EmployeeSubGroupHealthReimburse.Amount</c> for the employee's sub-group over the payment date --
/// exactly what <c>HealthReimburseRequest.PerformValidations</c> did, and this is the request path, so it is
/// the one that governs a routed claim.</description></item>
/// <item><description><see cref="GetEligibleHealthReimburseAmountAsync"/> is the richer per-dependant
/// calculation <c>EmployeeHealthReimburse.OnSaving</c> used, and it is what carry-forward is built from,
/// exactly as in legacy.</description></item>
/// </list>
/// The two can disagree -- the plain sum counts every tier row whether or not the employee actually has a
/// dependant of that type. That divergence is legacy's, not this port's, and it is documented rather than
/// quietly reconciled: silently switching the request path onto the stricter figure would start refusing
/// claims that the legacy system accepted.
/// </para>
/// </summary>
public interface IHealthReimburseQuotaService
{
    /// <summary>
    /// The employee's health-reimbursement position for the entitlement window containing
    /// <paramref name="asOf"/> (defaults to today).
    /// </summary>
    /// <param name="excludeClaimId">
    /// A claim to leave out of Used and Pending -- the row currently being submitted or edited, so that
    /// re-validating an existing claim does not have it compete with itself.
    /// </param>
    Task<HealthReimburseBalance> GetBalanceAsync(int employeeId, DateTime? asOf = null, int? excludeClaimId = null);

    /// <summary>
    /// Refuses a claim that is malformed, a same-day duplicate, or larger than what the employee can still
    /// claim. Throws <see cref="Abp.UI.UserFriendlyException"/>; returns quietly when the claim is fine.
    ///
    /// <para>
    /// One method rather than a rule per caller: the workflow handler, the self-service screen and the HR
    /// screen must all reach the same verdict, and a health claim entered while the transaction type is
    /// dormant has to be checked too -- legacy enforced this in <c>OnSaving</c>, so every write path went
    /// through it.
    /// </para>
    /// </summary>
    Task EnsureClaimIsAllowedAsync(int employeeId, DateTime paymentDate, double amount, int? excludeClaimId = null);

    /// <summary>
    /// Port of legacy <c>EmployeeHealthReimburse.GetEligibleHealthReimburseAmount</c>: the employee's own
    /// tier plus each eligible dependant's, honouring benefit exceptions, per-dependant exceptions, the
    /// dependant age limit and <c>EmployeeSubGroup.MaxDependantHealthReimburseCount</c>.
    /// </summary>
    Task<double> GetEligibleHealthReimburseAmountAsync(int employeeSubGroupId, int employeeId, DateTime paymentDate);

    /// <summary>
    /// Port of legacy <c>EmployeeHealthReimburse.GetCarryForwardAmount</c>: what was left unspent at the end
    /// of each prior entitlement year, rolled forward year by year up to <paramref name="closingDate"/>.
    /// </summary>
    Task<double> GetCarryForwardAmountAsync(int employeeId, DateTime joiningDate, DateTime closingDate);
}
