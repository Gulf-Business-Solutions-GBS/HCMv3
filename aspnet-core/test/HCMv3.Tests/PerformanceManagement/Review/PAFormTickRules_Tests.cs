using System.Collections.Generic;
using System.Linq;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Review;
using HCMv3.SystemSettings;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement.Review;

/// <summary>
/// The goal form's twelve-box rules (<see cref="PAFormTickRules"/>), ported from legacy
/// <c>PAFormController.ViewController1_CustomApplyAppearance</c>. Pure, so no database.
/// </summary>
// ReSharper disable once InconsistentNaming
public class PAFormTickRules_Tests
{
    private static readonly ActivityType[] AllStages =
    {
        ActivityType.Plan, ActivityType.Quarter1Review, ActivityType.MidYearReview, ActivityType.Quarter3Review, ActivityType.AnnualReview
    };

    private static PAForm Form(params PAFormBox[] ticked)
    {
        var form = new PAForm();
        foreach (var box in ticked)
        {
            typeof(PAForm).GetProperty(box.ToString())!.SetValue(form, true);
        }

        return form;
    }

    private static PAFormTickResult Evaluate(
        PAForm form,
        bool isAppraisee,
        IEnumerable<ActivityType> timeline = null,
        IEnumerable<ActivityType> openWindows = null,
        bool peerReview = false,
        bool isPeerNominee = false,
        bool peerSubmitted = false)
    {
        var windows = (openWindows ?? AllStages).ToHashSet();

        return PAFormTickRules.Evaluate(new PAFormTickContext
        {
            Values = PAFormBoxValues.From(form),
            HasActingEmployee = true,
            IsAppraisee = isAppraisee,
            TimelineActivityTypes = (timeline ?? AllStages).ToHashSet(),
            IsInWindow = activity => windows.Contains(activity),
            EnablePAPeerReview = peerReview,
            IsPeerNominee = isPeerNominee,
            PeerReviewSubmitted = peerSubmitted
        });
    }

    private static PAFormBox[] Enabled(PAFormTickResult result) =>
        result.Boxes.Where(b => b.Enabled).Select(b => b.Box).ToArray();

    [Fact]
    public void The_Plan_Stage_Should_Come_First_With_Each_Side_On_Its_Own_Box()
    {
        Enabled(Evaluate(Form(), isAppraisee: true)).ShouldBe(new[] { PAFormBox.PlanSubmitByAppraisee });
        Enabled(Evaluate(Form(), isAppraisee: false)).ShouldBe(new[] { PAFormBox.PlanSubmitByAppraiser });
    }

    [Fact]
    public void A_Stage_Should_Stay_Open_Until_Both_Sides_Have_Ticked()
    {
        // The appraisee has ticked; the appraiser has not. The plan is still the open stage, so the appraisee
        // may still untick their own box and nothing later is reachable.
        var result = Evaluate(Form(PAFormBox.PlanSubmitByAppraisee), isAppraisee: true);

        result.Phase.ShouldBe(PAFormPhase.Plan);
        Enabled(result).ShouldBe(new[] { PAFormBox.PlanSubmitByAppraisee });
    }

    [Fact]
    public void The_Next_Stage_Should_Open_Only_Once_The_Previous_One_Is_Complete()
    {
        var result = Evaluate(Form(PAFormBox.PlanSubmitByAppraisee, PAFormBox.PlanSubmitByAppraiser), isAppraisee: false);

        result.Phase.ShouldBe(PAFormPhase.Quarter1);
        Enabled(result).ShouldBe(new[] { PAFormBox.Quarter1SubmitByAppraiser });
    }

    [Fact]
    public void Nothing_Should_Be_Tickable_Outside_The_Stage_Window()
    {
        Enabled(Evaluate(Form(), isAppraisee: true, openWindows: new[] { ActivityType.AnnualReview })).ShouldBeEmpty();
    }

    [Fact]
    public void Stages_The_Timeline_Does_Not_Define_Should_Be_Hidden_And_Skipped()
    {
        // Plan and End-of-Year only: the three middle stages vanish and Plan leads straight to End-of-Year.
        var result = Evaluate(
            Form(PAFormBox.PlanSubmitByAppraisee, PAFormBox.PlanSubmitByAppraiser),
            isAppraisee: true,
            timeline: new[] { ActivityType.Plan, ActivityType.AnnualReview });

        result[PAFormBox.Quarter1SubmitByAppraisee].Visible.ShouldBeFalse();
        result[PAFormBox.MRSubmitByAppraiser].Visible.ShouldBeFalse();
        result[PAFormBox.Quarter3SubmitByAppraisee].Visible.ShouldBeFalse();
        result.Phase.ShouldBe(PAFormPhase.EndOfYear);
        Enabled(result).ShouldBe(new[] { PAFormBox.ERSubmitByAppraisee });
    }

    [Fact]
    public void Plan_And_End_Of_Year_Should_Always_Be_Visible()
    {
        var result = Evaluate(Form(), isAppraisee: true, timeline: new ActivityType[0]);

        result[PAFormBox.PlanSubmitByAppraisee].Visible.ShouldBeTrue();
        result[PAFormBox.ERSubmitByAppraiser].Visible.ShouldBeTrue();
        result[PAFormBox.AppraiseeSignedOff].Visible.ShouldBeTrue();
    }

