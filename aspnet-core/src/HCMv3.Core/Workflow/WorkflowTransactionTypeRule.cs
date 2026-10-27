using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.Workflow;

/// <summary>
/// One line of a transaction type's routing: requests of this type, optionally only of one sub type (Annual
/// Leave, Sick Leave ...) and optionally only for one action (New, Amendment, Cancellation, Return), go to
/// this approval chain.
///
/// <para>
/// The same three columns for every type, as legacy's <c>WorkflowGroupDetail</c> had them (RequestType,
/// RequestSubType, and the action as a sub type of <c>AbsenceRequest</c>). It replaces the three leave-shaped
/// columns the type used to carry -- a chain for amendments, one for cancellations, one for returns -- which
/// made the leave screen look different from every other type and could not say "sick leave cancellations".
/// </para>
///
/// <para>
/// Empty sub type and empty action mean "any". The most specific matching line wins; with none, the type's
/// default chain applies. Workflow groups (per part of the organisation) still take precedence over these,
/// exactly as before.
/// </para>
/// </summary>
public class WorkflowTransactionTypeRule : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowTransactionTypeId { get; set; }

    public virtual WorkflowTransactionType WorkflowTransactionType { get; set; }

    /// <summary>
    /// Id of a row in the type's sub type source (<see cref="WorkflowTransactionType.SubTypeSource"/>), e.g.
    /// an <c>AbsenceType</c> for leave. Null means every sub type.
    /// </summary>
    public virtual int? RequestSubTypeId { get; set; }

    /// <summary>Null means every action.</summary>
    public virtual WorkflowRequestAction? RequestAction { get; set; }

    public virtual int RequestMasterId { get; set; }

    public virtual RequestMaster RequestMaster { get; set; }
}
