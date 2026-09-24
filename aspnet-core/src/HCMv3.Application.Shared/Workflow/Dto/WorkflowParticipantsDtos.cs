using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.Workflow.Dto;

/// <summary>Which step of the chain an approver entry describes, and how it was obtained.</summary>
public enum WorkflowApproverOrigin
{
    /// <summary>
    /// A live <c>WorkFlowApprover</c> row. This is the step the request is sitting on right now, and the
    /// only step for which the engine still holds the full list of people entitled to act.
    /// </summary>
    CurrentStep = 0,

    /// <summary>
    /// Reconstructed from <c>RequestLog</c>, because the engine deleted the step's approver rows when the
    /// request moved on. See <see cref="WorkflowApproverEntryDto.Origin"/> for what this costs.
    /// </summary>
    ReconstructedFromLog = 1
}

/// <summary>Which way round a delegation runs relative to the approver it was found for.</summary>
public enum WorkflowDelegationRelation
{
    /// <summary>
    /// The approver has handed their authority to the named stand-in. Under HCMv3's substitution rule the
    /// stand-in is the one the engine will actually route to, so this row explains why the approver list
    /// contains a name the org chart would not have predicted.
    /// </summary>
    ApproverDelegatedOut = 0,

    /// <summary>
    /// The approver is themselves covering for the named colleague -- they are on this request because
    /// somebody else's authority was handed to them.
    /// </summary>
    ApproverIsStandingIn = 1
}

public class GetWorkflowParticipantsInput
{
    [Range(1, int.MaxValue)]
    public int WorkflowRequestId { get; set; }
}

public class GetWorkflowParticipantProfileInput
{
    [Range(1, int.MaxValue)]
    public int WorkflowRequestId { get; set; }

    /// <summary>
    /// Must be somebody already ON that request -- its requester, one of its approvers, a delegate of one
    /// of them, or somebody who has opened it. See the authorization note on the service method.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int EmployeeId { get; set; }
}

/// <summary>
/// A colleague's directory card: enough to know who you are looking at, and nothing more.
///
/// <para>
/// <b>What is deliberately NOT here, and why.</b> This is a directory entry, not a personnel file. Salary,
/// bank details, civil ID, passport, date of birth, marital status, home address, personal mobile,
/// emergency contacts and medical flags are all on <c>Employee</c> and all deliberately excluded: an
/// approver needs to know that the person who signed off at step 2 is the Finance Manager in Kuwait City,
/// not what they earn or where they live. Everything on this DTO is work contact information of the kind
/// an internal staff directory carries.
/// </para>
/// </summary>
public class WorkflowParticipantProfileDto
{
    public int EmployeeId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string JobTitleEn { get; set; }

    public string JobTitleAr { get; set; }

    /// <summary>The organization unit of their current assignment. Often blank -- see the note in
    /// <c>MyDashboardAppService</c>: many deployments never populate it.</summary>
    public string Department { get; set; }

    public string WorkLocationEn { get; set; }

    public string WorkLocationAr { get; set; }

    /// <summary>The corporate address the system itself writes to. Not <c>AlternateEmail</c>, which is
    /// where people put their personal one.</summary>
    public string WorkEmail { get; set; }

    public string BusinessLandline { get; set; }

    public string BusinessExtension { get; set; }

    public string PhotoUrl { get; set; }
}

/// <summary>A person on one of the participant lists, rendered the same way wherever they appear.</summary>
public class WorkflowPersonDto
{
    public int EmployeeId { get; set; }

    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string JobTitleEn { get; set; }

    public string JobTitleAr { get; set; }

    /// <summary>
    /// Points at the cached photo endpoint rather than inlining the image, for the same reason the activity
    /// feed does: a list of six approvers must not carry six blobs off a seventy-column entity.
    /// </summary>
    public string PhotoUrl { get; set; }
}

/// <summary>One person entitled to act on this request, at one step.</summary>
public class WorkflowApproverEntryDto
{
    public WorkflowPersonDto Person { get; set; }

    public int WorkflowStep { get; set; }

