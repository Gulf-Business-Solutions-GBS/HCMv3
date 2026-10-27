using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Employee loan recovery-exemption -- skipping one month's instalment on an existing
/// <see cref="EmployeeLoan"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PersonnelAdministration.EmployeeLoanExemption</c>).
///
/// <para>
/// This row IS the transaction AND the request. Legacy kept a separate
/// <c>Workflow.Payroll.LoanExemptionRequest</c> table that, on final approval, constructed an
/// <c>EmployeeLoanExemption</c> and saved it -- the usual shadow-table pair, with the usual failure modes
/// (an approved request with no exemption, an exemption for a request later rejected, two exemptions for
/// one approval). There is no <c>LoanExemptionRequest</c> table here: the exemption is created hidden by
/// the <c>ApprovedOnly</c> filter and becomes effective when it is approved.
/// </para>
///
/// <para>
/// It is a CHILD row of the loan, not an amendment to it. The same loan can legitimately carry many
/// exemptions -- which is exactly why the duplicate rule is scoped per calendar month rather than per loan.
/// Modelling it as an amendment would mean each new skip overwrote the last.
/// </para>
///
/// <para>
/// <b>The end-date cascade IS ported</b> (it was previously omitted). Legacy's
/// <c>EmployeeLoanExemption.UpdateRecoveryEndDate</c> pushes <see cref="EmployeeLoan.EndDate"/> out by the
/// number of months skipped, when the payroll setting <c>AutoUpdateLoanRecoveryEndDate</c> is on. Without
/// it an exemption is cosmetic in the worst possible way: the employee simply underpays the loan by one
/// instalment and the schedule never catches up. It now lives in
/// <see cref="Services.ILoanRecoveryScheduleService"/> and is driven from
/// <c>LoanExemptionWorkflowTransactionHandler.OnApprovedAsync</c> (and reversed when an approved exemption
/// is deleted), so a pending exemption never moves anybody's loan.
/// </para>
/// </summary>
public class EmployeeLoanExemption : FullAuditedEntity, IMustHaveTenant, IHasEmployeeId, IWorkflowEnabledEntity
{
    public int TenantId { get; set; }

    /// <summary>
    /// The employee whose loan this is. Denormalised from <see cref="EmployeeLoan"/> rather than reached
    /// through it, for the same reason it is denormalised on <see cref="EmployeeDocumentMvmt"/>:
    /// self-service scoping (<see cref="IHasEmployeeId"/>) has to be a column on the row being scoped. A
    /// query that had to join out to the parent loan to discover whose row this is could not be filtered
    /// generically, and every ESS screen would hand-write that join instead -- which is how a scoped list
    /// ends up next to an unscoped write.
    /// </summary>
    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int EmployeeLoanId { get; set; }

    public virtual EmployeeLoan EmployeeLoan { get; set; }

    /// <summary>Any date inside the month whose instalment is being skipped.</summary>
    public virtual DateTime ExemptionDate { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowRequestId"/>
    public virtual int? WorkflowRequestId { get; set; }

    /// <inheritdoc cref="IWorkflowEnabledEntity.WorkflowStatus"/>
    public virtual RequestStatus? WorkflowStatus { get; set; }
}
