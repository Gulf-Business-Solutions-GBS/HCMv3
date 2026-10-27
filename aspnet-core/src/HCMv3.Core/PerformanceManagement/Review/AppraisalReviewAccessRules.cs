using HCMv3.Workflow;

namespace HCMv3.PerformanceManagement.Review;

/// <summary>Who the signed-in employee is, relative to one appraisal, for <see cref="AppraisalReviewAccessRules"/>.</summary>
public class AppraisalReviewViewer
{
    /// <summary>The step's settings, or null when the chain defines none for the current step.</summary>
    public AppraisalStepSetting Setting { get; set; }

    /// <summary>Holds an approver row on the request's current step (legacy <c>WorkFlowApprover.Approver == loggedIn</c>).</summary>
    public bool IsApprover { get; set; }

    /// <summary>The appraisal is about this employee.</summary>
    public bool IsAppraisee { get; set; }

    /// <summary>This employee is on the appraisal's reviewer list.</summary>
    public bool IsReviewer { get; set; }

    /// <summary>Their reviewer row is flagged as a peer.</summary>
    public bool ReviewerIsPeer { get; set; }

    /// <summary>Their review has been submitted and is locked.</summary>
    public bool ReviewSubmitted { get; set; }

    /// <summary>
    /// The governing request's status; null when the appraisal is not workflow-managed. Approved, Rejected and
    /// Closed lock everything, as legacy ESS did.
    /// </summary>
    public RequestStatus? RequestStatus { get; set; }

    /// <summary>Appraisal Settings "Enable Marks In PA Review": marks instead of a rating scale.</summary>
    public bool EnableMarksInPAReview { get; set; }
}

/// <summary>What the signed-in employee may see and do on one appraisal.</summary>
public class AppraisalReviewAccess
{
    public bool IsOpen { get; set; }

    /// <summary>Copy KPI is offered.</summary>
    public bool CanCopyKpi { get; set; }

    /// <summary>Their own rating lines may be edited and saved.</summary>
    public bool CanSaveReview { get; set; }

    /// <summary>Submit Review is offered.</summary>
    public bool CanSubmitReview { get; set; }

    /// <summary>Add Peer is offered.</summary>
    public bool CanAddPeer { get; set; }

    /// <summary>The KPI list (legacy "Detail" tab).</summary>
    public bool ShowKpiDetails { get; set; }

    /// <summary>The rating section.</summary>
    public bool ShowRatings { get; set; }

    /// <summary>The reviewer list.</summary>
    public bool ShowReviewers { get; set; }

    /// <summary>Overall, peer and employee averages.</summary>
    public bool ShowAverages { get; set; }

    /// <summary>Marks are entered rather than a rating.</summary>
    public bool UseMarks { get; set; }

    /// <summary>Other non-peer reviewers' rating lines are shown, not only their own.</summary>
    public bool SeeNonPeerRatings { get; set; }

    /// <summary>Peers' rating lines are shown.</summary>
    public bool SeePeerRatings { get; set; }
}

/// <summary>
/// The legacy "who sees what" rules for a 360 appraisal, as one pure function so the screen and the server
/// read the same answer and the server can enforce it.
///
/// <para>
/// Ported from legacy <c>PAEmployeeController</c> (<c>OnActivated</c>, <c>CustomApplyAppearance</c>,
/// <c>HideShowProperties</c>, <c>FilterPAEmployeeRatingDataListView</c>) and the ESS
/// <c>RequestForm.cshtml</c>. The single idea legacy kept returning to is the "plain reviewer": somebody on
/// the reviewer list who is NOT an approver on this step. A plain reviewer sees only their own rating lines
/// -- no KPI tab, no reviewer list, no averages -- whatever the step flags say. Everybody else is governed
/// by the flags.
/// </para>
///
/// <para>
/// <b>Where the two legacy screens disagreed</b>, the more permissive one is kept, because the stricter one
/// could strand a step: XAF always let a plain reviewer rate, while ESS showed ratings to reviewers only with
/// "Can Peer Add Ratings". An auto-added manager on a step with that flag off would then have no way to rate,
/// would never submit, and "Auto Submit On Last Fill" would never fire.
/// </para>
///
/// <para>
/// <b>Can View Ratings / Can View Peer Ratings.</b> Legacy's only live use of them was the reviewer tab's
/// visibility; the per-reviewer filter that read them (<c>FilterPAEmployeeReviewerDataListView</c>) was
/// never called. The design note the owner accepted says they decide which rating sections are visible to
/// whom, which is exactly what that filter did, so it is honoured here: they open other reviewers' lines to
/// a viewer who is not a plain reviewer.
/// </para>
/// </summary>
public static class AppraisalReviewAccessRules
{
    public static AppraisalReviewAccess Evaluate(AppraisalReviewViewer viewer)
    {
        var access = new AppraisalReviewAccess
        {
            UseMarks = viewer.EnableMarksInPAReview,
            IsOpen = viewer.RequestStatus == null
                     || viewer.RequestStatus == RequestStatus.Open
                     || viewer.RequestStatus == RequestStatus.Draft
        };

        var setting = viewer.Setting;
        var plainReviewer = viewer.IsReviewer && !viewer.IsApprover;

        if (setting == null)
        {
            // Legacy wrapped every rule in "if (PARequest != null)". With no settings for the step nothing is
            // offered; a reviewer can still read their own lines.
            access.ShowRatings = viewer.IsReviewer || viewer.IsAppraisee;
            return access;
        }

        access.ShowKpiDetails = !plainReviewer && setting.CanAddKpi;

        access.SeeNonPeerRatings = !plainReviewer && setting.CanViewRatings;
        access.SeePeerRatings = !plainReviewer && setting.CanViewPeerRatings;

        // A view flag opens the section as well: "may see the peers' ratings" with no section to see them in
        // would be a switch that does nothing.
        access.ShowRatings = plainReviewer
                             || setting.CanAddRatings
                             || (setting.CanPeerAddRatings && viewer.IsReviewer)
                             || viewer.IsAppraisee
                             || access.SeeNonPeerRatings
                             || access.SeePeerRatings;

        access.ShowReviewers = !plainReviewer
                               && (setting.CanAddPeer || setting.CanViewRatings || setting.CanViewPeerRatings);

        // Marks replace the rating scale, and with it the average ratings, for everybody.
        access.ShowAverages = !viewer.EnableMarksInPAReview && !plainReviewer && setting.CanViewOverallAverageRating;

        // Legacy HideShowProperties: Copy KPI belongs to the approver on a step that allows it.
        access.CanCopyKpi = access.IsOpen && viewer.IsApprover && setting.CanAddKpi;

        var ownReviewPending = viewer.IsReviewer && !viewer.ReviewSubmitted && access.IsOpen;

        access.CanSaveReview = ownReviewPending && access.ShowRatings;

        // Legacy OnActivated, verbatim: a peer, the appraisee rating themselves, or an approver who rates on
        // an auto-submitting step. Any other approver who rates simply approves when done.
        access.CanSubmitReview = ownReviewPending
                                 && (viewer.ReviewerIsPeer
                                     || viewer.IsAppraisee
                                     || (viewer.IsApprover && setting.CanAddRatings && setting.AutoSubmitOnLastFill));

        // The approver or the employee may add peers; a peer may not add more peers.
        access.CanAddPeer = access.IsOpen
                            && setting.CanAddPeer
                            && (viewer.IsApprover || viewer.IsAppraisee)
                            && !(plainReviewer && viewer.ReviewerIsPeer);

        return access;
    }
}
