using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service delegation of duties: the employee's own delegations, and asking a colleague to cover.
///
/// <para>
/// This closes a real gap rather than adding a screen. An <c>EmployeeDelegation</c> row already has a system
/// consequence -- the notification service copies the delegate on the delegator's mail, and it is the row an
/// approver-substitution branch reads -- but until now the only way to create one was for an administrator
/// to insert it by hand.
/// </para>
///
/// <para>
/// The employee is always the one giving authority up, taken from the session. HR raising cover on somebody
/// else's behalf goes through the admin service instead, which routes the identical entity through the
/// identical handler -- so the rules cannot diverge between the two doors.
/// </para>
/// </summary>
public interface IMyDelegationAppService : IApplicationService
{
    Task<PagedResultDto<MyDelegationDto>> GetMyDelegations(GetMyRequestsInput input);

    /// <summary>Creates the delegation and, when the type is workflow-enabled, submits it in one step. Until
    /// it is approved the cover does not exist as far as the engine is concerned.</summary>
    Task<MyRequestSubmissionDto> RequestDelegation(RequestDelegationInput input);

    /// <summary>Withdraws the request while it is still theirs to withdraw.</summary>
    Task CancelMyDelegation(EntityDto input);
}
