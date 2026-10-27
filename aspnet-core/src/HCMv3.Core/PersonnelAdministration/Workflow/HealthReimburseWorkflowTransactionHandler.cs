using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement.Services;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for health reimbursement claims (<see cref="EmployeeHealthReimburse"/>).
///
/// <para>
/// This is a MONEY claim, and the rule that matters is the balance check. Getting it wrong is expensive in
/// both directions: too strict and a legitimate medical claim is refused at the worst possible moment for
/// the employee; too lax and somebody over-claims their annual entitlement and payroll pays it. So the
/// arithmetic is not written here -- it lives in <see cref="IHealthReimburseQuotaService"/>, which the
/// self-service screen and the HR screen call too, so all three reach the same verdict rather than three
/// nearly-identical verdicts.
/// </para>
///
/// <para>
/// <b>A pending claim reduces what can still be claimed, without touching any stored total.</b> There is no
/// "reimbursement used" column and nothing is written on approval -- the same Bookable-versus-Remaining model
/// <see cref="Services.ILeaveBalanceService"/> uses for leave. Approval simply stamps the row Approved, which
/// is what makes the <c>ApprovedOnly</c> filter show it to payroll and, in the same movement, moves its
/// amount from Pending into Used in every balance calculation. Nothing to reserve, nothing to leak if a claim
/// is abandoned, nothing to reconcile.
/// </para>
///
/// <para>
/// Note there is no <c>HealthReimburseRequest</c> table and nothing is created on approval. Legacy filed a
/// request row and copied its fields onto a brand new <c>EmployeeHealthReimburse</c> when the last approver
/// said yes; that shadow-table pair is precisely what this design removes, and it is also what made legacy's
/// balance blind to pending claims.
/// </para>
/// </summary>
public class HealthReimburseWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeHealthReimburse>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for health reimbursement.</summary>
    public const string HealthReimburseRequestType = "EmployeeHealthReimburse";

    private readonly IHealthReimburseQuotaService _quotaService;

    public HealthReimburseWorkflowTransactionHandler(
        IRepository<EmployeeHealthReimburse> repository,
        IUnitOfWorkManager unitOfWorkManager,
        IHealthReimburseQuotaService quotaService)
        : base(repository, unitOfWorkManager)
    {
        _quotaService = quotaService;
    }

    public override string RequestType => HealthReimburseRequestType;

    /// <summary>
    /// Refuses a claim that is unusable, already filed, or larger than the employee can still claim.
    ///
    /// <para>
    /// The claim being submitted is excluded from the comparison by id. It is still a Draft at this point so
    /// it is not in Pending yet, but excluding it explicitly means the check gives the same answer if it is
    /// ever re-run against a row that has already been stamped Open -- a claim must never compete with itself.
    /// </para>
    /// <para>
    /// The duplicate check is deliberately blind to nothing: it looks past the <c>ApprovedOnly</c> filter so a
    /// claim already sitting in an approver's inbox blocks a second one for the same day. Without that, an
    /// employee who did not see their first submission land could file it twice and both would pass, because
    /// each is invisible to the other.
    /// </para>
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var claim = await GetTargetAsync(context);
        if (claim == null)
        {
            return;
        }

        await _quotaService.EnsureClaimIsAllowedAsync(
            claim.EmployeeId,
            claim.PaymentDate,
            claim.Amount,
            excludeClaimId: claim.Id);
    }

    /// <summary>
    /// Applies the approval, and nothing else.
    ///
    /// <para>
    /// Stated as an explicit override rather than left to the base purely to record that the absence of a
    /// side effect is the design, not an omission. There is no total to increment: the moment the base stamps
    /// the row Approved it becomes visible to the <c>ApprovedOnly</c> filter, and every balance calculation
    /// starts counting it as Used on its next read. A handler that also wrote a stored total here would give
    /// the system two answers to the same question, and they would drift.
    /// </para>
    /// </summary>
    public override Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        return base.OnApprovedAsync(context);
    }

    /// <summary>
    /// The one-line inbox label, e.g. "Health Reimbursement, 250.5 on 01 Sep 2026".
    ///
    /// <para>
    /// The AMOUNT is in it, and that is the point. An approver deciding a money claim from a list needs the
    /// figure in front of them; a summary reading only "Health Reimbursement" asks them to open every row to
    /// find out what they are agreeing to spend, which in practice means they stop looking.
    /// </para>
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var claim = await GetTargetAsync(context);
        if (claim == null)
        {
            return null;
        }

        var amount = claim.Amount.ToString("0.###");
        var date = claim.PaymentDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"Health Reimbursement, {amount} on {date}",
            $"تعويض طبي، {amount} بتاريخ {date}");
    }
}
