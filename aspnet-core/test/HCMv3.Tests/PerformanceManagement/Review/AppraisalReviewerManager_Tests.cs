using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PerformanceManagement.Review;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement.Review;

/// <summary>
/// The reviewers legacy added when approvers were assigned to a step (<c>WorkFlowServices.OperateApproverInfo</c>)
/// and the reviewer-save rules (<c>PAEmployeeReviewer.PerformValidation</c>, <c>FillReview</c>).
/// </summary>
// ReSharper disable once InconsistentNaming
public class AppraisalReviewerManager_Tests : AppraisalReviewTestBase
{
    private AppraisalStepSetting StepWith(Action<AppraisalStepSetting> configure, params int[] relationshipTypeIds)
    {
        var master = CreateRequestMaster(1, 2);
        var setting = CreateStepSetting(master, 2, configure, relationshipTypeIds);
        setting.Relationships = Context.AppraisalStepSettingRelationships.Where(r => r.AppraisalStepSettingId == setting.Id).ToList();
        return setting;
    }

    [Fact]
    public async Task Should_Add_Nobody_When_Every_Flag_Is_Off()
    {
        // Every flag defaults to off: a tenant that has not configured its steps gets no reviewers added.
        var appraisal = CreateAppraisal(CreateEmployee("E1"));
        var approver = CreateEmployee("A1");

        await CreateReviewerManager().AddStepReviewersAsync(appraisal, StepWith(null), new[] { approver });

        ReviewersOf(appraisal).ShouldBeEmpty();
    }

    [Fact]
    public async Task Self_Rate_Should_Add_The_Employee_As_A_Non_Peer_Added_By_The_Approver()
    {
        var employee = CreateEmployee("E1");
        var appraisal = CreateAppraisal(employee);
        var approver = CreateEmployee("A1");

        await CreateReviewerManager().AddStepReviewersAsync(appraisal, StepWith(s => s.CanEmployeeSelfRate = true), new[] { approver });

        var reviewer = ReviewersOf(appraisal).Single();
        reviewer.EmployeeId.ShouldBe(employee);
        reviewer.IsPeer.ShouldBeFalse();
        reviewer.PeerAddedById.ShouldBe(approver);
    }

    [Fact]
    public async Task Can_Add_Ratings_Should_Add_Every_Approver_As_A_Non_Peer()
    {
        var appraisal = CreateAppraisal(CreateEmployee("E1"));
        var first = CreateEmployee("A1");
        var second = CreateEmployee("A2");

        await CreateReviewerManager().AddStepReviewersAsync(appraisal, StepWith(s => s.CanAddRatings = true), new[] { first, second });

        var reviewers = ReviewersOf(appraisal);
        reviewers.Select(r => r.EmployeeId).ShouldBe(new[] { first, second });
        reviewers.ShouldAllBe(r => !r.IsPeer);
    }

    [Fact]
    public async Task Auto_Add_Manager_Should_Add_The_Current_Manager_As_A_Peer()
    {
        // Legacy marked the manager a PEER. Odd-looking, kept: it is what makes them removable and what the
        // peer average counts.
        var employee = CreateEmployee("E1");
        var manager = CreateEmployee("M1");
        SetManager(employee, manager);
        var appraisal = CreateAppraisal(employee);

        await CreateReviewerManager().AddStepReviewersAsync(appraisal, StepWith(s => s.AutoAddManager = true), new[] { CreateEmployee("A1") });

        var reviewer = ReviewersOf(appraisal).Single();
        reviewer.EmployeeId.ShouldBe(manager);
        reviewer.IsPeer.ShouldBeTrue();
    }

