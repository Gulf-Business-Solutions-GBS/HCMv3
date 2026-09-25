using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// The approver's inbox: everything waiting on the current user, across every transaction type, and the
/// actions they can take on it.
///
/// <para>
/// One service for all types rather than one per type. That is only possible because the workflow tables are
/// generic and each request carries its own summary -- the alternative, which legacy took, was a separate
/// approval screen per request type, ~30 of them.
/// </para>
/// </summary>
public interface IEssInboxAppService : IApplicationService
{
    Task<PagedResultDto<EssInboxItemDto>> GetMyInbox(GetMyInboxInput input);

    /// <summary>Count only -- for the menu badge and the landing tile, which should not pay for the list.</summary>
    Task<int> GetMyPendingCount();

    Task Approve(ApproveRequestInput input);

    Task Reject(RejectRequestInput input);

    Task SendBack(SendBackRequestInput input);

    // ------------------------------------------------------------------ get feedback

    /// <summary>
     /// Asks one or more colleagues what they think, WITHOUT handing over the decision.
    ///
    /// <para>
    /// The approver who asks stays the approver. Nothing about this moves the request, changes its step or
    /// makes it wait: it can be approved a second later with none of the answers in. That is the whole
    /// distinction from delegation, where the authority itself moves, and it is why this is offered
    /// alongside the decision buttons rather than among them.
    /// </para>
    /// </summary>
    Task RequestFeedback(RequestFeedbackInput input);

    /// <summary>
    /// Records what somebody asked for feedback has to say, and closes their ask.
    ///
    /// <para>
    /// Their comment goes into the same conversation everyone else is reading. It is not a separate
    /// channel -- an opinion nobody can see is not feedback.
    /// </para>
    /// </summary>
    Task SubmitFeedback(SubmitFeedbackInput input);

    /// <summary>
    /// Who the current approver may ask on a request.
    ///
    /// <para>
    /// Scoped to the request rather than returning the staff directory: the caller must be able to act on
    /// it, and the list excludes the requester and the caller themselves -- asking the person whose request
    /// it is what they think of it is not a thing anybody wants to do by accident.
    /// </para>
    /// </summary>
    Task<List<FeedbackCandidateDto>> GetFeedbackCandidates(GetFeedbackCandidatesInput input);

    /// <summary>
    /// Withdraws a feedback request nobody has answered yet -- asked the wrong person, or no longer needs
    /// their view.
    ///
    /// <para>
    /// Refused once they have answered. Their opinion is already in the conversation by then, and removing
    /// the ask would leave a comment on the request that nothing explains -- worse than the ask itself.
    /// </para>
    /// </summary>
    Task CancelFeedbackRequest(CancelFeedbackInput input);

    /// <summary>Who has been asked on a request, and whether they have answered.</summary>
    Task<List<FeedbackRequestDto>> GetFeedbackRequests(GetFeedbackCandidatesInput input);
}
