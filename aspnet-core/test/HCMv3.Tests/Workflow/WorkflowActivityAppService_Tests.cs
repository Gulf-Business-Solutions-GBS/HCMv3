using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers the activity feed and the comment thread.
///
/// The visibility rules carry the most weight here. An internal comment reaching the employee who raised
/// the request is not a cosmetic bug -- approvers post them believing they are private, and the whole
/// feature is worthless if that is not reliable. So the tests assert on what the SERVICE returns, not on
/// what a UI might choose to hide.
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowActivityAppService_Tests : AppTestBase
{
    private readonly IWorkflowActivityAppService _activityService;

    public WorkflowActivityAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _activityService = Resolve<IWorkflowActivityAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    private int CreateEmployee(string code, long? userId = null)
    {
        Nationality nationality = null;
        UsingDbContext(context =>
        {
            nationality = context.Set<Nationality>().Add(new Nationality
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "N",
                NameAr = "N"
            }).Entity;
        });

        Employee employee = null;
        UsingDbContext(context =>
        {
            employee = context.Set<Employee>().Add(new Employee
            {
                TenantId = TenantId,
                Code = code,
                FirstNameEn = code,
                LastNameEn = "Test",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " Test",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>Links the signed-in user to an employee so the service resolves them as that person.</summary>
    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    private int CreateRequest(int requesterEmployeeId)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = requesterEmployeeId,
                RequestType = "EmployeeAbsence",
                RequestNumber = "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = RequestStatus.Open,
                SummaryEn = "Annual Leave, 5 day(s)"
            }).Entity;
        });

        return request.Id;
    }

    private void AddApprover(int requestId, int approverEmployeeId)
    {
        UsingDbContext(context =>
        {
            context.Set<WorkFlowApprover>().Add(new WorkFlowApprover
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ApproverId = approverEmployeeId,
                IsApproved = false
            });
        });
    }

    private void AddLog(int requestId, int actorEmployeeId, RequestActionType actionType, string comment, DateTime at)
    {
        UsingDbContext(context =>
        {
            context.Set<RequestLog>().Add(new RequestLog
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ActionById = actorEmployeeId,
                ActionType = actionType,
                RequestActionComment = comment,
                ActionDateTime = at,
                WorkflowStep = 1
            });
        });
    }

    private int AddCommentDirect(int requestId, int authorEmployeeId, string body, bool isInternal)
    {
        WorkflowComment comment = null;
        UsingDbContext(context =>
        {
            comment = context.Set<WorkflowComment>().Add(new WorkflowComment
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                AuthorEmployeeId = authorEmployeeId,
                Body = body,
                IsInternal = isInternal
            }).Entity;
        });

        return comment.Id;
    }

    [Fact]
    public async Task An_Admin_Should_Be_Able_To_View_A_Request_They_Have_No_Part_In()
    {
        // HR must be able to open any request to answer "what happened to mine?". The signed-in user here is
        // the tenant admin, who holds Pages.Workflow.WorkflowRequests, so access comes through the admin
        // route rather than through being requester or approver.
        CreateCurrentUserEmployee("ACT100ME");
        var strangerId = CreateEmployee("ACT100STRANGER");
        var requestId = CreateRequest(strangerId);

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        feed.WorkflowRequestId.ShouldBe(requestId);

        // An admin who is neither requester nor approver still counts as an approver-side viewer.
        feed.CanCommentInternally.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Merge_Actions_And_Comments_In_Chronological_Order()
    {
        var meId = CreateCurrentUserEmployee("ACT200ME");
        var requestId = CreateRequest(meId);

        AddLog(requestId, meId, RequestActionType.Created, "submitted", new DateTime(2026, 3, 1, 9, 0, 0));
        AddCommentDirect(requestId, meId, "a question", isInternal: false);
        AddLog(requestId, meId, RequestActionType.Approved, "looks fine", new DateTime(2026, 3, 3, 9, 0, 0));

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        feed.Activities.Count.ShouldBe(3);
        feed.Activities.Select(a => a.OccurredAt).ShouldBe(feed.Activities.Select(a => a.OccurredAt).OrderBy(d => d));

        // Both sources are present in one stream -- the reader should not have to know which table a row
        // came from, which is what legacy's separate grid and text blob forced on them.
        feed.Activities.ShouldContain(a => a.Kind == WorkflowActivityKind.Action);
        feed.Activities.ShouldContain(a => a.Kind == WorkflowActivityKind.Comment);
    }

    [Fact]
    public async Task Should_Carry_The_Action_Comment_And_Actor_On_An_Action_Entry()
    {
        var meId = CreateCurrentUserEmployee("ACT300ME");
        var requestId = CreateRequest(meId);

        AddLog(requestId, meId, RequestActionType.Approved, "approved with a note", new DateTime(2026, 3, 2, 10, 0, 0));

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        var action = feed.Activities.Single(a => a.Kind == WorkflowActivityKind.Action);
        action.ActionType.ShouldBe(RequestActionType.Approved);
        action.Body.ShouldBe("approved with a note");
        action.ActorNameEn.ShouldBe("ACT300ME Test");

        // Audit rows are immutable.
        action.CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Hide_Internal_Comments_From_The_Requester()
    {
        // The rule the whole feature rests on.
        //
        // Note the signed-in user is the tenant admin AND the requester here. That combination is the
        // sharpest form of the test: being the requester disqualifies you from internal comments even when
        // every other route -- admin permission included -- would have granted them.
        var meId = CreateCurrentUserEmployee("ACT400ME");
        var approverId = CreateEmployee("ACT400APPROVER");
        var requestId = CreateRequest(meId);
        AddApprover(requestId, approverId);

        AddCommentDirect(requestId, approverId, "visible to everyone", isInternal: false);
        AddCommentDirect(requestId, approverId, "approvers only", isInternal: true);

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        feed.Activities.ShouldContain(a => a.Body == "visible to everyone");

        // The assertion that matters: it must not reach the requester at all, not merely be hidden by the UI.
        feed.Activities.ShouldNotContain(a => a.Body == "approvers only");
        feed.Activities.ShouldNotContain(a => a.IsInternal);
        feed.CanCommentInternally.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Never_Show_Internal_Comments_To_The_Requester_Even_When_They_Are_Also_An_Approver()
    {
        // Some configurations let a person approve their own request. "Approvers only" has to mean the same
        // thing on every request, so being the requester disqualifies regardless.
        var meId = CreateCurrentUserEmployee("ACT500ME");
        var requestId = CreateRequest(meId);
        AddApprover(requestId, meId);

        AddCommentDirect(requestId, meId, "public one", isInternal: false);
        AddCommentDirect(requestId, meId, "private one", isInternal: true);

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        feed.Activities.ShouldContain(a => a.Body == "public one");
        feed.Activities.ShouldNotContain(a => a.Body == "private one");
        feed.CanCommentInternally.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Show_Internal_Comments_To_An_Approver_Who_Is_Not_The_Requester()
    {
        var requesterId = CreateEmployee("ACT600REQUESTER");
        var meId = CreateCurrentUserEmployee("ACT600APPROVER");
        var requestId = CreateRequest(requesterId);
        AddApprover(requestId, meId);

        AddCommentDirect(requestId, meId, "approvers only", isInternal: true);

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        feed.Activities.ShouldContain(a => a.Body == "approvers only" && a.IsInternal);
        feed.CanCommentInternally.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_An_Internal_Comment_From_The_Requester()
    {
        // Refused rather than silently downgraded: the author believes they are speaking privately.
        var meId = CreateCurrentUserEmployee("ACT700ME");
        var requestId = CreateRequest(meId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _activityService.AddComment(new AddWorkflowCommentInput
            {
                WorkflowRequestId = requestId,
                Body = "trying to be private",
                IsInternal = true
            }));
    }

    [Fact]
    public async Task Should_Refuse_A_Comment_From_A_User_With_No_Employee_Record()
    {
        // Reachable in production and missed by every other test here, all of which link the signed-in user
        // to an employee: an admin has view access through their permission alone, with no employee link at
        // all. A comment is authored BY an employee, so they cannot post one -- but they must be told that
        // rather than getting a 500 from a null dereference, which is what happened before this guard.
        var strangerId = CreateEmployee("ACT750STRANGER");
        var requestId = CreateRequest(strangerId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _activityService.AddComment(new AddWorkflowCommentInput
            {
                WorkflowRequestId = requestId,
                Body = "I have no employee record"
            }));
    }

    [Fact]
    public async Task Should_Add_A_Comment_And_Return_It()
    {
        var meId = CreateCurrentUserEmployee("ACT800ME");
        var requestId = CreateRequest(meId);

        var created = await _activityService.AddComment(new AddWorkflowCommentInput
        {
            WorkflowRequestId = requestId,
            Body = "when will this be approved?"
        });

        created.Kind.ShouldBe(WorkflowActivityKind.Comment);
        created.Body.ShouldBe("when will this be approved?");
        created.ActorEmployeeId.ShouldBe(meId);
        created.CanEdit.ShouldBeTrue();

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });
        feed.Activities.ShouldContain(a => a.Id == created.Id && a.Kind == WorkflowActivityKind.Comment);
    }

    [Fact]
    public async Task Should_Refuse_A_Public_Reply_Under_An_Internal_Comment()
    {
        // A public reply would leak the private thread's existence and context to the requester.
        var requesterId = CreateEmployee("ACT900REQUESTER");
        var meId = CreateCurrentUserEmployee("ACT900APPROVER");
        var requestId = CreateRequest(requesterId);
        AddApprover(requestId, meId);

        var internalId = AddCommentDirect(requestId, meId, "private parent", isInternal: true);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _activityService.AddComment(new AddWorkflowCommentInput
            {
                WorkflowRequestId = requestId,
                Body = "public reply",
                IsInternal = false,
                ParentCommentId = internalId
            }));
    }

    [Fact]
    public async Task Should_Mark_An_Edited_Comment()
    {
        var meId = CreateCurrentUserEmployee("ACT1000ME");
        var requestId = CreateRequest(meId);

        var created = await _activityService.AddComment(new AddWorkflowCommentInput
        {
            WorkflowRequestId = requestId,
            Body = "first version"
        });

        var edited = await _activityService.EditComment(new EditWorkflowCommentInput
        {
            Id = created.Id,
            Body = "second version"
        });

        edited.Body.ShouldBe("second version");

        // Marked, so an edit cannot quietly rewrite what an approver replied to.
        edited.IsEdited.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_To_Edit_Someone_Elses_Comment()
    {
        var meId = CreateCurrentUserEmployee("ACT1100ME");
        var otherId = CreateEmployee("ACT1100OTHER");
        var requestId = CreateRequest(meId);

        var othersComment = AddCommentDirect(requestId, otherId, "not mine", isInternal: false);

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _activityService.EditComment(new EditWorkflowCommentInput { Id = othersComment, Body = "hijacked" }));
    }

    [Fact]
    public async Task Should_Delete_Own_Comment()
    {
        var meId = CreateCurrentUserEmployee("ACT1200ME");
        var requestId = CreateRequest(meId);

        var created = await _activityService.AddComment(new AddWorkflowCommentInput
        {
            WorkflowRequestId = requestId,
            Body = "never mind"
        });

        await _activityService.DeleteComment(new EntityDto(created.Id));

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });
        feed.Activities.ShouldNotContain(a => a.Kind == WorkflowActivityKind.Comment && a.Id == created.Id);
    }

    [Fact]
    public async Task Should_Refuse_To_Delete_A_Comment_That_Has_Replies()
    {
        // Deleting the parent would orphan the replies and strip the context they were written against.
        var meId = CreateCurrentUserEmployee("ACT1300ME");
        var requestId = CreateRequest(meId);

        var parent = await _activityService.AddComment(new AddWorkflowCommentInput
        {
            WorkflowRequestId = requestId,
            Body = "parent"
        });

        await _activityService.AddComment(new AddWorkflowCommentInput
        {
            WorkflowRequestId = requestId,
            Body = "reply",
            ParentCommentId = parent.Id
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _activityService.DeleteComment(new EntityDto(parent.Id)));
    }

    [Fact]
    public async Task Should_Include_Feedback_Requests_As_System_Entries()
    {
        // WorkflowRemark is an ad-hoc feedback assignment, so the thread explains why someone who is not an
        // approver appears in it.
        var meId = CreateCurrentUserEmployee("ACT1400ME");
        var reviewerId = CreateEmployee("ACT1400REVIEWER");
        var requestId = CreateRequest(meId);

        UsingDbContext(context =>
        {
            context.Set<WorkflowRemark>().Add(new WorkflowRemark
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                CCReceiverId = reviewerId
            });
        });

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        var entry = feed.Activities.Single(a => a.Kind == WorkflowActivityKind.FeedbackRequested);
        entry.ActorEmployeeId.ShouldBe(reviewerId);
        entry.ActorNameEn.ShouldBe("ACT1400REVIEWER Test");
    }

    [Fact]
    public async Task Should_Not_Offer_A_Photo_Url_When_The_Employee_Has_None()
    {
        var meId = CreateCurrentUserEmployee("ACT1500ME");
        var requestId = CreateRequest(meId);
        AddCommentDirect(requestId, meId, "hello", isInternal: false);

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        // Null rather than a URL that would 404 on every render.
        feed.Activities.Single(a => a.Kind == WorkflowActivityKind.Comment).ActorPhotoUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Point_At_The_Photo_Endpoint_When_The_Employee_Has_One()
    {
        var meId = CreateCurrentUserEmployee("ACT1600ME");
        var requestId = CreateRequest(meId);
        AddCommentDirect(requestId, meId, "hello", isInternal: false);

        UsingDbContext(context =>
        {
            context.Set<Employee>().Single(e => e.Id == meId).Photo = new byte[] { 1, 2, 3 };
        });

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        // A URL, never the bytes -- a twenty-comment thread would otherwise carry twenty photo blobs.
        feed.Activities
            .Single(a => a.Kind == WorkflowActivityKind.Comment)
            .ActorPhotoUrl.ShouldBe("/api/employee-photo/" + meId);
    }
}
