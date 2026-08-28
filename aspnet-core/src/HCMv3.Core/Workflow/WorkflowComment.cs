using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.Workflow;

/// <summary>
/// A free-form comment on a <see cref="WorkflowRequest"/> -- a question, an answer, a discussion between
/// approvers.
///
/// <para>
/// This capability does not exist in the legacy system and there was nothing to port. Legacy had two things
/// that look like comments and are not:
/// </para>
/// <list type="bullet">
/// <item><description><see cref="RequestLog"/> -- the immutable audit trail. Each row is ONE action
/// (approve, reject, send back) with at most one comment attached to it, so you could only ever comment by
/// acting on the request. There was no way to ask a question, answer one, or discuss anything.</description></item>
/// <item><description><see cref="WorkflowRemark"/> -- despite the name, a list of people looped in for
/// ad-hoc feedback. It has no text field at all.</description></item>
/// </list>
/// <para>
/// Legacy also kept a <c>WorkflowComments</c> string column on each of ~42 concrete request classes, onto
/// which the approver's comment was string-concatenated with their name and a timestamp
/// (<c>RequestProcessApproval</c> assigns it 104 times). That column was capped at 1000 characters for the
/// entire conversation, used two different name/date formats depending on which branch ran, stored a NAME
/// rather than an employee id -- so no avatar was ever possible -- and duplicated data already held
/// properly in <see cref="RequestLog"/>. It is deliberately not ported; the activity feed reads
/// <see cref="RequestLog"/> and this table instead.
/// </para>
/// </summary>
public class WorkflowComment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int WorkflowRequestId { get; set; }

    public virtual WorkflowRequest WorkflowRequest { get; set; }

    /// <summary>
    /// The comment being replied to, or <c>null</c> for a top-level comment. Self-referencing rather than a
    /// flat list so a long thread stays readable; the feed renders one level of replies.
    /// </summary>
    public virtual int? ParentCommentId { get; set; }

    public virtual WorkflowComment ParentComment { get; set; }

    public virtual ICollection<WorkflowComment> Replies { get; set; }

    /// <summary>
    /// Who wrote it, as an employee rather than a user, so the feed can show their photo and job title.
    /// This is the specific thing legacy's name-string column could never support.
    /// </summary>
    public virtual int AuthorEmployeeId { get; set; }

    public virtual Employee AuthorEmployee { get; set; }

    [Required]
    [StringLength(MaxBodyLength)]
    public virtual string Body { get; set; }

    /// <summary>
    /// Visible to approvers only, never to the employee who raised the request.
    ///
    /// <para>
    /// Approvers routinely need to discuss a request between themselves -- whether an exception is
    /// justified, what HR advised -- without the requester reading it. Legacy had no way to express that, so
    /// those conversations happened over e-mail and the reasoning left the system entirely. Enforced in
    /// <c>WorkflowActivityAppService</c>, which never returns internal comments to the requester.
    /// </para>
    /// </summary>
    public virtual bool IsInternal { get; set; }

    /// <summary>
    /// Set when the body has been changed after posting. The feed marks it, so an edited comment cannot
    /// quietly rewrite what an approver replied to.
    /// </summary>
    public virtual bool IsEdited { get; set; }

    /// <summary>
    /// Generous on purpose. Legacy's 1000-character cap applied to the WHOLE conversation, not one comment,
    /// so a busy request silently stopped accepting them.
    /// </summary>
    public const int MaxBodyLength = 4000;
}
