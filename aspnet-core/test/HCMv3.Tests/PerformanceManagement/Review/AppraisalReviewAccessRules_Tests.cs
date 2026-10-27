using HCMv3.PerformanceManagement.Review;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement.Review;

/// <summary>
/// The legacy "who sees what" rules for a 360 appraisal (<see cref="AppraisalReviewAccessRules"/>), ported
/// from <c>PAEmployeeController</c> and the ESS RequestForm. Pure, so no database.
/// </summary>
// ReSharper disable once InconsistentNaming
public class AppraisalReviewAccessRules_Tests
{
    private static AppraisalStepSetting Setting(System.Action<AppraisalStepSetting> configure = null)
    {
        var setting = new AppraisalStepSetting { Step = 2 };
        configure?.Invoke(setting);
        return setting;
    }

    private static AppraisalReviewAccess Evaluate(AppraisalReviewViewer viewer)
    {
        viewer.RequestStatus ??= RequestStatus.Open;
        return AppraisalReviewAccessRules.Evaluate(viewer);
    }

    [Fact]
    public void Should_Offer_Nothing_When_The_Step_Has_No_Settings()
    {
        // Legacy wrapped every rule in "if (PARequest != null)".
        var access = Evaluate(new AppraisalReviewViewer { Setting = null, IsReviewer = true, IsApprover = true });

        access.CanSaveReview.ShouldBeFalse();
        access.CanSubmitReview.ShouldBeFalse();
        access.CanCopyKpi.ShouldBeFalse();
        access.CanAddPeer.ShouldBeFalse();
        access.ShowRatings.ShouldBeTrue();
    }

    [Fact]
    public void A_Peer_Should_See_Only_Their_Own_Ratings_Whatever_The_Flags_Say()
    {
        var access = Evaluate(new AppraisalReviewViewer
        {
            Setting = Setting(s =>
            {
                s.CanAddKpi = true;
                s.CanViewRatings = true;
                s.CanViewPeerRatings = true;
                s.CanViewOverallAverageRating = true;
            }),
            IsReviewer = true,
            ReviewerIsPeer = true
        });

        access.ShowRatings.ShouldBeTrue();
        access.ShowKpiDetails.ShouldBeFalse();
        access.ShowReviewers.ShouldBeFalse();
        access.ShowAverages.ShouldBeFalse();
        access.SeeNonPeerRatings.ShouldBeFalse();
        access.SeePeerRatings.ShouldBeFalse();
    }

    [Fact]
    public void A_Pending_Peer_Should_Be_Able_To_Save_And_Submit()
    {
        var access = Evaluate(new AppraisalReviewViewer { Setting = Setting(), IsReviewer = true, ReviewerIsPeer = true });

        access.CanSaveReview.ShouldBeTrue();
        access.CanSubmitReview.ShouldBeTrue();
    }

    [Fact]
    public void The_Appraisee_Rating_Themselves_Should_Be_Able_To_Submit()
    {
        var access = Evaluate(new AppraisalReviewViewer { Setting = Setting(), IsReviewer = true, IsAppraisee = true });

        access.CanSubmitReview.ShouldBeTrue();
    }

    [Fact]
    public void An_Approver_Who_Rates_On_An_Ordinary_Step_Should_Save_But_Not_Submit()
    {
        // Legacy hid Submit Review from such an approver: they finish by approving.
        var access = Evaluate(new AppraisalReviewViewer
        {
            Setting = Setting(s => s.CanAddRatings = true),
            IsApprover = true,
            IsReviewer = true
        });

        access.CanSaveReview.ShouldBeTrue();
        access.CanSubmitReview.ShouldBeFalse();
    }

    [Fact]
    public void An_Approver_Who_Rates_On_An_Auto_Submitting_Step_Should_Submit()
    {
        var access = Evaluate(new AppraisalReviewViewer
        {
            Setting = Setting(s =>
            {
                s.CanAddRatings = true;
                s.AutoSubmitOnLastFill = true;
            }),
            IsApprover = true,
            IsReviewer = true
        });

        access.CanSubmitReview.ShouldBeTrue();
    }

