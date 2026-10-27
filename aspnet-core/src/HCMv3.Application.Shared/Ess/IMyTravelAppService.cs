using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service travel requests: the employee's own trips, and asking for a new one.
///
/// <para>
/// The front half of travel, and until now the missing half. <c>IMyTravelClaimAppService</c> lets an
/// employee claim the expenses of a trip; this is what lets the trip exist in the first place without an
/// administrator keying it in by hand.
/// </para>
/// <para>
/// Deliberately not the admin <c>ITravelsAppService</c> with a filter bolted on. That one must let the
/// travel desk book for any employee; this one must not. Both sit on the same entity and the same workflow
/// handler, so the rules cannot diverge -- only the surface differs.
/// </para>
/// </summary>
public interface IMyTravelAppService : IApplicationService
{
    Task<PagedResultDto<MyTravelDto>> GetMyTravels(GetMyRequestsInput input);

    /// <summary>The travel types the employee may request against.</summary>
    Task<List<TravelTypeLookupDto>> GetTravelTypes();

    /// <summary>
    /// Creates the trip -- header, legs and cost lines in one graph -- and, when the type is
    /// workflow-enabled, submits it in the same call. The trip is hidden until it is approved.
    /// </summary>
    Task<MyRequestSubmissionDto> RequestTravel(RequestTravelInput input);

    /// <summary>Withdraws a request while it is still theirs to withdraw, taking its legs and costs with it.</summary>
    Task CancelMyTravel(EntityDto input);
}
