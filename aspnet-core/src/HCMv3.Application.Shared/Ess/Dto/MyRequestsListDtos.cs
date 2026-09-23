using System;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.Workflow;

namespace HCMv3.Ess.Dto;

/// <summary>
/// One request the current employee raised, of whatever type, as the central Requests screen lists it.
///
/// <para>
/// Every field comes from <c>WorkflowRequest</c> plus the <c>WorkflowTransactionType</c> catalog row -- no
/// transaction table is touched. That is the whole point of the single-table workflow design: the employee's
/// combined history is one indexed query, not a fan-out across twenty tables that would each have to be
/// paged independently and then merged in memory.
/// </para>
/// </summary>
public class MyRequestListItemDto : EntityDto<int>
{
    /// <summary>Catalog code, e.g. "EmployeeExitPermit". Raw, for filtering and routing -- never displayed.</summary>
    public string RequestType { get; set; }

    /// <summary>The catalog's display name, e.g. "Exit Permit". Null when the type has no catalog row, in
    /// which case the screen falls back to <see cref="RequestType"/> rather than showing a blank cell.</summary>
    public string RequestTypeNameEn { get; set; }

    public string RequestTypeNameAr { get; set; }

    /// <summary>The catalog's icon class, so a mixed list is scannable by type at a glance.</summary>
    public string IconClass { get; set; }

    /// <summary>
    /// The type's own self-service screen, e.g. "/app/main/ess/exit-permit". Carried so the row can offer
    /// "open on its own screen" without the client hardcoding a type-to-route map that would go stale the
    /// moment a tenant re-points one.
    /// </summary>
    public string EssRoute { get; set; }

    public string RequestNumber { get; set; }

    /// <summary>The one-line description the type's handler wrote at submit time.</summary>
    public string SummaryEn { get; set; }

    public string SummaryAr { get; set; }

    public RequestStatus RequestStatus { get; set; }

    public int WorkflowStep { get; set; }

    /// <summary>When it was raised. The screen's default sort, newest first.</summary>
    public DateTime CreationTime { get; set; }

    /// <summary>The transaction row this request governs, for the "open on its own screen" link.</summary>
    public int? TargetEntityId { get; set; }

    /// <summary>True when this asks to change an already-approved transaction rather than create one; the
    /// screen marks it, because "Annual Leave, 5 days" reads very differently as an amendment.</summary>
    public bool IsAmendment { get; set; }
}

/// <summary>
/// Filters for the central Requests screen.
///
/// <para>
/// Named <c>GetMyRequestListInput</c> rather than the obvious <c>GetMyRequestsInput</c> because that name is
/// already taken, by the per-type input the twenty individual self-service lists share. Two different DTOs
/// with one name would compile (different files, same namespace would not) but would collide in the
/// generated Angular proxy, which is flat.
/// </para>
/// </summary>
public class GetMyRequestListInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>Null means every status. <c>Open</c> is presented to the user as "Awaiting approval".</summary>
    public RequestStatus? Status { get; set; }

    /// <summary>Catalog code to narrow to one type; null for all types.</summary>
    public string RequestType { get; set; }

    /// <summary>Inclusive lower bound on the date raised.</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Inclusive upper bound on the date raised -- the whole of that day counts, not just midnight.</summary>
    public DateTime? ToDate { get; set; }

    /// <summary>Free text over request number and summary.</summary>
    public string Filter { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            // Newest first: the question this screen answers is "where is the thing I just raised".
            Sorting = "CreationTime DESC";
        }

        if (MaxResultCount <= 0)
        {
            MaxResultCount = 20;
        }
    }
}

/// <summary>How many of the employee's requests sit in one status, for the filter chips.</summary>
public class MyRequestStatusCountDto
{
    public RequestStatus Status { get; set; }

    public int Count { get; set; }
}

/// <summary>
/// A type the employee has actually raised at least once, for the type filter.
///
/// <para>
/// Deliberately derived from their own history rather than from the whole catalog: a dropdown listing
/// twenty types when the employee has only ever raised leave and exit permits is a worse filter than one
/// listing two.
/// </para>
/// </summary>
public class MyRequestTypeFilterDto
{
    public string RequestType { get; set; }

    public string NameEn { get; set; }

    public string NameAr { get; set; }

    public int Count { get; set; }
}