    [Fact]
    public void A_Submitted_Review_Should_Be_Locked()
    {
        var access = Evaluate(new AppraisalReviewViewer { Setting = Setting(), IsReviewer = true, ReviewerIsPeer = true, ReviewSubmitted = true });

        access.CanSaveReview.ShouldBeFalse();
        access.CanSubmitReview.ShouldBeFalse();
    }

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Closed)]
    public void A_Decided_Appraisal_Should_Allow_No_Change(RequestStatus status)
    {
        var access = Evaluate(new AppraisalReviewViewer
        {
            Setting = Setting(s =>
            {
                s.CanAddKpi = true;
                s.CanAddPeer = true;
            }),
            IsApprover = true,
            IsReviewer = true,
            ReviewerIsPeer = true,
            RequestStatus = status
        });

        access.IsOpen.ShouldBeFalse();
        access.CanSaveReview.ShouldBeFalse();
        access.CanSubmitReview.ShouldBeFalse();
        access.CanCopyKpi.ShouldBeFalse();
        access.CanAddPeer.ShouldBeFalse();
    }

    [Fact]
    public void Copy_Kpi_Should_Belong_To_The_Approver_On_A_Step_That_Allows_It()
    {
        var setting = Setting(s => s.CanAddKpi = true);

        Evaluate(new AppraisalReviewViewer { Setting = setting, IsApprover = true }).CanCopyKpi.ShouldBeTrue();
        Evaluate(new AppraisalReviewViewer { Setting = setting, IsAppraisee = true }).CanCopyKpi.ShouldBeFalse();
        Evaluate(new AppraisalReviewViewer { Setting = Setting(), IsApprover = true }).CanCopyKpi.ShouldBeFalse();
    }

    [Fact]
    public void Marks_Should_Hide_The_Averages_Even_Where_The_Step_Shows_Them()
    {
        var access = Evaluate(new AppraisalReviewViewer
        {
            Setting = Setting(s => s.CanViewOverallAverageRating = true),
            IsApprover = true,
            EnableMarksInPAReview = true
        });

        access.ShowAverages.ShouldBeFalse();
        access.UseMarks.ShouldBeTrue();
    }

    [Fact]
    public void The_Approver_Should_See_Averages_When_The_Step_Shows_Them()
    {
        Evaluate(new AppraisalReviewViewer { Setting = Setting(s => s.CanViewOverallAverageRating = true), IsApprover = true })
            .ShowAverages.ShouldBeTrue();
    }

    [Fact]
    public void The_View_Flags_Should_Open_Other_Reviewers_Lines_To_The_Approver()
    {
        var access = Evaluate(new AppraisalReviewViewer
        {
            Setting = Setting(s => s.CanViewPeerRatings = true),
            IsApprover = true
        });

        access.SeePeerRatings.ShouldBeTrue();
        access.SeeNonPeerRatings.ShouldBeFalse();
        access.ShowReviewers.ShouldBeTrue();

        // A view flag with no section to view in would be a switch that does nothing.
        access.ShowRatings.ShouldBeTrue();
    }

    [Fact]
    public void Peers_May_Be_Added_By_The_Approver_Or_The_Employee_But_Not_By_A_Peer()
    {
        var setting = Setting(s => s.CanAddPeer = true);

        Evaluate(new AppraisalReviewViewer { Setting = setting, IsApprover = true }).CanAddPeer.ShouldBeTrue();
        Evaluate(new AppraisalReviewViewer { Setting = setting, IsAppraisee = true, IsReviewer = true }).CanAddPeer.ShouldBeTrue();
        Evaluate(new AppraisalReviewViewer { Setting = setting, IsReviewer = true, ReviewerIsPeer = true }).CanAddPeer.ShouldBeFalse();
    }

    [Fact]
    public void A_Reviewer_Should_See_Ratings_Even_With_Can_Peer_Add_Ratings_Off()
    {
        // XAF let a plain reviewer rate; ESS needed "Can Peer Add Ratings". The permissive one is kept so an
        // auto-added manager can always rate and an auto-submitting step can always complete.
        Evaluate(new AppraisalReviewViewer { Setting = Setting(), IsReviewer = true, ReviewerIsPeer = true })
            .ShowRatings.ShouldBeTrue();
    }
}
