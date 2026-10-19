using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service appraisals: the employee's own, the ones they review, and their team's -- and the review
/// itself (ratings, Save Review, Submit Review, Add Peer, Copy KPI).
///
/// <para>
/// Every action is taken as the SIGNED-IN employee, never as an id the screen sends: a reviewer fills in only
/// their own lines, and what each person may do is worked out on the server from the appraisal's current
/// step (see <c>AppraisalReviewAccessRules</c>) and enforced on every write, not only by hiding buttons.
/// </para>
///
/// <para>
/// Not a <c>MyTransactionAppServiceBase</c> service: an appraisal is opened FOR an employee by HR, not raised
/// by them, so there is nothing here to create, draft or withdraw. Bulk launch for an organization unit stays
/// on the admin screen (owner's decision).
/// </para>
/// </summary>
public interface IMyAppraisalAppService : IApplicationService
{
    /// <summary>One of the three lists.</summary>
    Task<PagedResultDto<MyAppraisalListDto>> GetMyAppraisals(GetMyAppraisalsInput input);

    /// <summary>The appraisal as the signed-in employee may see it, with what they may do.</summary>
    Task<MyAppraisalReviewDto> GetAppraisalReview(EntityDto input);

    /// <summary>Save Review: writes the signed-in reviewer's lines and rescores. Nothing is locked.</summary>
    Task SaveReview(SaveMyAppraisalReviewInput input);

    /// <summary>
    /// Submit Review: saves, checks every line is complete, and locks the reviewer's part. On an "auto submit
    /// on last fill" step, the last outstanding submission approves the step as its first approver.
    /// </summary>
    Task<SubmitMyAppraisalReviewResultDto> SubmitReview(SaveMyAppraisalReviewInput input);

    /// <summary>The KPI sets Copy KPI can take from.</summary>
    Task<MyAppraisalKpiSourcesDto> GetKpiSources(EntityDto input);

    /// <summary>Copy KPI from a job title, an organization unit or another employee's appraisal in the same cycle.</summary>
    Task CopyKpi(CopyMyAppraisalKpiInput input);

    /// <summary>Who may be added as a peer.</summary>
    Task<List<MyAppraisalPersonDto>> GetPeerCandidates(GetMyAppraisalPeerCandidatesInput input);

    /// <summary>Add Peer.</summary>
    Task AddPeer(AddMyAppraisalPeerInput input);
}
