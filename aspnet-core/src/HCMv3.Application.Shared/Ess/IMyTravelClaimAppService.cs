using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service travel expense claims: an employee's own records, and raising a new one.
///
/// <para>
/// Deliberately not the admin service with a filter bolted on. That one must let HR pick any employee and
/// bypass approval; this one must do neither. Both sit on the same entity and the same workflow handler,
/// so the rules cannot diverge -- only the surface differs.
/// </para>
/// </summary>
public interface IMyTravelClaimAppService : IApplicationService
{
    Task<PagedResultDto<MyTravelClaimDto>> GetMyTravelClaims(GetMyRequestsInput input);

    /// <summary>Creates the record and, when the type is workflow-enabled, submits it in one step. The
    /// employee is never left holding a draft they have to remember to send.</summary>
    Task<MyRequestSubmissionDto> RequestTravelClaim(RequestTravelClaimInput input);

    /// <summary>Withdraws a request while it is still theirs to withdraw.</summary>
    Task CancelMyTravelClaim(EntityDto input);
}
