using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// The requester's own side of workflow: everything this employee has raised, of every type, in one place.
///
/// <para>
/// The mirror image of <see cref="IEssInboxAppService"/>, which answers "what is waiting on me" for an
/// approver. This answers "where is what I asked for" for the person who asked. Until now that question had
/// no single answer -- an employee had to visit twenty separate screens, one per transaction type, and
/// remember which of them they had used.
/// </para>
///
/// <para>
/// One service for all types, for the same reason the inbox is one service: every request of every type is
/// one row in <c>WorkflowRequest</c> carrying its own summary, so the combined list is a single query. The
/// per-type services remain, because raising and amending a request needs the type's own fields; only the
/// looking-up is unified here.
/// </para>
/// </summary>
public interface IMyRequestsAppService : IApplicationService
{
    /// <summary>
    /// The current employee's requests across every type, paged, filtered by status, type, date range and
    /// free text.
    ///
    /// <para>
    /// A user with no linked employee record gets an empty page rather than an error: this is a landing
    /// screen, and an administrator opening it should see "nothing here", not a failure.
    /// </para>
    /// </summary>
    Task<PagedResultDto<MyRequestListItemDto>> GetMyRequests(GetMyRequestListInput input);

    /// <summary>Counts per status, for the filter chips -- so the screen does not have to fetch six pages to
    /// label six buttons.</summary>
    Task<List<MyRequestStatusCountDto>> GetMyRequestStatusCounts();

    /// <summary>The types this employee has actually raised, for the type filter.</summary>
    Task<List<MyRequestTypeFilterDto>> GetMyRequestTypes();
}
