using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service employment letters: a salary certificate for a bank, a salary-continuity letter for a
/// landlord, an experience certificate for an embassy. Today this is an email to HR; here it is a request
/// that routes, and that the employee can watch.
///
/// <para>
/// What approval means for this type is unlike every other one. Approving a leave moves quota; approving a
/// loan schedules deductions. Approving a letter writes nothing anywhere -- it UNLOCKS PRINTING, and that is
/// all. So the list DTO carries an explicit <see cref="Dto.MyLetterRequestDto.CanPrint"/> flag: the
/// employee's question is "is my certificate ready?", and that flag is the answer.
/// </para>
///
/// <para>
/// Deliberately not the admin service with a filter bolted on, for the same reason as every other ESS
/// service: that one must let HR pick any employee and bypass approval; this one must do neither. Both sit
/// on the same entity and the same workflow handler, so the rules cannot diverge.
/// </para>
/// </summary>
public interface IMyLetterRequestAppService : IApplicationService
{
    Task<PagedResultDto<MyLetterRequestDto>> GetMyLetterRequests(GetMyRequestsInput input);

    /// <summary>Creates the request and, when the type is workflow-enabled, submits it in one step.</summary>
    Task<MyRequestSubmissionDto> RequestLetter(RequestLetterInput input);

    /// <summary>
    /// Files the request as a draft, or updates one already filed -- or a request an approver sent back.
    /// Nothing is sent. The kind of letter must already be a letter type; the rest is checked at submit.
    /// </summary>
    Task<MyRequestSubmissionDto> SaveMyLetterRequestDraft(SaveLetterDraftInput input);

    /// <summary>Reopens one of the requester's own drafts, or a request sent back to them, in the form.
    /// Refuses anything that is not theirs or is no longer theirs to change.</summary>
    Task<MyLetterRequestDto> GetMyLetterRequestDraftForEdit(EntityDto input);

    /// <summary>Sends a saved draft for approval, or sends a sent-back request again, after applying the
    /// rules a one-step request is held to.</summary>
    Task<MyRequestSubmissionDto> SubmitMyLetterRequestDraft(EntityDto input);

    /// <summary>Withdraws the request while it is still theirs to withdraw. Discards a draft, too.</summary>
    Task CancelMyLetterRequest(EntityDto input);
}
