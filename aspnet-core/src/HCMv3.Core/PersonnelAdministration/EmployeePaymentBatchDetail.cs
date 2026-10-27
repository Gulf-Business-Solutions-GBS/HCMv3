using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// One employee's line inside an <see cref="EmployeePaymentBatch"/> -- who is being paid, and how much
/// (ported from legacy XAF <c>HCM10.Module.BusinessObjects.Workflow.Payroll.PaymentRequestDetail</c>).
///
/// <para>
/// This row is the PROPOSAL. It becomes an <see cref="EmployeePayment"/> only when the batch is approved,
/// and only once -- see <c>PaymentBatchWorkflowTransactionHandler.OnApprovedAsync</c>. Until then payroll
/// cannot see it, because the batch that owns it is hidden by the <c>ApprovedOnly</c> filter.
/// </para>
///
/// <para>
/// Deliberately NOT <c>IWorkflowEnabledEntity</c>: the line has no approval state of its own. What is
/// approved is the batch, whole. A per-line status would allow half a decision.
/// </para>
/// </summary>
public class EmployeePaymentBatchDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeePaymentBatchId { get; set; }

    public virtual EmployeePaymentBatch EmployeePaymentBatch { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    /// <summary>
    /// Legacy display name "Number". <c>double</c> rather than <c>decimal</c> to match
    /// <see cref="EmployeePayment.Amount"/> exactly -- the value is copied across on approval, and a type
    /// change on one side only would round the money twice. Must not be zero: legacy said so in
    /// <c>PaymentRequestDetail.PerformValidation</c> and then never invoked the method, so the rule has never
    /// actually run. It runs now, in <see cref="Services.IEmployeePaymentBatchService"/>.
    /// </summary>
    public virtual double Amount { get; set; }
}
