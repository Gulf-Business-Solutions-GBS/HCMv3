using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service missing punches: the clock events an employee says were never recorded, and the state of
/// the requests they have raised for them.
///
/// <para>
/// The port of legacy <c>SkipPunchRequest</c>. What it creates is a raw <c>TimeEvent</c> -- a clock event --
/// not an attendance day: once granted, the Time Engine recomputes the day from the punches exactly as it
/// would have done had the badge worked. Routing this through attendance instead would hand payroll a day
/// nobody calculated.
/// </para>
/// </summary>
public interface IMyPunchAppService : IApplicationService
{
    Task<PagedResultDto<MyPunchRequestDto>> GetMyPunches(GetMyRequestsInput input);

    /// <summary>Creates the punch, hidden, and submits it for approval in the same call.</summary>
    Task<MyRequestSubmissionDto> RequestMissingPunch(RequestMissingPunchInput input);

    /// <summary>Withdraws the request while it is still theirs to withdraw.</summary>
    Task CancelMyPunch(EntityDto input);
}
