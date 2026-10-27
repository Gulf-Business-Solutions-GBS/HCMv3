using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service documents: an employee's own records, and raising a new one.
///
/// <para>
/// Deliberately not the admin service with a filter bolted on. That one must let HR pick any employee and
/// bypass approval; this one must do neither. Both sit on the same entity and the same workflow handler,
/// so the rules cannot diverge -- only the surface differs.
/// </para>
/// </summary>
public interface IMyDocumentAppService : IApplicationService
{
    Task<PagedResultDto<MyDocumentRequestDto>> GetMyDocuments(GetMyRequestsInput input);

    /// <summary>Creates the record and, when the type is workflow-enabled, submits it in one step. The
    /// employee is never left holding a draft they have to remember to send.</summary>
    Task<MyRequestSubmissionDto> RequestDocument(RequestDocumentInput input);

    /// <summary>
    /// Files the document as a draft, or updates one already filed -- or a request an approver sent back.
    /// Nothing is sent, and the duplicate check does not run: that is a rule about what is submitted.
    /// </summary>
    Task<MyRequestSubmissionDto> SaveMyDocumentDraft(SaveDocumentDraftInput input);

    /// <summary>Reopens one of the requester's own drafts, or a request sent back to them, in the form.
    /// Refuses anything that is not theirs or is no longer theirs to change.</summary>
    Task<MyDocumentRequestDto> GetMyDocumentDraftForEdit(EntityDto input);

    /// <summary>Sends a saved draft for approval, or sends a sent-back request again, after applying the
    /// rules a one-step request is held to -- the duplicate check among them.</summary>
    Task<MyRequestSubmissionDto> SubmitMyDocumentDraft(EntityDto input);

    /// <summary>Withdraws a request while it is still theirs to withdraw. Discards a draft, too.</summary>
    Task CancelMyDocument(EntityDto input);
}
