using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service exit permits: an employee's own permits, and recording a new one for internal sign-off.
///
/// <para>
/// Note what is NOT being requested here. The permit is issued by the government, outside this system, and
/// the employee already holds it before they open this screen -- so the number is keyed in, not generated,
/// and approval decides nothing about the permit itself. What is being asked for is the company's own
/// agreement to the absence.
/// </para>
///
/// <para>
/// Deliberately not the admin service with a filter bolted on. That one must let HR pick any employee and
/// bypass approval; this one must do neither. Both sit on the same entity and the same workflow handler, so
/// the rules cannot diverge -- only the surface differs.
/// </para>
/// </summary>
public interface IMyExitPermitAppService : IApplicationService
{
    Task<PagedResultDto<MyExitPermitDto>> GetMyExitPermits(GetMyRequestsInput input);

    /// <summary>Creates the record and, when the type is workflow-enabled, submits it in one step. The
    /// employee is never left holding a draft they have to remember to send.</summary>
    Task<MyRequestSubmissionDto> RequestExitPermit(RequestExitPermitInput input);

    /// <summary>
    /// Files the permit as a draft, or updates one already filed. Nothing is sent, nothing is effective,
    /// and nobody but the requester can see it.
    ///
    /// <para>
    /// The one call covers both because that is how the screen uses it: the form does not change when it is
    /// reopened on a draft, so neither does the call it makes. Which one it is, is the presence of an id.
    /// </para>
    /// </summary>
    Task<MyRequestSubmissionDto> SaveMyExitPermitDraft(SaveExitPermitDraftInput input);

    /// <summary>
    /// Reopens one of the requester's own drafts in the form. Refuses anything that is not theirs and not
    /// still a draft -- once it is with an approver the form is no longer the way to change it.
    /// </summary>
    Task<MyExitPermitDto> GetMyExitPermitDraftForEdit(EntityDto input);

    /// <summary>
    /// Sends a saved draft into the approval chain. This is the point at which it is validated, gets its
    /// request number, and lands on an approver -- none of which a draft has.
    /// </summary>
    Task<MyRequestSubmissionDto> SubmitMyExitPermitDraft(EntityDto input);

    /// <summary>Withdraws a request while it is still theirs to withdraw. Discards a draft, too.</summary>
    Task CancelMyExitPermit(EntityDto input);
}
