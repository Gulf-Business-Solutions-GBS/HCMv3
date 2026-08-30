using System.Collections.Generic;

namespace HCMv3.Ess.Dto;

/// <summary>
/// Everything the self-service shell needs on load: who the signed-in user is as an employee, what they may
/// raise, and how much is waiting on them.
///
/// <para>
/// One call rather than several because it is on the critical path of every ESS page load, and because the
/// answers are interdependent -- an unlinked user can raise nothing regardless of permissions.
/// </para>
/// </summary>
public class EssContextDto
{
    /// <summary>
    /// False when the signed-in user has no <c>Employee.UserId</c> link. This is the state the shell must
    /// handle first: the user may hold every ESS permission and still have no records, because scope
    /// resolves to "sees nothing" rather than "sees everything". Show the explanation, not an empty list.
    /// </summary>
    public bool IsLinkedToEmployee { get; set; }

    public int? EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public string EmployeeFullNameAr { get; set; }

    /// <summary>Whether a photo exists, so the shell can decide between an avatar and initials without
    /// pulling the image blob into this payload.</summary>
    public bool HasPhoto { get; set; }

    /// <summary>
    /// True when the employee has at least one current direct report. Drives whether team views are offered
    /// -- being a manager is a fact about the org chart, not a permission, so it is answered here rather
    /// than by granting <c>Ess.MyTeam</c> to individuals.
    /// </summary>
    public bool IsManager { get; set; }

    /// <summary>Requests waiting on this employee to approve, across every transaction type -- the inbox badge.</summary>
    public int PendingApprovalCount { get; set; }

    /// <summary>
    /// The employee's own requests still awaiting a decision, across every type.
    ///
    /// <para>
    /// Distinct from <see cref="PendingApprovalCount"/> and easily confused with it: this is what the
    /// employee is waiting on somebody else for, that one is what somebody else is waiting on them for.
    /// Most people are on both sides of the workflow, so the landing page has to say which is which.
    /// </para>
    /// </summary>
    public int MyPendingRequestCount { get; set; }

    /// <summary>The employee's most recent requests, newest first -- the landing page's activity list.</summary>
    public List<EssRecentRequestDto> RecentRequests { get; set; } = new List<EssRecentRequestDto>();

    /// <summary>
    /// The transaction types this user may raise: ESS-enabled and active in the catalog, and permitted to
    /// them. Ordered for display. The menu is built from this rather than hardcoded, so enabling a seventh
    /// transaction type is a catalog row, not a menu code change.
    /// </summary>
    public List<EssTransactionTypeDto> AvailableTransactionTypes { get; set; } = new List<EssTransactionTypeDto>();
}

public class EssTransactionTypeDto
{
    public string Code { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public string EssRoute { get; set; }

    public string IconClass { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>
    /// False when the type is ESS-visible but not routed for approval, so the form can say the record takes
    /// effect immediately instead of implying an approval that will never come.
    /// </summary>
    public bool IsWorkflowEnabled { get; set; }
}

/// <summary>
/// One of the employee's own recent requests, as the landing page lists them.
///
/// <para>
/// Built entirely from <c>WorkflowRequest</c> -- the summary its handler wrote at submit, and the catalog's
/// name for the type. Nothing here reaches into a transaction table, which is what lets one list mix leave,
/// assets and loans without a join per row.
/// </para>
/// </summary>
public class EssRecentRequestDto
{
    public int Id { get; set; }

    public string RequestType { get; set; }

    public string RequestTypeNameEn { get; set; }

    public string RequestTypeNameAr { get; set; }

    public string RequestNumber { get; set; }

    public string SummaryEn { get; set; }

    public string SummaryAr { get; set; }

    public HCMv3.Workflow.RequestStatus RequestStatus { get; set; }

    public System.DateTime SubmittedOn { get; set; }

    /// <summary>True when this request is a change to an already-approved transaction rather than a new
    /// one, so the list can label it as such.</summary>
    public bool IsAmendment { get; set; }
}