    [Fact]
    public async Task Auto_Add_Relationship_Should_Add_Only_Current_Relations_Of_The_Configured_Types()
    {
        var employee = CreateEmployee("E1");
        var dottedLine = CreateRelationshipType("Dotted Line Manager");
        var mentor = CreateRelationshipType("Mentor");

        var current = CreateEmployee("R1");
        var expired = CreateEmployee("R2");
        var otherType = CreateEmployee("R3");
        Relate(employee, current, dottedLine);
        Relate(employee, expired, dottedLine, DateTime.Today.AddYears(-2), DateTime.Today.AddDays(-1));
        Relate(employee, otherType, mentor);

        var appraisal = CreateAppraisal(employee);

        await CreateReviewerManager().AddStepReviewersAsync(
            appraisal,
            StepWith(s => s.AutoAddRelationShip = true, dottedLine),
            new[] { CreateEmployee("A1") });

        var reviewer = ReviewersOf(appraisal).Single();
        reviewer.EmployeeId.ShouldBe(current);
        reviewer.IsPeer.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Never_Add_The_Same_Person_Twice()
    {
        // The manager is also the approver: added once, by the first rule that reaches them (the approver
        // rule, as a non-peer), exactly as legacy's "already on the list?" check behaved.
        var employee = CreateEmployee("E1");
        var manager = CreateEmployee("M1");
        SetManager(employee, manager);
        var appraisal = CreateAppraisal(employee);

        var setting = StepWith(s =>
        {
            s.CanEmployeeSelfRate = true;
            s.CanAddRatings = true;
            s.AutoAddManager = true;
        });

        await CreateReviewerManager().AddStepReviewersAsync(appraisal, setting, new[] { manager });

        var reviewers = ReviewersOf(appraisal);
        reviewers.Count.ShouldBe(2);
        reviewers.Single(r => r.EmployeeId == manager).IsPeer.ShouldBeFalse();
    }

    [Fact]
    public async Task A_Second_Assignment_Of_The_Same_Step_Should_Add_Nobody_New()
    {
        // A send-back and resubmission assigns the step again. Reviewers already there stay, and stay single.
        var employee = CreateEmployee("E1");
        var appraisal = CreateAppraisal(employee);
        var approver = CreateEmployee("A1");
        var setting = StepWith(s =>
        {
            s.CanEmployeeSelfRate = true;
            s.CanAddRatings = true;
        });

        var manager = CreateReviewerManager();
        await manager.AddStepReviewersAsync(appraisal, setting, new[] { approver });
        var added = await manager.AddStepReviewersAsync(appraisal, setting, new[] { approver });

        added.ShouldBeEmpty();
        ReviewersOf(appraisal).Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_New_Reviewer_Should_Get_An_Empty_Line_For_Every_Kpi()
    {
        var appraisal = CreateAppraisal(CreateEmployee("E1"));
        AddKpiLine(appraisal, CreateKpi("K1"));
        AddKpiLine(appraisal, CreateKpi("K2"));

        var reviewer = await CreateReviewerManager().AddReviewerAsync(appraisal, CreateEmployee("P1"), isPeer: true, peerAddedById: null, setting: null);

        Context.PAEmployeeRatings.Count(r => r.PAEmployeeReviewerId == reviewer.Id).ShouldBe(2);
    }

    [Fact]
    public async Task The_Form_Type_Limit_Should_Stop_An_Automatic_Addition_As_It_Did_In_Legacy()
    {
        // Legacy saved the automatic reviewer through the ordinary validation, so overfilling the form failed
        // the assignment outright. Kept: a step must not open missing somebody who should have scored.
        var employee = CreateEmployee("E1");
        var appraisal = CreateAppraisal(employee, CreateFormType(maxReviewerLimit: 1));
        AddReviewer(appraisal, CreateEmployee("P1"), isPeer: true);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateReviewerManager().AddStepReviewersAsync(appraisal, StepWith(s => s.CanEmployeeSelfRate = true), new[] { CreateEmployee("A1") }));
    }

    [Fact]
    public async Task Adding_Somebody_Already_Reviewing_Should_Be_Refused()
    {
        var appraisal = CreateAppraisal(CreateEmployee("E1"));
        var peer = CreateEmployee("P1");
        AddReviewer(appraisal, peer, isPeer: true);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateReviewerManager().AddReviewerAsync(appraisal, peer, isPeer: true, peerAddedById: null, setting: null));
    }

    [Fact]
    public async Task The_Employee_Should_Not_Nominate_More_Peers_Than_The_Step_Allows()
    {
        var employee = CreateEmployee("E1");
        var appraisal = CreateAppraisal(employee);
        var setting = StepWith(s =>
        {
            s.CanAddPeer = true;
            s.AllowedPeerCount = 1;
        });
        var manager = CreateReviewerManager();

        await manager.AddReviewerAsync(appraisal, CreateEmployee("P1"), isPeer: true, peerAddedById: employee, setting);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            manager.AddReviewerAsync(appraisal, CreateEmployee("P2"), isPeer: true, peerAddedById: employee, setting));
    }

    [Fact]
    public async Task Peers_Added_By_Somebody_Else_Should_Not_Use_Up_The_Employees_Allowance()
    {
        var employee = CreateEmployee("E1");
        var appraisal = CreateAppraisal(employee);
        var approver = CreateEmployee("A1");
        AddReviewer(appraisal, CreateEmployee("P1"), isPeer: true, addedBy: approver);
        AddReviewer(appraisal, CreateEmployee("P2"), isPeer: true, addedBy: approver);

        var setting = StepWith(s =>
        {
            s.CanAddPeer = true;
            s.AllowedPeerCount = 1;
        });

        await CreateReviewerManager().AddReviewerAsync(appraisal, CreateEmployee("P3"), isPeer: true, peerAddedById: employee, setting);

        ReviewersOf(appraisal).Count.ShouldBe(3);
    }
}
