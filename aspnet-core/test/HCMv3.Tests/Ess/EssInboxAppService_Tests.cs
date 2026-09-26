using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Covers the approver's inbox.
///
/// <para>
/// The behaviour worth protecting hardest is what the inbox does NOT show: a request already approved by
/// this person, one waiting on somebody else, or one that is no longer open. An inbox that over-reports is
/// worse than useless -- approvers stop trusting the count and start ignoring it.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class EssInboxAppService_Tests : AppTestBase
{
    private readonly IEssInboxAppService _inbox;

    public EssInboxAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _inbox = Resolve<IEssInboxAppService>();
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
                LastNameEn = "T",
                FirstNameAr = "ت",
                LastNameAr = "ت",
                FullNameEn = code + " T",
                FullNameAr = "ت ت",
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,

                // Active, explicitly. The column defaults to Inactive, which made every employee this
                // helper built invisible to any query that filters on status -- a trap for the next test
                // that touches one, and the reason the feedback-candidates test could find nobody.
                Status = EmployeeStatus.Active,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    /// <summary>Builds a request already sitting at step 1 with the given approver, as submit would leave it.</summary>
    private int CreatePendingRequest(
        int requesterId,
        int approverId,
        string requestType = "EmployeeAbsence",
        string summary = "Annual Leave, 3 day(s)",
        bool approverAlreadyActed = false,
        RequestStatus status = RequestStatus.Open,
        int workflowStep = 1)
    {
        WorkflowRequest request = null;
        UsingDbContext(context =>
        {
            request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = requesterId,
                RequestType = requestType,
                RequestNumber = "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                RequestStatus = status,
                WorkflowStep = workflowStep,
                SummaryEn = summary,
                SummaryAr = summary
            }).Entity;
        });

        var requestId = request.Id;

        UsingDbContext(context =>
        {
            context.Set<WorkFlowApprover>().Add(new WorkFlowApprover
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ApproverId = approverId,
                IsApproved = approverAlreadyActed,

                // The step this row belongs to. Without it the column defaults to 0, which matches no
                // request past step 0, and every step-scoped query skips the row.
                WorkflowStep = workflowStep
            });
        });

        return requestId;
    }

    /// <summary>A one-step chain wired to the leave catalog row, so step capabilities resolve.</summary>
    private void ConfigureLeaveChain(bool canReject = true, bool canSendBack = true, int steps = 1, string typeCode = "EmployeeAbsence")
    {
        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = "R" + Guid.NewGuid().ToString("N").Substring(0, 5),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = typeCode == "EmployeeAbsence" ? "LEAVE-STD" : typeCode.ToUpperInvariant() + "-STD",
                NameEn = "Standard Leave",
                NameAr = "Standard Leave",
                WorkflowSteps = steps
            }).Entity;

            context.SaveChanges();

            for (var step = 1; step <= steps; step++)
            {
                context.Set<RequestDetail>().Add(new RequestDetail
                {
                    TenantId = TenantId,
                    RequestMasterId = master.Id,
                    Step = step,
                    WorkflowRoleId = role.Id,
                    ApproverPerson = WorkflowApproverPersonType.Person,
                    ApprovalRequire = ApprovalRequire.Single,
                    CanReject = canReject,
                    CanSendBack = canSendBack
                });
            }

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == typeCode);
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });
    }

    private static GetMyInboxInput Query(string requestType = null) =>
        new GetMyInboxInput { RequestType = requestType, MaxResultCount = 50, SkipCount = 0 };

    [Fact]
    public async Task Should_Refuse_The_Inbox_For_A_User_With_No_Employee_Record()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() => _inbox.GetMyInbox(Query()));
    }

    [Fact]
    public async Task The_Badge_Count_Should_Be_Zero_For_An_Unlinked_User_Rather_Than_An_Error()
    {
        // The badge is decoration on every page; throwing there would break the shell for an admin, who is
        // never linked to an employee. The screens themselves explain the missing link.
        var count = await _inbox.GetMyPendingCount();

        count.ShouldBe(0);
    }

    [Fact]
    public async Task Should_List_A_Request_Waiting_On_Me()
    {
        var meId = CreateCurrentUserEmployee("INB100ME");
        var requesterId = CreateEmployee("INB100REQ");
        var requestId = CreatePendingRequest(requesterId, meId);

        var result = await _inbox.GetMyInbox(Query());

        var item = result.Items.Single(i => i.Id == requestId);
        item.SummaryEn.ShouldBe("Annual Leave, 3 day(s)");
        item.RequesterNameEn.ShouldBe("INB100REQ T");
        item.RequestType.ShouldBe("EmployeeAbsence");

        // Read from the catalog, so the row can be labelled without touching a transaction table.
        item.RequestTypeNameEn.ShouldBe("Leave Request");
    }

    [Fact]
    public async Task Should_Not_List_A_Request_I_Have_Already_Approved()
    {
        var meId = CreateCurrentUserEmployee("INB200ME");
        var requesterId = CreateEmployee("INB200REQ");
        CreatePendingRequest(requesterId, meId, approverAlreadyActed: true);

        var result = await _inbox.GetMyInbox(Query());

        result.TotalCount.ShouldBe(0);
        (await _inbox.GetMyPendingCount()).ShouldBe(0);
    }

    [Fact]
    public async Task Should_Not_List_A_Request_Waiting_On_Someone_Else()
    {
        var meId = CreateCurrentUserEmployee("INB300ME");
        var otherApproverId = CreateEmployee("INB300OTHER");
        var requesterId = CreateEmployee("INB300REQ");
        CreatePendingRequest(requesterId, otherApproverId);

        var result = await _inbox.GetMyInbox(Query());

        result.TotalCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(RequestStatus.Approved)]
    [InlineData(RequestStatus.Rejected)]
    [InlineData(RequestStatus.Draft)]
    [InlineData(RequestStatus.Closed)]
    public async Task Should_Only_List_Requests_That_Are_Still_Open(RequestStatus status)
    {
        // A decided request must leave the inbox even if its approver row is still unapproved -- otherwise
        // the count never goes down and approvers stop trusting it.
        var meId = CreateCurrentUserEmployee("INB400ME" + status);
        var requesterId = CreateEmployee("INB400REQ" + status);
        CreatePendingRequest(requesterId, meId, status: status);

        var result = await _inbox.GetMyInbox(Query());

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Serve_Every_Transaction_Type_From_One_Query()
    {
        // The reason there is one inbox rather than one per type.
        var meId = CreateCurrentUserEmployee("INB500ME");
        var requesterId = CreateEmployee("INB500REQ");

        CreatePendingRequest(requesterId, meId, requestType: "EmployeeAbsence", summary: "Leave");
        CreatePendingRequest(requesterId, meId, requestType: "EmployeeAsset", summary: "Laptop");
        CreatePendingRequest(requesterId, meId, requestType: "EmployeeLoan", summary: "Loan");

        var result = await _inbox.GetMyInbox(Query());

        result.TotalCount.ShouldBe(3);
        result.Items.Select(i => i.RequestType).ShouldContain("EmployeeAsset");
        result.Items.Select(i => i.RequestType).ShouldContain("EmployeeLoan");
    }

    [Fact]
    public async Task Should_Filter_To_One_Transaction_Type()
    {
        var meId = CreateCurrentUserEmployee("INB600ME");
        var requesterId = CreateEmployee("INB600REQ");

        CreatePendingRequest(requesterId, meId, requestType: "EmployeeAbsence");
        CreatePendingRequest(requesterId, meId, requestType: "EmployeeAsset");

        var result = await _inbox.GetMyInbox(Query("EmployeeAsset"));

        result.TotalCount.ShouldBe(1);
        result.Items.Single().RequestType.ShouldBe("EmployeeAsset");
    }

    [Fact]
    public async Task Should_Report_What_The_Step_Actually_Permits()
    {
        // So the screen never offers a Reject button the engine would refuse.
        var meId = CreateCurrentUserEmployee("INB700ME");
        var requesterId = CreateEmployee("INB700REQ");
        ConfigureLeaveChain(canReject: false, canSendBack: true, steps: 2);
        SetWorkflowSettings(sendBackToCreator: true);
        var requestId = CreatePendingRequest(requesterId, meId);

        var item = (await _inbox.GetMyInbox(Query())).Items.Single(i => i.Id == requestId);

        item.CanReject.ShouldBeFalse();
        item.CanSendBack.ShouldBeTrue();

        // Two approval rows, numbered 1 and 2: the last step that names an approver is 2.
        item.TotalSteps.ShouldBe(2);
    }

    [Fact]
    public async Task Should_Not_Count_The_Creator_Row_As_An_Approval_Step()
    {
        // EXIT-STD in the live Default tenant, exactly: three rows, the first of which nominates nobody
        // because it is the rule for who may RAISE the request, and a stored WorkflowSteps of 2 that was
        // never recalculated. A request at step 2 was shown as "Step 2 / 2" -- reading as the final
        // approval when the step-3 approver had not seen it yet.
        var meId = CreateCurrentUserEmployee("INB750ME");
        var requesterId = CreateEmployee("INB750REQ");
        ConfigureChainWithCreatorRow(storedWorkflowSteps: 2);
        var requestId = CreatePendingRequest(requesterId, meId, workflowStep: 2);

        var item = (await _inbox.GetMyInbox(Query())).Items.Single(i => i.Id == requestId);

        // Not 2 (the stale stored count, and also the number of approval rows) -- the numerator is the
        // engine's own step number, so the denominator has to be the last step number that has an approver.
        item.TotalSteps.ShouldBe(3);
        item.WorkflowStep.ShouldBe(2);
    }

    /// <summary>
    /// A chain shaped like the ported production ones: step 1 is the creator rule (no approver), the
    /// approvals are steps 2 and 3, and <c>RequestMaster.WorkflowSteps</c> is whatever was stored last.
    /// </summary>
    private void ConfigureChainWithCreatorRow(int storedWorkflowSteps)
    {
        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = "R" + Guid.NewGuid().ToString("N").Substring(0, 5),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "CREATOR-STD",
                NameEn = "With creator row",
                NameAr = "With creator row",
                WorkflowSteps = storedWorkflowSteps
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                ApproverPerson = WorkflowApproverPersonType.None,
                Person = WorkflowPersonScope.Themselves,
                ApprovalRequire = ApprovalRequire.Single
            });

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 2,
                ApproverPerson = WorkflowApproverPersonType.PersonManager,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 3,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAbsence");
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });
    }

    private void SetWorkflowSettings(bool sendBackToCreator = false, int travelCostStep = 0)
    {
        UsingDbContext(context =>
        {
            var settings = context.Set<HCMv3.SystemSettings.ManageWorkflowSettings>().FirstOrDefault(s => s.TenantId == TenantId)
                           ?? context.Set<HCMv3.SystemSettings.ManageWorkflowSettings>()
                               .Add(new HCMv3.SystemSettings.ManageWorkflowSettings { TenantId = TenantId }).Entity;
            settings.SendBackToCreator = sendBackToCreator;
            settings.EnableTravelCostOnWorkflowstep = travelCostStep;
        });
    }

    /// <summary>
    /// Legacy TravelController: at the step named by ManageWorkflowSettings.EnableTravelCostOnWorkflowstep, a travel
    /// request's approver is offered the travel cost entry; other request types never are.
    /// </summary>
    [Fact]
    public async Task Travel_Cost_Should_Be_Offered_Only_On_A_Travel_Request_At_The_Configured_Step()
    {
        var meId = CreateCurrentUserEmployee("INB720ME");
        var requesterId = CreateEmployee("INB720REQ");
        ConfigureLeaveChain(typeCode: "Travel");
        ConfigureLeaveChain();
        SetWorkflowSettings(travelCostStep: 1);
        var travelId = CreatePendingRequest(requesterId, meId, requestType: "Travel", summary: "Trip");
        var leaveId = CreatePendingRequest(requesterId, meId);

        var items = (await _inbox.GetMyInbox(Query())).Items;

        items.Single(i => i.Id == travelId).EnableTravelCost.ShouldBeTrue();
        items.Single(i => i.Id == leaveId).EnableTravelCost.ShouldBeFalse();

        SetWorkflowSettings(travelCostStep: 2);
        (await _inbox.GetMyInbox(Query())).Items.Single(i => i.Id == travelId).EnableTravelCost.ShouldBeFalse();
    }

    /// <summary>Legacy ESS: the send-back button needs ManageWorkflowSettings.SendBackToCreator as well as the step's own flag.</summary>
    [Fact]
    public async Task Send_Back_Should_Not_Be_Offered_When_The_Tenant_Setting_Is_Off()
    {
        var meId = CreateCurrentUserEmployee("INB710ME");
        var requesterId = CreateEmployee("INB710REQ");
        ConfigureLeaveChain(canReject: true, canSendBack: true);
        SetWorkflowSettings(sendBackToCreator: false);
        var requestId = CreatePendingRequest(requesterId, meId);

        var item = (await _inbox.GetMyInbox(Query())).Items.Single(i => i.Id == requestId);

        item.CanSendBack.ShouldBeFalse();
        item.CanReject.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Still_List_A_Request_Whose_Chain_Cannot_Be_Resolved()
    {
        // No catalog configuration for this type, so the capabilities cannot be read. The row must still
        // appear -- one broken configuration should not hide everything else waiting on an approver -- but
        // it degrades to approve-only rather than guessing.
        var meId = CreateCurrentUserEmployee("INB800ME");
        var requesterId = CreateEmployee("INB800REQ");
        var requestId = CreatePendingRequest(requesterId, meId, requestType: "SomethingUncatalogued");

        var item = (await _inbox.GetMyInbox(Query())).Items.Single(i => i.Id == requestId);

        item.CanReject.ShouldBeFalse();
        item.CanSendBack.ShouldBeFalse();
        item.TotalSteps.ShouldBe(0);
    }

    [Fact]
    public async Task Should_Offer_A_Photo_Url_Only_When_The_Requester_Has_One()
    {
        var meId = CreateCurrentUserEmployee("INB900ME");
        var withPhotoId = CreateEmployee("INB900PHOTO");
        var withoutPhotoId = CreateEmployee("INB900NOPHOTO");

        UsingDbContext(context =>
        {
            context.Set<Employee>().Single(e => e.Id == withPhotoId).PhotoPath = "tenant-1/employee-photos/1-test.img";
        });

        var withPhoto = CreatePendingRequest(withPhotoId, meId);
        var withoutPhoto = CreatePendingRequest(withoutPhotoId, meId);

        var items = (await _inbox.GetMyInbox(Query())).Items;

        items.Single(i => i.Id == withPhoto).RequesterPhotoUrl.ShouldBe("/api/employee-photo/" + withPhotoId);
        items.Single(i => i.Id == withoutPhoto).RequesterPhotoUrl.ShouldBeNull();
    }

    [Fact]
    public async Task Approving_Should_Complete_A_Single_Step_Request()
    {
        var meId = CreateCurrentUserEmployee("INB1000ME");
        var requesterId = CreateEmployee("INB1000REQ");
        ConfigureLeaveChain(steps: 1);
        var requestId = CreatePendingRequest(requesterId, meId);

        await _inbox.Approve(new ApproveRequestInput { WorkflowRequestId = requestId, Comment = "fine" });

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId));
        request.RequestStatus.ShouldBe(RequestStatus.Approved);

        // And it leaves the inbox.
        (await _inbox.GetMyPendingCount()).ShouldBe(0);
    }

    [Fact]
    public async Task Rejecting_Should_Record_The_Reason_On_The_Audit_Trail()
    {
        var meId = CreateCurrentUserEmployee("INB1100ME");
        var requesterId = CreateEmployee("INB1100REQ");
        ConfigureLeaveChain();
        var requestId = CreatePendingRequest(requesterId, meId);

        await _inbox.Reject(new RejectRequestInput { WorkflowRequestId = requestId, Comment = "Too short notice" });

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId));
        request.RequestStatus.ShouldBe(RequestStatus.Rejected);

        // The reason has to survive into the activity feed, or the requester never learns why.
        var log = UsingDbContext(c => c.Set<RequestLog>()
            .Where(l => l.WorkflowRequestId == requestId && l.ActionType == RequestActionType.Rejected)
            .Select(l => l.RequestActionComment)
            .FirstOrDefault());

        log.ShouldBe("Too short notice");
    }

    [Fact]
    public async Task Sending_Back_Should_Return_The_Request_To_Draft()
    {
        var meId = CreateCurrentUserEmployee("INB1200ME");
        var requesterId = CreateEmployee("INB1200REQ");
        ConfigureLeaveChain();
        var requestId = CreatePendingRequest(requesterId, meId);

        await _inbox.SendBack(new SendBackRequestInput { WorkflowRequestId = requestId, Comment = "Attach the form" });

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId));

        // Draft, not Rejected: still alive and resubmittable.
        request.RequestStatus.ShouldBe(RequestStatus.Draft);
    }

    [Fact]
    public async Task Should_Refuse_To_Approve_A_Request_Not_Waiting_On_Me()
    {
        // Authorization must not rest on the inbox query having been honest about what it listed -- the
        // engine re-checks the current approver on every action.
        CreateCurrentUserEmployee("INB1300ME");
        var otherApproverId = CreateEmployee("INB1300OTHER");
        var requesterId = CreateEmployee("INB1300REQ");
        ConfigureLeaveChain();
        var requestId = CreatePendingRequest(requesterId, otherApproverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _inbox.Approve(new ApproveRequestInput { WorkflowRequestId = requestId, Comment = "not mine" }));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId));
        request.RequestStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task The_Badge_Count_Should_Match_What_The_List_Returns()
    {
        var meId = CreateCurrentUserEmployee("INB1400ME");
        var requesterId = CreateEmployee("INB1400REQ");

        CreatePendingRequest(requesterId, meId);
        CreatePendingRequest(requesterId, meId, requestType: "EmployeeAsset");
        CreatePendingRequest(requesterId, meId, approverAlreadyActed: true);
        CreatePendingRequest(requesterId, meId, status: RequestStatus.Approved);

        var listed = (await _inbox.GetMyInbox(Query())).TotalCount;
        var badge = await _inbox.GetMyPendingCount();

        // The badge is a separate cheaper query, so it can drift from the list if the two disagree about
        // what counts as pending.
        badge.ShouldBe(listed);
        badge.ShouldBe(2);
    }

    // ---------------------------------------------------------------- get feedback

    /// <summary>
    /// THE property of this feature, and the one everything else depends on: asking moves nothing.
    ///
    /// <para>
    /// The approver who asks is still the approver, the request is still on its step, and its status has
    /// not changed. If any of that stopped being true, "get feedback" would have quietly become a
    /// delegation -- which is a different feature with different consequences for who is accountable.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Asking_For_Feedback_Should_Not_Move_The_Request()
    {
        var meId = CreateCurrentUserEmployee("FBK100ME");
        var requesterId = CreateEmployee("FBK100REQ");
        var colleagueId = CreateEmployee("FBK100COL");
        var requestId = CreatePendingRequest(requesterId, meId);

        await _inbox.RequestFeedback(new RequestFeedbackInput
        {
            WorkflowRequestId = requestId,
            EmployeeIds = new List<int> { colleagueId },
            Note = "Does this date look right to you?"
        });

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId));

        request.WorkflowStep.ShouldBe(1);
        request.RequestStatus.ShouldBe(RequestStatus.Open);

        // And it is still sitting with the person who asked.
        var stillMine = await _inbox.GetMyInbox(Query());
        stillMine.Items.ShouldContain(i => i.Id == requestId && i.Kind == EssInboxItemKind.Approval);
    }

    /// <summary>
    /// An unanswered ask must not be able to hold a request up. The approver can decide at any moment, and
    /// nobody has to chase the colleague first.
    /// </summary>
    [Fact]
    public async Task An_Unanswered_Ask_Should_Not_Block_The_Approval()
    {
        var meId = CreateCurrentUserEmployee("FBK200ME");
        var requesterId = CreateEmployee("FBK200REQ");
        var colleagueId = CreateEmployee("FBK200COL");
        var requestId = CreatePendingRequest(requesterId, meId);
        ConfigureLeaveChain();

        await _inbox.RequestFeedback(new RequestFeedbackInput
        {
            WorkflowRequestId = requestId,
            EmployeeIds = new List<int> { colleagueId }
        });

        // Nobody has replied. Approve anyway.
        await _inbox.Approve(new ApproveRequestInput { WorkflowRequestId = requestId, Comment = "fine" });

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == requestId));
        request.RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    /// <summary>
    /// Only the approver the request is waiting on may pull colleagues into it. Being able to READ a
    /// request is not the same entitlement -- the requester can read their own.
    /// </summary>
    [Fact]
    public async Task Someone_Who_Is_Not_The_Current_Approver_Should_Not_Be_Able_To_Ask()
    {
        var meId = CreateCurrentUserEmployee("FBK300ME");
        var requesterId = CreateEmployee("FBK300REQ");
        var otherApproverId = CreateEmployee("FBK300OTHER");
        var colleagueId = CreateEmployee("FBK300COL");

        // Waiting on somebody else entirely.
        var requestId = CreatePendingRequest(requesterId, otherApproverId);

        await Should.ThrowAsync<UserFriendlyException>(() => _inbox.RequestFeedback(new RequestFeedbackInput
        {
            WorkflowRequestId = requestId,
            EmployeeIds = new List<int> { colleagueId }
        }));
    }

    /// <summary>Asking the requester what they think of their own request is not a thing anyone means.</summary>
    [Fact]
    public async Task The_Requester_Should_Not_Be_Askable_On_Their_Own_Request()
    {
        var meId = CreateCurrentUserEmployee("FBK400ME");
        var requesterId = CreateEmployee("FBK400REQ");
        var requestId = CreatePendingRequest(requesterId, meId);

        await Should.ThrowAsync<UserFriendlyException>(() => _inbox.RequestFeedback(new RequestFeedbackInput
        {
            WorkflowRequestId = requestId,
            EmployeeIds = new List<int> { requesterId }
        }));
    }

    /// <summary>
    /// Answering closes the ask: it leaves the answerer's list, the words land in the conversation everyone
    /// reads, and the log records that somebody was asked and replied.
    ///
    /// <para>
    /// The log entry is Remarked and not Approved on purpose. A feedback entry counted as an approval would
    /// make a report say a request was approved by somebody who decided nothing.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Answering_Should_Close_The_Ask_And_Be_Recorded()
    {
        var approverId = CreateEmployee("FBK500APR");
        var requesterId = CreateEmployee("FBK500REQ");
        var meId = CreateCurrentUserEmployee("FBK500ME");
        var requestId = CreatePendingRequest(requesterId, approverId);

        // Asked directly, since the asking path needs the asker to be the current approver and this test
        // is about the answering half.
        UsingDbContext(context => context.Set<WorkflowRemark>().Add(new WorkflowRemark
        {
            TenantId = TenantId,
            WorkflowRequestId = requestId,
            CCReceiverId = meId,
            RequestedById = approverId,
            WorkflowStep = 1
        }));

        var before = await _inbox.GetMyInbox(Query());
        before.Items.ShouldContain(i => i.Id == requestId && i.Kind == EssInboxItemKind.FeedbackRequest);

        await _inbox.SubmitFeedback(new SubmitFeedbackInput
        {
            WorkflowRequestId = requestId,
            Comment = "The dates are consistent with the roster."
        });

        // Gone from the list: nothing is expected of them any more.
        var after = await _inbox.GetMyInbox(Query());
        after.Items.ShouldNotContain(i => i.Id == requestId && i.Kind == EssInboxItemKind.FeedbackRequest);

        var ask = UsingDbContext(c => c.Set<WorkflowRemark>().Single(r => r.WorkflowRequestId == requestId));
        ask.RespondedAt.ShouldNotBeNull();

        var log = UsingDbContext(c => c.Set<RequestLog>()
            .Where(l => l.WorkflowRequestId == requestId && l.ActionById == meId)
            .ToList());

        log.ShouldContain(l => l.ActionType == RequestActionType.Remarked);
        log.ShouldNotContain(l => l.ActionType == RequestActionType.Approved);
    }

    /// <summary>A settled ask cannot be answered twice -- the second submission has nothing to close.</summary>
    [Fact]
    public async Task Answering_Twice_Should_Be_Refused()
    {
        var approverId = CreateEmployee("FBK600APR");
        var requesterId = CreateEmployee("FBK600REQ");
        var meId = CreateCurrentUserEmployee("FBK600ME");
        var requestId = CreatePendingRequest(requesterId, approverId);

        UsingDbContext(context => context.Set<WorkflowRemark>().Add(new WorkflowRemark
        {
            TenantId = TenantId,
            WorkflowRequestId = requestId,
            CCReceiverId = meId,
            RequestedById = approverId,
            WorkflowStep = 1
        }));

        await _inbox.SubmitFeedback(new SubmitFeedbackInput
        {
            WorkflowRequestId = requestId,
            Comment = "First and only answer."
        });

        await Should.ThrowAsync<UserFriendlyException>(() => _inbox.SubmitFeedback(new SubmitFeedbackInput
        {
            WorkflowRequestId = requestId,
            Comment = "Second answer."
        }));
    }

    /// <summary>
    /// Somebody who was never asked cannot answer. Without this, anyone who could see a request could post
    /// into its conversation through this path and have it recorded as solicited feedback.
    /// </summary>
    [Fact]
    public async Task Answering_Without_Being_Asked_Should_Be_Refused()
    {
        var approverId = CreateEmployee("FBK700APR");
        var requesterId = CreateEmployee("FBK700REQ");
        CreateCurrentUserEmployee("FBK700ME");
        var requestId = CreatePendingRequest(requesterId, approverId);

        await Should.ThrowAsync<UserFriendlyException>(() => _inbox.SubmitFeedback(new SubmitFeedbackInput
        {
            WorkflowRequestId = requestId,
            Comment = "Nobody asked me."
        }));
    }

    /// <summary>
    /// The picker flags people rather than hiding them: already asked, and already in the approval chain.
    /// The requester is the one genuine exclusion.
    /// </summary>
    [Fact]
    public async Task Candidates_Should_Flag_Existing_Approvers_And_Exclude_The_Requester()
    {
        var meId = CreateCurrentUserEmployee("FBK800ME");
        var requesterId = CreateEmployee("FBK800REQ");
        var colleagueId = CreateEmployee("FBK800COL");
        var requestId = CreatePendingRequest(requesterId, meId);

        var candidates = await _inbox.GetFeedbackCandidates(new GetFeedbackCandidatesInput
        {
            WorkflowRequestId = requestId
        });

        // The requester is not on the list at all.
        candidates.ShouldNotContain(c => c.EmployeeId == requesterId);

        // Nor is the person doing the asking.
        candidates.ShouldNotContain(c => c.EmployeeId == meId);

        var colleague = candidates.Single(c => c.EmployeeId == colleagueId);
        colleague.AlreadyAsked.ShouldBeFalse();
        colleague.IsAlreadyApprover.ShouldBeFalse();

        await _inbox.RequestFeedback(new RequestFeedbackInput
        {
            WorkflowRequestId = requestId,
            EmployeeIds = new List<int> { colleagueId }
        });

        var afterAsking = await _inbox.GetFeedbackCandidates(new GetFeedbackCandidatesInput
        {
            WorkflowRequestId = requestId
        });

        afterAsking.Single(c => c.EmployeeId == colleagueId).AlreadyAsked.ShouldBeTrue();
    }

    // ------------------------------------------------------------------ tabs

    private static GetMyInboxInput TabQuery(EssInboxTab tab) =>
        new GetMyInboxInput { Tab = tab, MaxResultCount = 50, SkipCount = 0 };

    /// <summary>Records that this person sent the request back, which is the only trace a send-back leaves.</summary>
    private void LogSendBack(int requestId, int actorEmployeeId)
    {
        UsingDbContext(context =>
        {
            context.Set<RequestLog>().Add(new RequestLog
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ActionById = actorEmployeeId,
                ActionType = RequestActionType.SentBack,
                ActionDateTime = DateTime.Now,
                WorkflowStep = 1
            });
        });
    }

    [Fact]
    public async Task The_Awaiting_Me_Tab_Should_Show_Only_Requests_Waiting_On_Me()
    {
        var meId = CreateCurrentUserEmployee("TAB100ME");
        var requesterId = CreateEmployee("TAB100REQ");
        var waiting = CreatePendingRequest(requesterId, meId);
        CreatePendingRequest(requesterId, meId, approverAlreadyActed: true);

        var result = await _inbox.GetMyInbox(TabQuery(EssInboxTab.AwaitingMe));

        result.Items.Select(i => i.Id).ShouldBe(new[] { waiting });
    }

    [Fact]
    public async Task The_Approved_Tab_Should_Show_What_I_Approved_And_Is_Still_Running()
    {
        var meId = CreateCurrentUserEmployee("TAB200ME");
        var requesterId = CreateEmployee("TAB200REQ");
        var stillWaiting = CreatePendingRequest(requesterId, meId);
        var alreadyApproved = CreatePendingRequest(requesterId, meId, approverAlreadyActed: true);

        var result = await _inbox.GetMyInbox(TabQuery(EssInboxTab.Approved));

        var ids = result.Items.Select(i => i.Id).ToList();
        ids.ShouldContain(alreadyApproved);
        ids.ShouldNotContain(stillWaiting);
    }

    /// <summary>
    /// The point of the tab: it answers whether something went through, so a request that has finished is
    /// no longer an open question and drops out.
    /// </summary>
    [Fact]
    public async Task The_Approved_Tab_Should_Drop_A_Request_That_Has_Finished()
    {
        var meId = CreateCurrentUserEmployee("TAB210ME");
        var requesterId = CreateEmployee("TAB210REQ");
        CreatePendingRequest(requesterId, meId, approverAlreadyActed: true, status: RequestStatus.Approved);

        var result = await _inbox.GetMyInbox(TabQuery(EssInboxTab.Approved));

        result.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task The_Sent_Back_Tab_Should_Show_What_I_Pushed_Back()
    {
        var meId = CreateCurrentUserEmployee("TAB300ME");
        var requesterId = CreateEmployee("TAB300REQ");
        var pushedBack = CreatePendingRequest(requesterId, meId);
        var untouched = CreatePendingRequest(requesterId, meId);
        LogSendBack(pushedBack, meId);

        var result = await _inbox.GetMyInbox(TabQuery(EssInboxTab.SentBack));

        var ids = result.Items.Select(i => i.Id).ToList();
        ids.ShouldContain(pushedBack);
        ids.ShouldNotContain(untouched);
    }

    /// <summary>Another approver's send-back is not mine, however open the request still is.</summary>
    [Fact]
    public async Task The_Sent_Back_Tab_Should_Not_Show_Another_Approvers_Send_Back()
    {
        var meId = CreateCurrentUserEmployee("TAB310ME");
        var colleagueId = CreateEmployee("TAB310COL");
        var requesterId = CreateEmployee("TAB310REQ");
        var request = CreatePendingRequest(requesterId, meId);
        LogSendBack(request, colleagueId);

        var result = await _inbox.GetMyInbox(TabQuery(EssInboxTab.SentBack));

        result.TotalCount.ShouldBe(0);
    }

    /// <summary>Neither retrospective tab is actionable; the buttons must not be offered.</summary>
    [Fact]
    public async Task The_Retrospective_Tabs_Should_Offer_No_Decisions()
    {
        var meId = CreateCurrentUserEmployee("TAB400ME");
        var requesterId = CreateEmployee("TAB400REQ");
        ConfigureLeaveChain();
        var request = CreatePendingRequest(requesterId, meId, approverAlreadyActed: true);
        LogSendBack(request, meId);

        var approved = await _inbox.GetMyInbox(TabQuery(EssInboxTab.Approved));
        var sentBack = await _inbox.GetMyInbox(TabQuery(EssInboxTab.SentBack));

        approved.Items.Single().CanReject.ShouldBeFalse();
        approved.Items.Single().CanSendBack.ShouldBeFalse();
        sentBack.Items.Single().CanReject.ShouldBeFalse();
        sentBack.Items.Single().CanSendBack.ShouldBeFalse();
    }

    [Fact]
    public async Task The_Counts_Should_Agree_With_What_Each_Tab_Lists()
    {
        var meId = CreateCurrentUserEmployee("CNT100ME");
        var requesterId = CreateEmployee("CNT100REQ");
        CreatePendingRequest(requesterId, meId);
        CreatePendingRequest(requesterId, meId, approverAlreadyActed: true);
        var pushedBack = CreatePendingRequest(requesterId, meId);
        LogSendBack(pushedBack, meId);

        var counts = await _inbox.GetMyInboxCounts();

        counts.AwaitingMe.ShouldBe((await _inbox.GetMyInbox(TabQuery(EssInboxTab.AwaitingMe))).TotalCount);
        counts.Approved.ShouldBe((await _inbox.GetMyInbox(TabQuery(EssInboxTab.Approved))).TotalCount);
        counts.SentBack.ShouldBe((await _inbox.GetMyInbox(TabQuery(EssInboxTab.SentBack))).TotalCount);
        counts.Approved.ShouldBe(1);
        counts.SentBack.ShouldBe(1);
    }

    /// <summary>Sending the same request back twice is one row in the tab, so it must be one in the count.</summary>
    [Fact]
    public async Task Sending_The_Same_Request_Back_Twice_Should_Count_Once()
    {
        var meId = CreateCurrentUserEmployee("CNT200ME");
        var requesterId = CreateEmployee("CNT200REQ");
        var request = CreatePendingRequest(requesterId, meId);
        LogSendBack(request, meId);
        LogSendBack(request, meId);

        var counts = await _inbox.GetMyInboxCounts();

        counts.SentBack.ShouldBe(1);
    }

    // ------------------------------------------------------------------ bulk approve

    [Fact]
    public async Task Should_Approve_Several_Requests_At_Once()
    {
        var meId = CreateCurrentUserEmployee("BLK100ME");
        var requesterId = CreateEmployee("BLK100REQ");
        ConfigureLeaveChain();
        var first = CreatePendingRequest(requesterId, meId);
        var second = CreatePendingRequest(requesterId, meId);

        var result = await _inbox.BulkApprove(new BulkApproveInput
        {
            WorkflowRequestIds = new List<int> { first, second }
        });

        result.ApprovedCount.ShouldBe(2);
        result.FailedCount.ShouldBe(0);
        result.SkippedCount.ShouldBe(0);
        (await _inbox.GetMyInbox(TabQuery(EssInboxTab.AwaitingMe))).TotalCount.ShouldBe(0);
    }

    /// <summary>
    /// The rule the audit called out: a step that demands a comment is left alone rather than approved with
    /// a blank one, and says so per row.
    /// </summary>
    [Fact]
    public async Task Should_Skip_A_Request_Whose_Step_Requires_A_Comment()
    {
        var meId = CreateCurrentUserEmployee("BLK200ME");
        var requesterId = CreateEmployee("BLK200REQ");
        ConfigureLeaveChain();
        RequireCommentOnEveryStep();

        var request = CreatePendingRequest(requesterId, meId);

        var result = await _inbox.BulkApprove(new BulkApproveInput
        {
            WorkflowRequestIds = new List<int> { request }
        });

        result.SkippedCount.ShouldBe(1);
        result.ApprovedCount.ShouldBe(0);
        result.Items.Single().Outcome.ShouldBe(BulkApproveOutcome.SkippedCommentRequired);

        // Left where it was, so it can be opened and answered properly.
        (await _inbox.GetMyInbox(TabQuery(EssInboxTab.AwaitingMe))).TotalCount.ShouldBe(1);
    }

    /// <summary>A comment on the batch satisfies those same steps -- otherwise they could never be cleared.</summary>
    [Fact]
    public async Task A_Comment_On_The_Batch_Should_Satisfy_A_Step_That_Requires_One()
    {
        var meId = CreateCurrentUserEmployee("BLK300ME");
        var requesterId = CreateEmployee("BLK300REQ");
        ConfigureLeaveChain();
        RequireCommentOnEveryStep();

        var request = CreatePendingRequest(requesterId, meId);

        var result = await _inbox.BulkApprove(new BulkApproveInput
        {
            WorkflowRequestIds = new List<int> { request },
            Comment = "Checked against the roster."
        });

        result.ApprovedCount.ShouldBe(1);
        result.SkippedCount.ShouldBe(0);
    }

    private void RequireCommentOnEveryStep()
    {
        UsingDbContext(context =>
        {
            foreach (var detail in context.Set<RequestDetail>().ToList())
            {
                detail.IsCommentRequire = true;
            }
        });
    }

    /// <summary>
    /// One refusal must not cost the others. This is the whole reason the call reports per row instead of
    /// throwing.
    /// </summary>
    [Fact]
    public async Task One_Refusal_Should_Not_Stop_The_Rest_Being_Approved()
    {
        var meId = CreateCurrentUserEmployee("BLK400ME");
        var requesterId = CreateEmployee("BLK400REQ");
        ConfigureLeaveChain();
        var mine = CreatePendingRequest(requesterId, meId);

        // Waiting on somebody else, so the engine will refuse it for this approver.
        var colleagueId = CreateEmployee("BLK400COL");
        var theirs = CreatePendingRequest(requesterId, colleagueId);

        var result = await _inbox.BulkApprove(new BulkApproveInput
        {
            WorkflowRequestIds = new List<int> { theirs, mine }
        });

        result.ApprovedCount.ShouldBe(1);
        result.FailedCount.ShouldBe(1);
        result.Items.Single(i => i.WorkflowRequestId == mine).Outcome.ShouldBe(BulkApproveOutcome.Approved);
        result.Items.Single(i => i.WorkflowRequestId == theirs).Outcome.ShouldBe(BulkApproveOutcome.Failed);
    }

    [Fact]
    public async Task Bulk_Approving_Nothing_Should_Do_Nothing()
    {
        CreateCurrentUserEmployee("BLK500ME");

        var result = await _inbox.BulkApprove(new BulkApproveInput { WorkflowRequestIds = new List<int>() });

        result.ApprovedCount.ShouldBe(0);
        result.Items.ShouldBeEmpty();
    }

    /// <summary>A request that has gone is reported as a failed row, not an exception that loses the batch.</summary>
    [Fact]
    public async Task A_Missing_Request_Should_Be_Reported_Rather_Than_Thrown()
    {
        CreateCurrentUserEmployee("BLK600ME");

        var result = await _inbox.BulkApprove(new BulkApproveInput { WorkflowRequestIds = new List<int> { 999999 } });

        result.FailedCount.ShouldBe(1);
        result.Items.Single().Outcome.ShouldBe(BulkApproveOutcome.Failed);
    }
}
