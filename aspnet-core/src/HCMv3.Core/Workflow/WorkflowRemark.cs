using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow;

/// <summary>
/// One request for somebody's opinion on a <see cref="WorkflowRequest"/> -- "Get Feedback" on screen.
///
/// <para>
/// An approver who wants a colleague's view asks for it here. The approval does not move: the approver who
/// asked is still the one the request is waiting on, still the only one who can approve, reject or send it
/// back. The person asked can read the request and say what they think, and nothing they do or fail to do
/// blocks the decision -- a feedback request that is never answered must not be able to strand a request,
/// because the whole point of asking is that the answer is optional.
/// </para>
///
/// <para>
/// The legacy class this is ported from was called <c>WorkflowRemark</c> and the table still is. The name
/// describes the wrong half of the thing -- the remark is what may come back, while the row itself is the
/// asking -- but renaming a table costs a migration and every reference to it, and the screens have been
/// relabelled where it matters, which is where people read it.
/// </para>
/// </summary>
public class WorkflowRemark : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowRequestId { get; set; }

    public virtual WorkflowRequest WorkflowRequest { get; set; }

    /// <summary>Who was asked.</summary>
    public virtual int? CCReceiverId { get; set; }

    public virtual Employee CCReceiver { get; set; }

    /// <summary>
    /// Who asked them.
    ///
    /// <para>
    /// Recorded because the first question of anyone receiving one of these is "why has this come to me?".
    /// Without it the request simply appears in their list, from nobody, about a transaction they have no
    /// part in.
    /// </para>
    /// </summary>
    public virtual int? RequestedById { get; set; }

    public virtual Employee RequestedBy { get; set; }

    /// <summary>
    /// The step the request was sitting on when the feedback was asked for.
    ///
    /// <para>
    /// Kept because a request moves on and the asking does not move with it. Six months later, "who asked
    /// whom, at which point in the chain" is the question this row is read to answer.
    /// </para>
    /// </summary>
    public virtual int WorkflowStep { get; set; }

    /// <summary>What the approver wanted looked at. Optional -- some asks need no framing.</summary>
    public virtual string RequestNote { get; set; }

    /// <summary>
    /// When they answered, or null while they have not.
    ///
    /// <para>
    /// This is the only thing separating an outstanding ask from a settled one, and so the only thing that
    /// lets a list say "waiting on you" without lying. It is deliberately not a status enum: there are two
    /// states, they are answered and not answered, and a timestamp says which while also saying when.
    /// </para>
    /// </summary>
    public virtual DateTime? RespondedAt { get; set; }
}
