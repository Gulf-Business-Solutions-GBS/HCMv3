using System;
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
        RequestStatus status = RequestStatus.Open)
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
                WorkflowStep = 1,
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
                IsApproved = approverAlreadyActed
            });
        });

        return requestId;
    }

    /// <summary>A one-step chain wired to the leave catalog row, so step capabilities resolve.</summary>
    private void ConfigureLeaveChain(bool canReject = true, bool canSendBack = true, int steps = 1)
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
                Code = "LEAVE-STD",
                NameEn = "Standard Leave",
                NameAr = "Standard Leave",
                WorkflowSteps = steps
            }).Entity;

            context.SaveChanges();

            context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = master.Id,
                Step = 1,
                WorkflowRoleId = role.Id,
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = canReject,
                CanSendBack = canSendBack
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeAbsence");
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
        var requestId = CreatePendingRequest(requesterId, meId);

        var item = (await _inbox.GetMyInbox(Query())).Items.Single(i => i.Id == requestId);

        item.CanReject.ShouldBeFalse();
        item.CanSendBack.ShouldBeTrue();
        item.TotalSteps.ShouldBe(2);
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
}