    /// <summary>
    /// LEGACY ODDITY, kept by the owner's decision: both sign-off boxes are the appraiser's. The legacy code
    /// comment says the employee ticks "Appraisee Signed Off"; the code gives it to the manager.
    /// </summary>
    [Fact]
    public void Both_Sign_Off_Boxes_Should_Belong_To_The_Appraiser()
    {
        var everythingDone = Form(
            PAFormBox.PlanSubmitByAppraisee, PAFormBox.PlanSubmitByAppraiser,
            PAFormBox.Quarter1SubmitByAppraisee, PAFormBox.Quarter1SubmitByAppraiser,
            PAFormBox.MRSubmitByAppraisee, PAFormBox.MRSubmitByAppraiser,
            PAFormBox.Quarter3SubmitByAppraisee, PAFormBox.Quarter3SubmitByAppraiser,
            PAFormBox.ERSubmitByAppraisee, PAFormBox.ERSubmitByAppraiser);

        Enabled(Evaluate(everythingDone, isAppraisee: false))
            .ShouldBe(new[] { PAFormBox.AppraiserSignedOff, PAFormBox.AppraiseeSignedOff });

        Enabled(Evaluate(everythingDone, isAppraisee: true)).ShouldBeEmpty();
    }

    [Fact]
    public void Sign_Off_Should_Need_No_Time_Window()
    {
        var everythingDone = Form(
            PAFormBox.PlanSubmitByAppraisee, PAFormBox.PlanSubmitByAppraiser,
            PAFormBox.ERSubmitByAppraisee, PAFormBox.ERSubmitByAppraiser);

        Enabled(Evaluate(everythingDone, isAppraisee: false, timeline: new[] { ActivityType.Plan, ActivityType.AnnualReview }, openWindows: new ActivityType[0]))
            .ShouldContain(PAFormBox.AppraiserSignedOff);
    }

    [Fact]
    public void An_Account_With_No_Employee_Should_Be_Unrestricted()
    {
        // Legacy applied everything inside "if (loggedinEmp != null)"; the admin, by standing rule, has none.
        var result = PAFormTickRules.Evaluate(new PAFormTickContext { HasActingEmployee = false });

        result.Unrestricted.ShouldBeTrue();
        result.Boxes.ShouldAllBe(b => b.Enabled && b.Visible);
    }

    [Fact]
    public void A_New_Form_Should_Offer_No_Box_And_Hide_The_Optional_Stages()
    {
        var result = PAFormTickRules.Evaluate(new PAFormTickContext { HasActingEmployee = true, IsNew = true });

        result.Boxes.ShouldAllBe(b => !b.Enabled);
        result[PAFormBox.Quarter1SubmitByAppraiser].Visible.ShouldBeFalse();
        result[PAFormBox.MRSubmitByAppraisee].Visible.ShouldBeFalse();
        result[PAFormBox.PlanSubmitByAppraisee].Visible.ShouldBeTrue();
    }

    [Fact]
    public void A_Peer_Should_See_None_Of_The_Twelve_Boxes()
    {
        var result = Evaluate(Form(), isAppraisee: false, peerReview: true, isPeerNominee: true);

        result.Boxes.ShouldAllBe(b => !b.Visible && !b.Enabled);
    }

    [Fact]
    public void A_Peer_Should_Tick_Submitted_Only_During_End_Of_Year()
    {
        var planOpen = Evaluate(Form(), isAppraisee: false, peerReview: true, isPeerNominee: true);
        planOpen.CanTickPeerSubmitted.ShouldBeFalse();

        var endOfYear = Evaluate(
            Form(PAFormBox.PlanSubmitByAppraisee, PAFormBox.PlanSubmitByAppraiser),
            isAppraisee: false,
            timeline: new[] { ActivityType.Plan, ActivityType.AnnualReview },
            peerReview: true,
            isPeerNominee: true);

        endOfYear.ShowPeerSubmitted.ShouldBeTrue();
        endOfYear.CanTickPeerSubmitted.ShouldBeTrue();
    }

    [Fact]
    public void A_Submitted_Peer_Review_Should_Hide_The_Box()
    {
        var result = Evaluate(
            Form(PAFormBox.PlanSubmitByAppraisee, PAFormBox.PlanSubmitByAppraiser),
            isAppraisee: false,
            timeline: new[] { ActivityType.Plan, ActivityType.AnnualReview },
            peerReview: true,
            isPeerNominee: true,
            peerSubmitted: true);

        result.ShowPeerSubmitted.ShouldBeFalse();
        result.CanTickPeerSubmitted.ShouldBeFalse();
    }

    [Theory]
    [InlineData(AppraisalCycle.Quarterly, 0)]
    [InlineData(AppraisalCycle.HalfYearly, 4)]
    [InlineData(AppraisalCycle.Annually, 6)]
    public void The_List_Should_Hide_Quarter_Boxes_By_Appraisal_Cycle(AppraisalCycle cycle, int hidden)
    {
        var boxes = PAFormTickRules.HiddenForCycle(cycle);

        boxes.Count.ShouldBe(hidden);
        boxes.ShouldNotContain(PAFormBox.PlanSubmitByAppraiser);
        boxes.ShouldNotContain(PAFormBox.ERSubmitByAppraisee);

        if (cycle == AppraisalCycle.HalfYearly)
        {
            boxes.ShouldNotContain(PAFormBox.MRSubmitByAppraisee);
        }
    }
}
