using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.PerformanceManagement;
using HCMv3.PerformanceManagement.Review;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PerformanceManagement.Review;

/// <summary>
/// Self-service appraisals (<see cref="MyAppraisalAppService"/>): the three lists, who may open what, and
/// the review actions -- Save Review, Submit Review with auto-approval on the last fill, Copy KPI, Add Peer.
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyAppraisalAppService_Tests : AppraisalReviewTestBase
{
    private MyAppraisalAppService CreateAppService()
    {
        return WireApp(new MyAppraisalAppService(
            ReviewFakeRepository.For<PAEmployee>(Context),
            ReviewFakeRepository.For<PAEmployeeDetail>(Context),
            ReviewFakeRepository.For<PAEmployeeReviewer>(Context),
            ReviewFakeRepository.For<PAEmployeeRating>(Context),
            ReviewFakeRepository.For<PARating>(Context),
            ReviewFakeRepository.For<KpiByJobTitle>(Context),
            ReviewFakeRepository.For<KpiByJobTitleDetail>(Context),
            ReviewFakeRepository.For<KpiByOrgUnit>(Context),
            ReviewFakeRepository.For<KpiByOrgUnitDetail>(Context),
            ReviewFakeRepository.For<Employee>(Context),
            ReviewFakeRepository.For<EmployeeManager>(Context),
            ReviewFakeRepository.For<EmployeeRelationship>(Context),
            ReviewFakeRepository.For<AppraisalStepSetting>(Context),
            ReviewFakeRepository.For<AppraisalStepSettingRelationship>(Context),
            ReviewFakeRepository.For<WorkflowRequest>(Context),
            ReviewFakeRepository.For<WorkFlowApprover>(Context),
            CreateScopeProvider(),
            CreateStepSettingResolver(),
            CreateReviewerManager(),
            CreateScoreCalculator(),
            CreateSettingsAccessor(),
            ApprovalService));
    }

    /// <summary>An appraisal at step 2 of a two-step chain, with one approver and one KPI line.</summary>
    private (PAEmployee Appraisal, WorkflowRequest Request, int Employee, int Approver, PAEmployeeDetail Kpi) Scenario(
        Action<AppraisalStepSetting> configure = null)
    {
        var employee = CreateEmployee("E1");
        var approver = CreateEmployee("A1");
        var appraisal = CreateAppraisal(employee);
        var kpi = AddKpiLine(appraisal, CreateKpi("K1"));
        var master = CreateRequestMaster(1, 2);
        CreateStepSetting(master, 2, configure);
        var request = Route(appraisal, step: 2);
        AddApprover(request, 2, approver);

        return (appraisal, request, employee, approver, kpi);
    }

    private PAEmployeeRating LineOf(PAEmployeeReviewer reviewer) =>
        Context.PAEmployeeRatings.AsNoTracking().Single(r => r.PAEmployeeReviewerId == reviewer.Id);

    private static SaveMyAppraisalReviewInput ReviewInput(PAEmployee appraisal, PAEmployeeRating line, int? ratingId, string comment = null) => new()
    {
        PAEmployeeId = appraisal.Id,
        Lines = new List<SaveMyAppraisalRatingLineInput> { new() { Id = line.Id, RatingId = ratingId, Comments = comment } }
    };

    // ---------------------------------------------------------------- lists

    [Fact]
    public async Task Mine_Should_List_Only_Appraisals_About_Me()
    {
        var s = Scenario();
        CreateAppraisal(CreateEmployee("OTHER"));
        Me = s.Employee;

        var result = await CreateAppService().GetMyAppraisals(new GetMyAppraisalsInput { Scope = MyAppraisalListScope.Mine, MaxResultCount = 10 });

        result.Items.Select(i => i.Id).ShouldBe(new[] { s.Appraisal.Id });
        result.Items[0].RequestNumber.ShouldBe(s.Request.RequestNumber);
    }

    [Fact]
    public async Task To_Review_Should_List_What_I_Review_Or_Approve_Now()
    {
        var s = Scenario();
        var peer = CreateEmployee("P1");
        AddReviewer(s.Appraisal, peer, isPeer: true);

        Me = peer;
        (await CreateAppService().GetMyAppraisals(new GetMyAppraisalsInput { Scope = MyAppraisalListScope.ToReview, MaxResultCount = 10 }))
            .Items.Single().Id.ShouldBe(s.Appraisal.Id);

        Me = s.Approver;
        (await CreateAppService().GetMyAppraisals(new GetMyAppraisalsInput { Scope = MyAppraisalListScope.ToReview, MaxResultCount = 10 }))
            .Items.Single().Id.ShouldBe(s.Appraisal.Id);
    }

    [Fact]
    public async Task To_Review_Should_Not_List_A_Step_I_Approved_Before()
    {
        var s = Scenario();
        var earlier = CreateEmployee("A0");
        AddApprover(s.Request, 1, earlier, isApproved: true);
        Me = earlier;

        (await CreateAppService().GetMyAppraisals(new GetMyAppraisalsInput { Scope = MyAppraisalListScope.ToReview, MaxResultCount = 10 }))
            .Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Team_Should_List_My_Direct_Reports_Appraisals()
    {
        var s = Scenario();
        var manager = CreateEmployee("M1");
        Me = manager;
        DirectReports.Add(s.Employee);

        (await CreateAppService().GetMyAppraisals(new GetMyAppraisalsInput { Scope = MyAppraisalListScope.Team, MaxResultCount = 10 }))
            .Items.Single().Id.ShouldBe(s.Appraisal.Id);
    }

    // ---------------------------------------------------------------- opening

    [Fact]
    public async Task Somebody_The_Appraisal_Is_Not_About_Should_Not_Open_It()
    {
        var s = Scenario();
        Me = CreateEmployee("OUTSIDER");

        await Should.ThrowAsync<AbpAuthorizationException>(() => CreateAppService().GetAppraisalReview(new EntityDto(s.Appraisal.Id)));
    }

    [Fact]
    public async Task A_Peer_Should_See_Only_Their_Own_Lines()
    {
        var s = Scenario(cfg => cfg.CanViewPeerRatings = true);
        var peer = CreateEmployee("P1");
        var otherPeer = CreateEmployee("P2");
        AddReviewer(s.Appraisal, peer, isPeer: true);
        AddReviewer(s.Appraisal, otherPeer, isPeer: true);
        Me = peer;

        var review = await CreateAppService().GetAppraisalReview(new EntityDto(s.Appraisal.Id));

        review.RatingLines.ShouldAllBe(l => l.ReviewerEmployeeId == peer);
        review.RatingLines.Single().IsOwn.ShouldBeTrue();
        review.Reviewers.ShouldBeEmpty();
        review.Access.CanSubmitReview.ShouldBeTrue();
    }

    [Fact]
    public async Task The_Approver_Should_See_Peer_Lines_Where_The_Step_Allows()
    {
        var s = Scenario(cfg => cfg.CanViewPeerRatings = true);
        AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        Me = s.Approver;

        var review = await CreateAppService().GetAppraisalReview(new EntityDto(s.Appraisal.Id));

        review.RatingLines.Count.ShouldBe(1);
        review.RatingLines[0].IsOwn.ShouldBeFalse();
        review.Reviewers.Single().ReviewerType.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task The_Rating_Options_Should_Show_What_Is_Left_Of_Each_Limit()
    {
        var s = Scenario();
        var good = CreateRating("3", maxLimit: 2);
        var peer = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        var line = Context.PAEmployeeRatings.Single(r => r.PAEmployeeReviewerId == peer.Id);
        line.RatingId = good.Id;
        Context.SaveChanges();
        Me = peer.EmployeeId;

        var review = await CreateAppService().GetAppraisalReview(new EntityDto(s.Appraisal.Id));

        review.RatingOptions.Single(o => o.Id == good.Id).RemainingLimit.ShouldBe(1);
    }

    // ---------------------------------------------------------------- save review

    [Fact]
    public async Task Save_Review_Should_Write_My_Line_And_Rescore()
    {
        var s = Scenario();
        var rating = CreateRating("4");
        var peer = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        Me = peer.EmployeeId;

        await CreateAppService().SaveReview(ReviewInput(s.Appraisal, LineOf(peer), rating.Id, "Good work"));

        var line = LineOf(peer);
        line.RatingId.ShouldBe(rating.Id);
        line.Comments.ShouldBe("Good work");
        Context.PAEmployeeReviewers.AsNoTracking().Single(r => r.Id == peer.Id).AverageScore.ShouldBe(4);
        Context.PAEmployeeReviewers.AsNoTracking().Single(r => r.Id == peer.Id).ReviewSubmitted.ShouldBeFalse();
    }

    [Fact]
    public async Task Save_Review_Should_Refuse_Somebody_Elses_Line()
    {
        var s = Scenario();
        var rating = CreateRating("4");
        var peer = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        var other = AddReviewer(s.Appraisal, CreateEmployee("P2"), isPeer: true);
        Me = peer.EmployeeId;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateAppService().SaveReview(ReviewInput(s.Appraisal, LineOf(other), rating.Id)));
    }

    [Fact]
    public async Task Save_Review_Should_Refuse_A_Rating_Over_Its_Limit()
    {
        var s = Scenario();
        AddKpiLine(s.Appraisal, CreateKpi("K2"));
        var rating = CreateRating("5", maxLimit: 1);
        var peer = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        Me = peer.EmployeeId;
        var lines = Context.PAEmployeeRatings.AsNoTracking().Where(r => r.PAEmployeeReviewerId == peer.Id).ToList();

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().SaveReview(new SaveMyAppraisalReviewInput
        {
            PAEmployeeId = s.Appraisal.Id,
            Lines = lines.Select(l => new SaveMyAppraisalRatingLineInput { Id = l.Id, RatingId = rating.Id }).ToList()
        }));
    }

    /// <summary>
    /// LEGACY ODDITY, kept: a rating whose "Max Limit" is zero cannot be chosen in self-service at all --
    /// legacy's balance was zero and it refused the selection.
    /// </summary>
    [Fact]
    public async Task A_Rating_With_A_Limit_Of_Zero_Should_Not_Be_Choosable()
    {
        var s = Scenario();
        var rating = CreateRating("3", maxLimit: 0);
        var peer = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        Me = peer.EmployeeId;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateAppService().SaveReview(ReviewInput(s.Appraisal, LineOf(peer), rating.Id)));
    }

    // ---------------------------------------------------------------- submit review

    [Fact]
    public async Task Submit_Review_Should_Need_Every_Line_Rated()
    {
        var s = Scenario();
        var peer = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        Me = peer.EmployeeId;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateAppService().SubmitReview(ReviewInput(s.Appraisal, LineOf(peer), ratingId: null)));
    }

    [Fact]
    public async Task Submit_Review_Should_Need_A_Comment_Where_The_Rating_Asks_For_One()
    {
        var s = Scenario();
        var rating = CreateRating("1", commentRequired: true);
        var peer = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        Me = peer.EmployeeId;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateAppService().SubmitReview(ReviewInput(s.Appraisal, LineOf(peer), rating.Id, comment: " ")));
    }

    [Fact]
    public async Task Submit_Review_Should_Lock_My_Part()
    {
        var s = Scenario();
        var rating = CreateRating("4");
        var peer = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        Me = peer.EmployeeId;

        var result = await CreateAppService().SubmitReview(ReviewInput(s.Appraisal, LineOf(peer), rating.Id));

        result.StepApproved.ShouldBeFalse();
        Context.PAEmployeeReviewers.AsNoTracking().Single(r => r.Id == peer.Id).ReviewSubmitted.ShouldBeTrue();

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateAppService().SaveReview(ReviewInput(s.Appraisal, LineOf(peer), rating.Id)));
    }

    [Fact]
    public async Task The_Last_Submission_On_An_Auto_Submitting_Step_Should_Approve_As_The_First_Approver()
    {
        var s = Scenario(cfg => cfg.AutoSubmitOnLastFill = true);
        var rating = CreateRating("4");
        var done = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true, submitted: true);
        var last = AddReviewer(s.Appraisal, CreateEmployee("P2"), isPeer: true);
        Me = last.EmployeeId;

        var result = await CreateAppService().SubmitReview(ReviewInput(s.Appraisal, LineOf(last), rating.Id));

        result.StepApproved.ShouldBeTrue();
        await ApprovalService.Received(1).ApproveAsync(s.Request.Id, s.Approver, Arg.Any<string>());
        done.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_Submission_With_Others_Still_Pending_Should_Approve_Nothing()
    {
        var s = Scenario(cfg => cfg.AutoSubmitOnLastFill = true);
        var rating = CreateRating("4");
        AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        var mine = AddReviewer(s.Appraisal, CreateEmployee("P2"), isPeer: true);
        Me = mine.EmployeeId;

        (await CreateAppService().SubmitReview(ReviewInput(s.Appraisal, LineOf(mine), rating.Id))).StepApproved.ShouldBeFalse();

        await ApprovalService.DidNotReceive().ApproveAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Submitting_Twice_Should_Be_Refused_So_The_Step_Cannot_Be_Approved_Twice()
    {
        var s = Scenario(cfg => cfg.AutoSubmitOnLastFill = true);
        var rating = CreateRating("4");
        var only = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        Me = only.EmployeeId;
        var service = CreateAppService();

        await service.SubmitReview(ReviewInput(s.Appraisal, LineOf(only), rating.Id));

        await Should.ThrowAsync<UserFriendlyException>(() => service.SubmitReview(ReviewInput(s.Appraisal, LineOf(only), rating.Id)));
        await ApprovalService.Received(1).ApproveAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string>());
    }

    [Fact]
    public async Task An_Approver_Who_Rates_On_An_Ordinary_Step_Should_Not_Submit_A_Review()
    {
        var s = Scenario(cfg => cfg.CanAddRatings = true);
        var rating = CreateRating("4");
        var approverAsReviewer = AddReviewer(s.Appraisal, s.Approver, isPeer: false);
        Me = s.Approver;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateAppService().SubmitReview(ReviewInput(s.Appraisal, LineOf(approverAsReviewer), rating.Id)));
    }

    // ---------------------------------------------------------------- copy KPI

    private int KpiSetForJobTitle(params (int KpiInfoId, double Weight)[] lines)
    {
        var jobTitle = new HCMv3.OrganizationManagement.JobTitle { TenantId = TenantId, Code = "JT", NameEn = "Analyst", NameAr = "محلل" };
        Context.JobTitles.Add(jobTitle);
        Context.SaveChanges();

        var header = new KpiByJobTitle { TenantId = TenantId, JobTitleId = jobTitle.Id, StartDate = DateTime.Today.AddYears(-1), EndDate = DateTime.Today.AddYears(1) };
        Context.KpiByJobTitles.Add(header);
        Context.SaveChanges();

        foreach (var (kpiInfoId, weight) in lines)
        {
            Context.KpiByJobTitleDetails.Add(new KpiByJobTitleDetail { TenantId = TenantId, KpiByJobTitleId = header.Id, KpiInfoId = kpiInfoId, Weight = weight, Target = "JT" });
        }

        Context.SaveChanges();
        return header.Id;
    }

    [Fact]
    public async Task Copy_Kpi_Should_Add_Only_The_Kpis_The_Appraisal_Lacks()
    {
        var s = Scenario(cfg => cfg.CanAddKpi = true);
        var newKpi = CreateKpi("K9");
        var header = KpiSetForJobTitle((s.Kpi.KpiInfoId, 10), (newKpi, 40));
        Me = s.Approver;

        await CreateAppService().CopyKpi(new CopyMyAppraisalKpiInput { PAEmployeeId = s.Appraisal.Id, KpiFrom = KpiFrom.JobTitle, KpiByJobTitleId = header });

        var details = Context.PAEmployeeDetails.AsNoTracking().Where(d => d.PAEmployeeId == s.Appraisal.Id).ToList();
        details.Count.ShouldBe(2);
        details.Single(d => d.KpiInfoId == s.Kpi.KpiInfoId).Weight.ShouldBe(50);
        details.Single(d => d.KpiInfoId == newKpi).Weight.ShouldBe(40);
    }

    [Fact]
    public async Task Copy_Kpi_Should_Give_Existing_Reviewers_A_Line_For_Each_New_Kpi()
    {
        var s = Scenario(cfg => cfg.CanAddKpi = true);
        var peer = AddReviewer(s.Appraisal, CreateEmployee("P1"), isPeer: true);
        var header = KpiSetForJobTitle((CreateKpi("K9"), 40));
        Me = s.Approver;

        await CreateAppService().CopyKpi(new CopyMyAppraisalKpiInput { PAEmployeeId = s.Appraisal.Id, KpiFrom = KpiFrom.JobTitle, KpiByJobTitleId = header });

        Context.PAEmployeeRatings.Count(r => r.PAEmployeeReviewerId == peer.Id).ShouldBe(2);
    }

    [Fact]
    public async Task Copy_Kpi_On_A_Rating_Step_Should_Put_The_Approver_On_The_Reviewer_List()
    {
        var s = Scenario(cfg =>
        {
            cfg.CanAddKpi = true;
            cfg.CanAddRatings = true;
        });
        var header = KpiSetForJobTitle((CreateKpi("K9"), 40));
        Me = s.Approver;

        await CreateAppService().CopyKpi(new CopyMyAppraisalKpiInput { PAEmployeeId = s.Appraisal.Id, KpiFrom = KpiFrom.JobTitle, KpiByJobTitleId = header });

        var reviewer = ReviewersOf(s.Appraisal).Single();
        reviewer.EmployeeId.ShouldBe(s.Approver);
        reviewer.IsPeer.ShouldBeFalse();
        Context.PAEmployeeRatings.Count(r => r.PAEmployeeReviewerId == reviewer.Id).ShouldBe(2);
    }

    [Fact]
    public async Task Copy_Kpi_From_An_Employee_Should_Take_Their_Appraisal_In_The_Same_Cycle()
    {
        var s = Scenario(cfg => cfg.CanAddKpi = true);
        var colleague = CreateEmployee("C1");
        var theirs = CreateAppraisal(colleague, s.Appraisal.FormTypeId, s.Appraisal.PACycleId);
        var theirKpi = CreateKpi("K7");
        AddKpiLine(theirs, theirKpi, weight: 30);
        Me = s.Approver;

        await CreateAppService().CopyKpi(new CopyMyAppraisalKpiInput { PAEmployeeId = s.Appraisal.Id, KpiFrom = KpiFrom.Employee, SourceEmployeeId = colleague });

        Context.PAEmployeeDetails.AsNoTracking().Single(d => d.PAEmployeeId == s.Appraisal.Id && d.KpiInfoId == theirKpi).Weight.ShouldBe(30);
    }

    [Fact]
    public async Task Copy_Kpi_From_An_Employee_With_No_Appraisal_In_The_Cycle_Should_Be_Refused()
    {
        var s = Scenario(cfg => cfg.CanAddKpi = true);
        Me = s.Approver;

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CopyKpi(new CopyMyAppraisalKpiInput
        {
            PAEmployeeId = s.Appraisal.Id,
            KpiFrom = KpiFrom.Employee,
            SourceEmployeeId = CreateEmployee("NOBODY")
        }));
    }

    [Fact]
    public async Task Copy_Kpi_Should_Be_Refused_Where_The_Step_Does_Not_Allow_It()
    {
        var s = Scenario();
        Me = s.Approver;

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CopyKpi(new CopyMyAppraisalKpiInput
        {
            PAEmployeeId = s.Appraisal.Id,
            KpiFrom = KpiFrom.JobTitle,
            KpiByJobTitleId = KpiSetForJobTitle((CreateKpi("K9"), 40))
        }));
    }

    [Fact]
    public async Task Copy_Kpi_With_Peer_Review_On_Should_Respect_The_Reviewer_Limit()
    {
        // Legacy: with peer review on, the copy is refused if one more reviewer would break the form's limit.
        var employee = CreateEmployee("E1");
        var approver = CreateEmployee("A1");
        var appraisal = CreateAppraisal(employee, CreateFormType(maxReviewerLimit: 1));
        var master = CreateRequestMaster(1, 2);
        CreateStepSetting(master, 2, cfg => cfg.CanAddKpi = true);
        var request = Route(appraisal, 2);
        AddApprover(request, 2, approver);
        AddReviewer(appraisal, CreateEmployee("P1"), isPeer: true);
        AppraisalSettings.EnablePAPeerReview = true;
        Me = approver;

        await Should.ThrowAsync<UserFriendlyException>(() => CreateAppService().CopyKpi(new CopyMyAppraisalKpiInput
        {
            PAEmployeeId = appraisal.Id,
            KpiFrom = KpiFrom.JobTitle,
            KpiByJobTitleId = KpiSetForJobTitle((CreateKpi("K9"), 40))
        }));
    }

    // ---------------------------------------------------------------- add peer

    [Fact]
    public async Task The_Employee_Should_Add_A_Peer_Within_Their_Allowance()
    {
        var s = Scenario(cfg =>
        {
            cfg.CanAddPeer = true;
            cfg.AllowedPeerCount = 1;
        });
        AppraisalSettings.SetPeerMaxLimit = 5;
        var colleague = CreateEmployee("C1");
        Me = s.Employee;
        var service = CreateAppService();

        await service.AddPeer(new AddMyAppraisalPeerInput { PAEmployeeId = s.Appraisal.Id, PeerEmployeeId = colleague });

        var reviewer = ReviewersOf(s.Appraisal).Single();
        reviewer.EmployeeId.ShouldBe(colleague);
        reviewer.IsPeer.ShouldBeTrue();
        reviewer.PeerAddedById.ShouldBe(s.Employee);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.AddPeer(new AddMyAppraisalPeerInput { PAEmployeeId = s.Appraisal.Id, PeerEmployeeId = CreateEmployee("C2") }));
    }

    [Fact]
    public async Task The_Employee_Should_Not_Pick_Their_Own_Manager_As_A_Peer()
    {
        // The manager arrives on their own (Auto Add Manager); legacy left them out of the employee's list.
        var s = Scenario(cfg =>
        {
            cfg.CanAddPeer = true;
            cfg.AllowedPeerCount = 3;
        });
        AppraisalSettings.SetPeerMaxLimit = 5;
        var manager = CreateEmployee("M1");
        SetManager(s.Employee, manager);
        Me = s.Employee;
        var service = CreateAppService();

        (await service.GetPeerCandidates(new GetMyAppraisalPeerCandidatesInput { PAEmployeeId = s.Appraisal.Id }))
            .ShouldNotContain(p => p.Id == manager || p.Id == s.Employee);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            service.AddPeer(new AddMyAppraisalPeerInput { PAEmployeeId = s.Appraisal.Id, PeerEmployeeId = manager }));
    }

    [Fact]
    public async Task Add_Peer_Should_Need_The_Peer_Limit_Configured()
    {
        // Legacy ESS: "Peer reviewer limit is not configured correctly."
        var s = Scenario(cfg => cfg.CanAddPeer = true);
        AppraisalSettings.SetPeerMaxLimit = 0;
        Me = s.Approver;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateAppService().AddPeer(new AddMyAppraisalPeerInput { PAEmployeeId = s.Appraisal.Id, PeerEmployeeId = CreateEmployee("C1") }));
    }

    [Fact]
    public async Task Add_Peer_Should_Stop_At_The_Peer_Limit_Counting_Every_Reviewer()
    {
        var s = Scenario(cfg => cfg.CanAddPeer = true);
        AppraisalSettings.SetPeerMaxLimit = 1;
        AddReviewer(s.Appraisal, s.Employee, isPeer: false);
        Me = s.Approver;

        await Should.ThrowAsync<UserFriendlyException>(() =>
            CreateAppService().AddPeer(new AddMyAppraisalPeerInput { PAEmployeeId = s.Appraisal.Id, PeerEmployeeId = CreateEmployee("C1") }));
    }
}