    /// <summary>Whether this person has already given their approval at <see cref="WorkflowStep"/>.</summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// How this row was obtained.
    ///
    /// <para>
    /// <b>This flag exists because of a real defect, not for decoration.</b>
    /// <c>WorkflowApprovalService.AssignApproversForStepAsync</c> DELETES every <c>WorkFlowApprover</c> row
    /// for a request before inserting the next step's, so at step 3 nothing records who was entitled to act
    /// at step 2. Past steps are therefore rebuilt from <c>RequestLog</c>, which is never deleted and does
    /// retain who acted at which step.
    /// </para>
    ///
    /// <para>
    /// The cost is precise and worth stating: a reconstructed step lists the people who <em>acted</em>, not
    /// the people who were <em>entitled</em> to. Where a step had three eligible approvers and one of them
    /// approved, the other two are not recoverable. Only fixing the deletion restores that.
    /// </para>
    /// </summary>
    public WorkflowApproverOrigin Origin { get; set; }

    /// <summary>
    /// The action this person took, for reconstructed rows -- so a step that was rejected or sent back is
    /// not silently displayed as "not approved yet". Null for live rows, where nothing has happened.
    /// </summary>
    public RequestActionType? ActionType { get; set; }

    /// <summary>When they acted. Null while they have not.</summary>
    public DateTime? ActedAt { get; set; }
}

/// <summary>An active delegation touching one of this request's approvers.</summary>
public class WorkflowDelegateEntryDto
{
    /// <summary>The stand-in -- the person who may act because somebody handed them the authority.</summary>
    public WorkflowPersonDto Person { get; set; }

    /// <summary>The person whose authority it is.</summary>
    public WorkflowPersonDto DelegatedBy { get; set; }

    public WorkflowDelegationRelation Relation { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }
}

/// <summary>One recorded opening of the request.</summary>
public class WorkflowReadLogEntryDto
{
    public DateTime ReadAt { get; set; }

    /// <summary>Null when the stored user value does not resolve to an employee we still hold.</summary>
    public WorkflowPersonDto Person { get; set; }

    /// <summary>
    /// The raw <c>WFReadLog.User</c> value, shown as-is when it cannot be resolved to a person. Legacy's
    /// Read Log had a User column and this is it; keeping it means an unresolvable row still says something
    /// rather than rendering as a blank line.
    /// </summary>
    public string User { get; set; }
}

/// <summary>
/// Who is entitled to act on one request, who has acted, and who has merely looked.
///
/// <para>
/// This is the data behind legacy's "Workflow Actions &amp; Information" panel. The activity feed answers
/// <em>what happened</em>; this answers <em>who may act</em> -- two different questions, which is why one
/// chronological stream could not replace it.
/// </para>
///
/// <para>
/// Legacy's Request Log tab is deliberately absent. Every <c>RequestLog</c> row it displayed is already
/// served by <c>GetActivityFeed</c> and rendered in the conversation, and a grid restating the same rows
/// beside it would be the one genuinely redundant tab of the five.
/// </para>
/// </summary>
public class WorkflowParticipantsDto
{
    public int WorkflowRequestId { get; set; }

    public string RequestNumber { get; set; }

    public int WorkflowStep { get; set; }

    public RequestStatus RequestStatus { get; set; }

    /// <summary>
    /// The <c>WorkflowRole</c> configured on the step the request is sitting on. Null when the request
    /// cannot be routed to a <c>RequestMaster</c> at all, or when the step names no role -- neither is
    /// allowed to fail the read, because an unroutable request is exactly the one an administrator opens
    /// this panel to diagnose.
    /// </summary>
    public string CurrentWorkflowRoleNameEn { get; set; }

    /// <inheritdoc cref="CurrentWorkflowRoleNameEn"/>
    public string CurrentWorkflowRoleNameAr { get; set; }

    public List<WorkflowApproverEntryDto> Approvers { get; set; } = new List<WorkflowApproverEntryDto>();

    public List<WorkflowDelegateEntryDto> Delegates { get; set; } = new List<WorkflowDelegateEntryDto>();

    public List<WorkflowReadLogEntryDto> ReadLog { get; set; } = new List<WorkflowReadLogEntryDto>();
}
