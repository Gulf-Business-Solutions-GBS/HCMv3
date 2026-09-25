using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Authorization.Users;
using HCMv3.Authorization.Users;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Workflow;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Dto;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The request-detail endpoint: what an approver is allowed to read, and what they actually get back.
///
/// <para>
/// This endpoint exists because an approver could not see the fields of the thing they were approving. The
/// inbox carries the one-line summary a handler froze at submit and nothing else, so for an exit permit the
/// approver got a sentence that happened to mention the permit number and the exit date while the start date
/// the requester had entered was simply unreachable.
/// </para>
///
/// <para>
/// Two groups of tests, and the first matters more. A workflow request id is a small integer, so an endpoint
/// that returned a request's contents to any signed-in user would let anyone walk the range and read every
/// resignation, salary advance and medical claim in the tenant. The authorization tests assert the whole
/// rule: the requester yes, an approver yes, an approver who has already acted yes, an unrelated employee
/// no.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowRequestDetail_Tests : AppTestBase
{
    private readonly IWorkflowActivityAppService _activityService;

    public WorkflowRequestDetail_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _activityService = Resolve<IWorkflowActivityAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- fixtures

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
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
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

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    /// <summary>
    /// A signed-in user with no permissions at all, linked to their own employee record.
    ///
    /// <para>
    /// Needed because every other test here runs as the tenant admin, who holds
    /// <c>Pages.Workflow.WorkflowRequests</c> and would therefore be admitted by the admin branch no matter
    /// what the rest of the rule said. A test for "an outsider is refused" run as an admin proves nothing.
    /// </para>
    /// </summary>
    private int LoginAsUnrelatedEmployee(string code)
    {
        var userId = 0L;

        UsingDbContext(context =>
        {
            var unique = Guid.NewGuid().ToString("N").Substring(0, 8);
            var user = new User
            {
                TenantId = TenantId,
                UserName = "wf" + unique,
                Name = "WF",
                Surname = "Tester",
                EmailAddress = "wf" + unique + "@testdomain.com",
                IsEmailConfirmed = true,
                Password = "AM4OLBpptxBYmM79lGOX9egzZk3vIQU3d/gFCJzaBjAPXzYIK3tQ2N7X4fcrHtElTw=="
            };
            user.SetNormalizedNames();

            context.Users.Add(user);
            context.SaveChanges();
            userId = user.Id;
        });

        AbpSession.UserId = userId;

        return CreateEmployee(code, userId);
    }

    private int CreateRequest(int requesterEmployeeId, string requestType = "EmployeeAbsence", int? targetEntityId = null, int step = 1)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = requesterEmployeeId,
                RequestType = requestType,
                TargetEntityId = targetEntityId,
                RequestNumber = "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = RequestStatus.Open,
                WorkflowStep = step,
                SummaryEn = "Annual Leave, 5 day(s)",
                SummaryAr = "إجازة سنوية، 5 أيام"
            }).Entity;
        });

        return request.Id;
    }

    private void AddApprover(int requestId, int approverEmployeeId, bool isApproved = false)
    {
        UsingDbContext(context =>
        {
            // The row must carry its request's step. Read rather than assumed: these helpers serve
            // requests on different steps, and a row whose step does not match its request is invisible
            // to every step-scoped query -- which looks, from the test, like a broken service.
            var approverStep = context.Set<WorkflowRequest>().Single(r => r.Id == requestId).WorkflowStep;

            context.Set<WorkFlowApprover>().Add(new WorkFlowApprover
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ApproverId = approverEmployeeId,
                IsApproved = isApproved,

                WorkflowStep = approverStep
            });
        });
    }

    private void AddLog(int requestId, int actorEmployeeId, RequestActionType actionType, string comment = null)
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
                ActionDateTime = new DateTime(2026, 9, 1, 9, 0, 0),
                WorkflowStep = 1
            });
        });
    }

    private int CreateExitPermit(int employeeId, string permitNumber, DateTime startDate, DateTime exitDate, string comments)
    {
        EmployeeExitPermit permit = null;
        UsingDbContext(context =>
        {
            permit = context.Set<EmployeeExitPermit>().Add(new EmployeeExitPermit
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                ExternalRequestNumber = permitNumber,
                TravelReason = TravelReason.PlannedLeave,
                StartDate = startDate,
                ExitDate = exitDate,
                Comments = comments,
                WorkflowStatus = RequestStatus.Open
            }).Entity;
        });

        return permit.Id;
    }

    private Task<WorkflowRequestDetailDto> GetDetail(int requestId)
    {
        return _activityService.GetRequestDetail(new GetWorkflowRequestDetailInput { WorkflowRequestId = requestId });
    }

    // ---------------------------------------------------------------- authorization

    [Fact]
    public async Task Should_Let_The_Requester_Read_Their_Own_Request()
    {
        var meId = CreateCurrentUserEmployee("DET100ME");
        var requestId = CreateRequest(meId);

        var detail = await GetDetail(requestId);

        detail.WorkflowRequestId.ShouldBe(requestId);
        detail.RequesterEmployeeId.ShouldBe(meId);

        // A requester is not an approver of their own request, whatever else is true of them.
        detail.IsCurrentApprover.ShouldBeFalse();
        detail.CanApprove.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Let_The_Approver_The_Request_Is_Waiting_On_Read_It()
    {
        var requesterId = CreateEmployee("DET200REQUESTER");
        var meId = CreateCurrentUserEmployee("DET200APPROVER");
        var requestId = CreateRequest(requesterId);
        AddApprover(requestId, meId);

        var detail = await GetDetail(requestId);

        detail.WorkflowRequestId.ShouldBe(requestId);
        detail.IsCurrentApprover.ShouldBeTrue();
        detail.CanApprove.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Refuse_An_Employee_With_No_Part_In_The_Request()
    {
        // The whole reason this endpoint needs a test. Request ids are small consecutive integers, so
        // without this check any signed-in employee could read every request in the tenant by counting.
        var requesterId = CreateEmployee("DET300REQUESTER");
        var requestId = CreateRequest(requesterId);

        LoginAsUnrelatedEmployee("DET300OUTSIDER");

        await Assert.ThrowsAsync<AbpAuthorizationException>(() => GetDetail(requestId));
    }

    [Fact]
    public async Task Should_Let_An_Approver_Who_Has_Already_Acted_Read_The_Request()
    {
        // The defect this fixes is in the engine, not here. AssignApproversForStep DELETES every
        // WorkFlowApprover row for a request before inserting the next step's, so the manager who approved
        // at step 1 holds no approver row the instant the request reaches step 2 -- and used to lose sight
        // of the request entirely, AbpAuthorizationException, including the conversation they had taken part
        // in. RequestLog is never deleted, so it is the durable record of who acted.
        var requesterId = CreateEmployee("DET400REQUESTER");
        var meId = CreateCurrentUserEmployee("DET400STEP1APPROVER");
        var nextApproverId = CreateEmployee("DET400STEP2APPROVER");

        var requestId = CreateRequest(requesterId, step: 2);

        // Exactly the state the engine leaves behind: only the CURRENT step's approver has a row.
        AddApprover(requestId, nextApproverId);
        AddLog(requestId, meId, RequestActionType.Approved, "fine by me");

        var detail = await GetDetail(requestId);

        detail.WorkflowRequestId.ShouldBe(requestId);

        // They can read it, but the request is no longer theirs to decide.
        detail.IsCurrentApprover.ShouldBeFalse();
        detail.CanApprove.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Treat_The_Requester_As_An_Approver_Because_They_Submitted()
    {
        // Created is an action too, and its actor is the requester. Counting it as "has acted" would make
        // every requester a past approver of their own request and hand them the internal thread.
        var meId = CreateCurrentUserEmployee("DET500ME");
        var approverId = CreateEmployee("DET500APPROVER");
        var requestId = CreateRequest(meId);
        AddApprover(requestId, approverId);
        AddLog(requestId, meId, RequestActionType.Created, "please approve");

        var feed = await _activityService.GetActivityFeed(new GetWorkflowActivityFeedInput { WorkflowRequestId = requestId });

        feed.CanCommentInternally.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Offer_A_Decision_On_A_Request_That_Is_No_Longer_Open()
    {
        // An approver row can outlive the decision -- the engine stamps IsApproved rather than deleting the
        // row on the final step. Offering the buttons again would let the same person approve twice.
        var requesterId = CreateEmployee("DET600REQUESTER");
        var meId = CreateCurrentUserEmployee("DET600APPROVER");
        var requestId = CreateRequest(requesterId);
        AddApprover(requestId, meId);

        UsingDbContext(context =>
        {
            var request = context.Set<WorkflowRequest>().Single(r => r.Id == requestId);
            request.RequestStatus = RequestStatus.Approved;
        });

        var detail = await GetDetail(requestId);

        detail.IsCurrentApprover.ShouldBeFalse();
        detail.CanApprove.ShouldBeFalse();
        detail.CanReject.ShouldBeFalse();
        detail.CanSendBack.ShouldBeFalse();
    }

    // ---------------------------------------------------------------- the exit permit projection

    [Fact]
    public async Task Should_Project_Every_Field_Of_An_Exit_Permit()
    {
        // The type that exposed the gap, and the assertion that closes it. The StartDate below is the field
        // that was previously unreachable: it appears nowhere in the summary sentence, so no amount of
        // reading the inbox could have produced it.
        var requesterId = CreateEmployee("DET700REQUESTER");
        var meId = CreateCurrentUserEmployee("DET700APPROVER");

        var permitId = CreateExitPermit(
            requesterId,
            "KW-88119",
            new DateTime(2026, 10, 1),
            new DateTime(2026, 10, 5),
            "Family wedding in Cairo");

        var requestId = CreateRequest(requesterId, ExitPermitWorkflowTransactionHandler.ExitPermitRequestType, permitId);
        AddApprover(requestId, meId);

        var detail = await GetDetail(requestId);

        detail.IsDefaultProjection.ShouldBeFalse();

        string ValueOf(string name) => detail.Fields.Single(f => f.Name == name).Value;

        ValueOf(nameof(EmployeeExitPermit.ExternalRequestNumber)).ShouldBe("KW-88119");
        ValueOf(nameof(EmployeeExitPermit.Comments)).ShouldBe("Family wedding in Cairo");

        // ISO, not formatted: the client renders it in the viewer's calendar, and the kind says how.
        ValueOf(nameof(EmployeeExitPermit.StartDate)).ShouldBe("2026-10-01");
        ValueOf(nameof(EmployeeExitPermit.ExitDate)).ShouldBe("2026-10-05");

        detail.Fields.Single(f => f.Name == nameof(EmployeeExitPermit.StartDate)).Kind
            .ShouldBe(WorkflowDetailFieldKind.Date);

        // The reason arrives as words rather than as the enum name, because it is displayed, and because a
        // generic viewer that has never heard of an exit permit would otherwise print "PlannedLeave".
        // Asserted against the English text of TravelReason_PlannedLeave, which is the culture these tests
        // run in.
        ValueOf(nameof(EmployeeExitPermit.TravelReason)).ShouldBe("Planned Leave");

        // The engine's own identifier is part of every projection, type-specific or not.
        ValueOf(WorkflowDetailFieldNames.RequestNumber).ShouldNotBeNullOrWhiteSpace();

        // Labels are localized for reading; names are stable for binding. Both are populated on every field,
        // because the typed Angular form binds on the second and the generic list renders the first.
        detail.Fields.ShouldAllBe(f => !string.IsNullOrWhiteSpace(f.Name));
        detail.Fields.ShouldAllBe(f => !string.IsNullOrWhiteSpace(f.Label));
    }

    [Fact]
    public async Task Should_Carry_The_Requesters_Submission_Comment_From_The_Transaction_Row()
    {
        // What makes the requester's own words the first thing in the activity feed instead of a bare
        // "Created" line with the comment stored somewhere the feed cannot see.
        var requesterId = CreateEmployee("DET800REQUESTER");
        var meId = CreateCurrentUserEmployee("DET800APPROVER");

        var permitId = CreateExitPermit(
            requesterId,
            "KW-4455",
            new DateTime(2026, 11, 2),
            new DateTime(2026, 11, 4),
            "Need to travel on short notice");

        var requestId = CreateRequest(requesterId, ExitPermitWorkflowTransactionHandler.ExitPermitRequestType, permitId);
        AddApprover(requestId, meId);

        var detail = await GetDetail(requestId);

        detail.SubmissionComment.ShouldBe("Need to travel on short notice");
    }

    [Fact]
    public async Task Should_Identify_The_Employee_The_Request_Is_About()
    {
        var requesterId = CreateEmployee("DET850REQUESTER");
        var meId = CreateCurrentUserEmployee("DET850APPROVER");
        var requestId = CreateRequest(requesterId);
        AddApprover(requestId, meId);

        var detail = await GetDetail(requestId);

        // An approver deciding on a permit needs to know whose it is, not just its id.
        detail.RequesterEmployeeId.ShouldBe(requesterId);
        detail.RequesterNameEn.ShouldBe("DET850REQUESTER T");
        detail.RequesterCode.ShouldBe("DET850REQUESTER");
    }

    // ---------------------------------------------------------------- the default projection

    [Fact]
    public async Task Should_Fall_Back_To_The_Engines_Own_Fields_For_A_Type_With_No_Projection()
    {
        // The floor. A handler that has not overridden BuildDetailAsync must still describe its request --
        // an approver looking at a blank card would be worse off than with the one-line summary they had,
        // which is why the base class supplies this rather than being abstract.
        var requesterId = CreateEmployee("DET900REQUESTER");
        var meId = CreateCurrentUserEmployee("DET900APPROVER");
        var requestId = CreateRequest(requesterId);
        AddApprover(requestId, meId);

        var detail = await GetDetail(requestId);

        detail.IsDefaultProjection.ShouldBeTrue();
        detail.Fields.Count.ShouldBe(3);

        detail.Fields.ShouldContain(f => f.Name == WorkflowDetailFieldNames.RequestNumber);
        detail.Fields.ShouldContain(f => f.Name == WorkflowDetailFieldNames.SubmittedOn);

        detail.Fields.Single(f => f.Name == WorkflowDetailFieldNames.Summary)
            .Value.ShouldBe("Annual Leave, 5 day(s)");

        // Flagged, so the screen can say "this type has no detailed view yet" rather than implying a leave
        // request genuinely has three fields.
        detail.SubmissionComment.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Still_Describe_A_Request_Whose_Type_Has_No_Handler_At_All()
    {
        // A type can be catalogued and routed with no handler -- the resolver returning null is a supported
        // answer, not an error. The endpoint must not fall over on one.
        var requesterId = CreateEmployee("DET950REQUESTER");
        var meId = CreateCurrentUserEmployee("DET950APPROVER");
        var requestId = CreateRequest(requesterId, "ATypeNobodyHasWrittenAHandlerFor");
        AddApprover(requestId, meId);

        var detail = await GetDetail(requestId);

        detail.IsDefaultProjection.ShouldBeTrue();
        detail.Fields.ShouldContain(f => f.Name == WorkflowDetailFieldNames.RequestNumber);
    }
}
