using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>
/// Adds reviewers to a 360 appraisal under the legacy rules (<c>PAEmployeeReviewer.PerformValidation</c> and
/// <c>FillReview</c>, and <c>WorkFlowServices.OperateApproverInfo</c>'s appraisal branch). One place, so the
/// admin reviewer grid, self-service Add Peer and the automatic additions all refuse the same things.
/// </summary>
public interface IAppraisalReviewerManager : IDomainService
{
    /// <summary>
    /// Adds one reviewer and creates an empty rating line for every KPI already on the appraisal.
    /// Throws when the person is already a reviewer, when the form type's reviewer limit is reached, or when
    /// the employee is nominating more peers than the step allows.
    /// </summary>
    /// <param name="appraisal">The appraisal.</param>
    /// <param name="reviewerEmployeeId">Who will review.</param>
    /// <param name="isPeer">Legacy <c>IsPeer</c>: peers can be removed again; the employee, approvers and the KPI owner cannot.</param>
    /// <param name="peerAddedById">Who added them. The employee's own nominations are what the peer limit counts.</param>
    /// <param name="setting">The current step's settings, for the peer limit. Null skips that check, as there is nothing to check against.</param>
    Task<PAEmployeeReviewer> AddReviewerAsync(
        PAEmployee appraisal,
        int reviewerEmployeeId,
        bool isPeer,
        int? peerAddedById,
        AppraisalStepSetting setting);

    /// <summary>
    /// The reviewer-save validation on its own, for a caller that writes the row itself (the admin reviewer
    /// grid). <paramref name="reviewer"/> may be new (Id 0) or existing.
    /// </summary>
    Task ValidateAsync(PAEmployeeReviewer reviewer, PAEmployee appraisal, AppraisalStepSetting setting);

    /// <summary>
    /// The moment approvers are assigned to a step: adds the employee (self rate), each approver (can add
    /// ratings), the current manager and related employees, as that step's settings say. Never adds the same
    /// person twice; manager and relationship reviewers are peers, exactly as legacy marked them.
    /// </summary>
    /// <returns>The reviewer rows that were added.</returns>
    Task<List<PAEmployeeReviewer>> AddStepReviewersAsync(
        PAEmployee appraisal,
        AppraisalStepSetting setting,
        IReadOnlyCollection<int> approverEmployeeIds);
}
