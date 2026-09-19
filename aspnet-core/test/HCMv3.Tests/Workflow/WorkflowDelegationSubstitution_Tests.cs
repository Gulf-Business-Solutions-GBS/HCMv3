using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers approver substitution: while somebody has approved cover in place, the chain goes to the person
/// covering them.
///
/// <para>
/// Until this existed an <c>EmployeeDelegation</c> row copied a colleague on emails and nothing else, so the
/// approval itself still waited for whoever was on a beach. These tests pin down the four decisions that make
/// substitution mean something: the stand-in REPLACES the delegator rather than joining them, the audit trail
/// names both people, exactly ONE hop is followed, and the delegation itself is the authority (the stand-in
/// does not need the delegator's role).
/// </para>
/// <para>
/// They also pin the two things that would quietly ruin it: cover that is not yet approved must grant
/// nothing, and cover handed to somebody who has left must not be handed over at all.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowDelegationSubstitution_Tests : AppTestBase
{
    private readonly IWorkflowApproverResolver _resolver;
    private readonly IWorkflowApprovalService _approval;
    private readonly IWorkflowEscalationService _escalation;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public WorkflowDelegationSubstitution_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _resolver = Resolve<IWorkflowApproverResolver>();
        _approval = Resolve<IWorkflowApprovalService>();
        _escalation = Resolve<IWorkflowEscalationService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // ---------------------------------------------------------------- arrangement helpers

    /// <summary>
    /// <paramref name="status"/> defaults to Active on purpose: <c>EmployeeStatus.Inactive</c> is the zero
    /// value, so an employee created without one is a leaver, and every delegation to them would hit the
    /// safety valve instead of the rule under test.
    /// </summary>
    private int CreateEmployee(string code, EmployeeStatus status = EmployeeStatus.Active)
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
                Status = status,
                BirthDate = new DateTime(1990, 1, 1),
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    /// <summary>
    /// Inserts cover directly, bypassing the app services, so a test can arrange states those services now
    /// refuse to create -- a self-delegation, an overlap, cover that is still awaiting approval.
    /// </summary>
    private int CreateDelegation(
        int employeeId,
        int delegateToId,
        RequestStatus? workflowStatus = RequestStatus.Approved,
        int startsInDays = -1,
        int endsInDays = 30)
    {
        EmployeeDelegation delegation = null;
        UsingDbContext(context =>
        {
            delegation = context.Set<EmployeeDelegation>().Add(new EmployeeDelegation
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DelegateToId = delegateToId,
                StartDate = DateTime.Today.AddDays(startsInDays),
                EndDate = DateTime.Today.AddDays(endsInDays),
                WorkflowStatus = workflowStatus
            }).Entity;
        });

        return delegation.Id;
    }

    private void SetManager(int employeeId, int managerId)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeManager>().Add(new EmployeeManager
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                ManagerId = managerId,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });
        });
    }

    private int CreateWorkflowRole(string name = "Approver")
    {
        WorkflowRole role = null;
        UsingDbContext(context =>
        {
            role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = name,
                NameAr = name
            }).Entity;
        });

        return role.Id;
    }

    private void GiveRole(int employeeId, int roleId)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                WorkflowRoleId = roleId,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });
        });
    }

    private int CreateMaster(int steps = 1, int? defaultApproverId = null)
    {
        RequestMaster master = null;
        UsingDbContext(context =>
        {
            master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "M" + Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Chain",
                NameAr = "Chain",
                WorkflowSteps = steps,
                DefaultApproverId = defaultApproverId
            }).Entity;
        });

        return master.Id;
    }

    private int AddStep(
        int masterId,
        int step,
        WorkflowApproverPersonType approverPerson = WorkflowApproverPersonType.Person,
        int? roleId = null,
        bool shouldEscalate = false,
        int escalateAfterDays = 0,
        int? escalateRoleId = null,
        int reminderDays = 0)
    {
        RequestDetail detail = null;
        UsingDbContext(context =>
        {
            detail = context.Set<RequestDetail>().Add(new RequestDetail
            {
                TenantId = TenantId,
                RequestMasterId = masterId,
                Step = step,
                ApproverPerson = approverPerson,
                WorkflowRoleId = roleId,
                WorkflowRoleGroup = WorkflowRoleGroup.None,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true,
                ShouldEscalate = shouldEscalate,
                EscalateAfterDays = escalateAfterDays,
                EscalateNextLevel = false,
                EscalateWorkflowRoleId = escalateRoleId,
                ReminderDays = reminderDays
            }).Entity;
        });

        return detail.Id;
    }

    private void EnableCatalog(int masterId, string code = "EmployeeAbsence")
    {
        UsingDbContext(context =>
        {
            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == code);
            type.IsWorkflowEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = masterId;
        });
    }

    private int CreateRequest(int requesterId, RequestStatus status, int step, string requestType = "EmployeeAbsence")
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
                WorkflowStep = step,
                SummaryEn = "Something",
                SummaryAr = "Something"
            }).Entity;
        });

        return request.Id;
    }

    private int CreateDraftRequest(int requesterId) => CreateRequest(requesterId, RequestStatus.Draft, 0);

    private int CreateOpenRequest(int requesterId) => CreateRequest(requesterId, RequestStatus.Open, 1);

    private void SetApprover(int requestId, int approverId)
    {
        UsingDbContext(context =>
        {
            context.Set<WorkFlowApprover>().Add(new WorkFlowApprover
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ApproverId = approverId,
                IsApproved = false
            });

            var request = context.Set<WorkflowRequest>().Single(r => r.Id == requestId);
            request.ApproverId = approverId;
        });
    }

    private void SetLastActionDaysAgo(int requestId, int actorId, int days)
    {
        UsingDbContext(context =>
        {
            context.Set<RequestLog>().Add(new RequestLog
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ActionById = actorId,
                ActionDateTime = DateTime.Now.AddDays(-days),
                ActionType = RequestActionType.Created,
                WorkflowStep = 1,
                Platform = WorkflowPlatform.Core
            });
        });
    }

    private WorkflowRequest RequestRow(int id) =>
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == id));

    private RequestDetail DetailRow(int id) =>
        UsingDbContext(c => c.Set<RequestDetail>().Single(d => d.Id == id));

    private RequestMaster MasterRow(int id) =>
        UsingDbContext(c => c.Set<RequestMaster>().Single(m => m.Id == id));

    private List<int> LiveApproverIds(int requestId) =>
        UsingDbContext(c => c.Set<WorkFlowApprover>()
            .Where(a => a.WorkflowRequestId == requestId && !a.IsDeleted && a.ApproverId.HasValue)
            .Select(a => a.ApproverId.Value)
            .ToList());

    private List<RequestLog> LogsFor(int requestId) =>
        UsingDbContext(c => c.Set<RequestLog>().Where(l => l.WorkflowRequestId == requestId).ToList());

    /// <summary>
    /// Resolves a step the way the engine does, inside a unit of work -- which is where the
    /// <c>ApprovedOnly</c> data filter lives. Resolving outside one would see delegations that are still
    /// awaiting approval and prove the opposite of what these tests claim.
    /// </summary>
    private List<int> Resolve(int requestId, int masterId, int detailId)
    {
        using (_unitOfWorkManager.Begin())
        {
            return _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(detailId));
        }
    }

    /// <summary>A one-step chain routed at the requester's direct manager. The commonest real arrangement.</summary>
    private (int RequestId, int MasterId, int DetailId) ManagerChain(string prefix, out int requesterId, out int managerId)
    {
        requesterId = CreateEmployee(prefix + "REQ");
        managerId = CreateEmployee(prefix + "MGR");
        SetManager(requesterId, managerId);

        var masterId = CreateMaster();
        var detailId = AddStep(masterId, 1, WorkflowApproverPersonType.PersonManager);
        var requestId = CreateOpenRequest(requesterId);

        return (requestId, masterId, detailId);
    }

    // ---------------------------------------------------------------- decision 1: replacement

    [Fact]
    public void Cover_Should_Route_The_Step_To_The_Stand_In()
    {
        var chain = ManagerChain("DG100", out _, out var managerId);
        var standInId = CreateEmployee("DG100SUB");
        CreateDelegation(managerId, standInId);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { standInId });
    }

    [Fact]
    public void The_Delegator_Should_Be_Removed_From_The_Candidates_Not_Merely_Joined()
    {
        // THE regression that would silently take this back to legacy. Legacy ADDED the delegate and left the
        // delegator able to act, so "I have arranged cover" guaranteed nothing about who actually acted. This
        // asserts the delegator is gone, not just that the stand-in is present.
        var chain = ManagerChain("DG110", out _, out var managerId);
        var standInId = CreateEmployee("DG110SUB");
        CreateDelegation(managerId, standInId);

        var approvers = Resolve(chain.RequestId, chain.MasterId, chain.DetailId);

        approvers.ShouldNotContain(managerId);
        approvers.ShouldContain(standInId);
    }

    [Fact]
    public async Task The_Stand_In_Should_Be_Able_To_Approve_A_Request_The_Chain_Routed_To_The_Delegator()
    {
        var requesterId = CreateEmployee("DG120REQ");
        var managerId = CreateEmployee("DG120MGR");
        var standInId = CreateEmployee("DG120SUB");
        SetManager(requesterId, managerId);
        CreateDelegation(managerId, standInId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, WorkflowApproverPersonType.PersonManager);
        EnableCatalog(masterId);

        var requestId = CreateDraftRequest(requesterId);
        await _approval.SubmitAsync(requestId, requesterId);

        LiveApproverIds(requestId).ShouldBe(new[] { standInId });

        await _approval.ApproveAsync(requestId, standInId, "Covering for the week.");

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task The_Delegator_Should_Not_Be_Able_To_Approve_While_Cover_Is_Running()
    {
        // The decision the user made with the trade-off in front of them: replacement, not addition. If the
        // dates are wrong the approver is locked out of their own queue until the delegation is edited.
        var requesterId = CreateEmployee("DG130REQ");
        var managerId = CreateEmployee("DG130MGR");
        var standInId = CreateEmployee("DG130SUB");
        SetManager(requesterId, managerId);
        CreateDelegation(managerId, standInId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, WorkflowApproverPersonType.PersonManager);
        EnableCatalog(masterId);

        var requestId = CreateDraftRequest(requesterId);
        await _approval.SubmitAsync(requestId, requesterId);

        await Assert.ThrowsAsync<UserFriendlyException>(
            () => _approval.ApproveAsync(requestId, managerId, "I am back early"));

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Open);
    }

    // ---------------------------------------------------------------- dates

    [Fact]
    public void Cover_That_Has_Not_Started_Should_Change_Nothing()
    {
        var chain = ManagerChain("DG200", out _, out var managerId);
        var standInId = CreateEmployee("DG200SUB");
        CreateDelegation(managerId, standInId, startsInDays: 5, endsInDays: 20);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { managerId });
    }

    [Fact]
    public void Cover_That_Has_Finished_Should_Change_Nothing()
    {
        var chain = ManagerChain("DG210", out _, out var managerId);
        var standInId = CreateEmployee("DG210SUB");
        CreateDelegation(managerId, standInId, startsInDays: -20, endsInDays: -1);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { managerId });
    }

    [Fact]
    public void The_First_Day_Of_Cover_Should_Count()
    {
        // Inclusive at both ends. An approver who arranged cover starting today is away today.
        var chain = ManagerChain("DG220", out _, out var managerId);
        var standInId = CreateEmployee("DG220SUB");
        CreateDelegation(managerId, standInId, startsInDays: 0, endsInDays: 10);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { standInId });
    }

    [Fact]
    public void The_Last_Day_Of_Cover_Should_Count()
    {
        var chain = ManagerChain("DG230", out _, out var managerId);
        var standInId = CreateEmployee("DG230SUB");
        CreateDelegation(managerId, standInId, startsInDays: -10, endsInDays: 0);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { standInId });
    }

    // ---------------------------------------------------------------- effectiveness

    [Fact]
    public void Cover_Still_Awaiting_Approval_Should_Grant_Nothing()
    {
        // The whole reason EmployeeDelegation is routed for approval. A delegation typed this morning and not
        // yet agreed to must not hand anybody authority this afternoon.
        var chain = ManagerChain("DG300", out _, out var managerId);
        var standInId = CreateEmployee("DG300SUB");
        CreateDelegation(managerId, standInId, RequestStatus.Open);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { managerId });
    }

    [Fact]
    public void Draft_Cover_Should_Grant_Nothing()
    {
        var chain = ManagerChain("DG310", out _, out var managerId);
        var standInId = CreateEmployee("DG310SUB");
        CreateDelegation(managerId, standInId, RequestStatus.Draft);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { managerId });
    }

    [Fact]
    public void Refused_Cover_Should_Grant_Nothing()
    {
        var chain = ManagerChain("DG320", out _, out var managerId);
        var standInId = CreateEmployee("DG320SUB");
        CreateDelegation(managerId, standInId, RequestStatus.Rejected);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { managerId });
    }

    [Fact]
    public void Approved_Cover_Should_Substitute()
    {
        var chain = ManagerChain("DG330", out _, out var managerId);
        var standInId = CreateEmployee("DG330SUB");
        CreateDelegation(managerId, standInId, RequestStatus.Approved);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { standInId });
    }

    [Fact]
    public void Cover_With_No_Workflow_Status_Should_Substitute()
    {
        // Null means "never workflow-managed" -- HR typed it in, or the row predates delegation routing. It
        // behaved as real cover before this feature existed and it still does. This is the backward
        // compatibility rule, not a loophole.
        var chain = ManagerChain("DG340", out _, out var managerId);
        var standInId = CreateEmployee("DG340SUB");
        CreateDelegation(managerId, standInId, workflowStatus: null);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { standInId });
    }

    // ---------------------------------------------------------------- decision 3: one hop only

    [Fact]
    public void A_Second_Hop_Should_Not_Carry_Authority()
    {
        // Ali -> Sara and Sara -> Omar must NOT let Omar approve Ali's requests. Authority handed to a named
        // person is not theirs to pass on, and a chain is how a two-row data-entry slip ends up letting
        // somebody three desks away sign off a salary change.
        var chain = ManagerChain("DG400", out _, out var managerId);
        var firstStandInId = CreateEmployee("DG400SUB1");
        var secondStandInId = CreateEmployee("DG400SUB2");

        CreateDelegation(managerId, firstStandInId);
        CreateDelegation(firstStandInId, secondStandInId);

        var approvers = Resolve(chain.RequestId, chain.MasterId, chain.DetailId);

        approvers.ShouldBe(new[] { firstStandInId });
        approvers.ShouldNotContain(secondStandInId);
    }

    [Fact]
    public void Cover_Delegated_To_Yourself_Should_Leave_You_In_Place()
    {
        // Refused on creation now, but old rows exist. Treating it as cover would take an approver's queue
        // away and hand it straight back -- an outage dressed up as cover.
        var chain = ManagerChain("DG410", out _, out var managerId);
        CreateDelegation(managerId, managerId);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { managerId });
    }

    [Fact]
    public void Mutual_Cover_Should_Not_Loop_Or_Strand_The_Request()
    {
        // Ali -> Sara and Sara -> Ali. One hop from the person the chain nominated, and stop. The request
        // lands on exactly one reachable person rather than bouncing or resolving to nobody.
        var chain = ManagerChain("DG420", out _, out var managerId);
        var standInId = CreateEmployee("DG420SUB");

        CreateDelegation(managerId, standInId);
        CreateDelegation(standInId, managerId);

        var approvers = Resolve(chain.RequestId, chain.MasterId, chain.DetailId);

        approvers.Count.ShouldBe(1);
        approvers.ShouldBe(new[] { standInId });
    }

    // ---------------------------------------------------------------- the safety valve

    [Fact]
    public void Cover_Handed_To_A_Leaver_Should_Not_Be_Handed_Over()
    {
        // The only softening, and it is not a softening of the replacement rule -- it stops that rule
        // producing a request nobody at all can approve, which is strictly worse than the stalling this
        // whole feature exists to fix.
        var chain = ManagerChain("DG500", out _, out var managerId);
        var leaverId = CreateEmployee("DG500GONE", EmployeeStatus.Inactive);
        CreateDelegation(managerId, leaverId);

        var approvers = Resolve(chain.RequestId, chain.MasterId, chain.DetailId);

        approvers.ShouldBe(new[] { managerId });
        approvers.ShouldNotContain(leaverId);
    }

    // ---------------------------------------------------------------- decision 4: the delegation IS the authority

    [Fact]
    public async Task A_Stand_In_With_No_Approver_Role_Of_Their_Own_Should_Still_Be_Able_To_Approve()
    {
        // Requiring the stand-in to pass the role check as well would make delegation a no-op in exactly the
        // ordinary case, because the reason the step landed on this person is that nobody else holds it.
        var requesterId = CreateEmployee("DG600REQ");
        var roleHolderId = CreateEmployee("DG600ROLE");
        var standInId = CreateEmployee("DG600SUB");

        var roleId = CreateWorkflowRole();
        GiveRole(roleHolderId, roleId);
        CreateDelegation(roleHolderId, standInId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId);
        EnableCatalog(masterId);

        var requestId = CreateDraftRequest(requesterId);
        await _approval.SubmitAsync(requestId, requesterId);

        LiveApproverIds(requestId).ShouldBe(new[] { standInId });

        await _approval.ApproveAsync(requestId, standInId, "Approved while covering.");

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    // ---------------------------------------------------------------- decision 2: the audit names both

    [Fact]
    public async Task The_Approval_Log_Should_Name_The_Stand_In_And_The_Person_They_Stood_In_For()
    {
        // "Sara, on behalf of Ali". ActionById stays the person who actually clicked -- overwriting it would
        // be a lie about who decided -- and the delegator goes into the comment, so the history can still
        // answer "under whose authority" six months later.
        var requesterId = CreateEmployee("DG700REQ");
        var managerId = CreateEmployee("DG700MGR");
        var standInId = CreateEmployee("DG700SUB");
        SetManager(requesterId, managerId);
        CreateDelegation(managerId, standInId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, WorkflowApproverPersonType.PersonManager);
        EnableCatalog(masterId);

        var requestId = CreateDraftRequest(requesterId);
        await _approval.SubmitAsync(requestId, requesterId);
        await _approval.ApproveAsync(requestId, standInId, "Fine by me.");

        var approvalLog = LogsFor(requestId).Single(l => l.ActionType == RequestActionType.Approved);

        approvalLog.ActionById.ShouldBe(standInId);
        approvalLog.RequestActionComment.ShouldContain("Fine by me.");
        approvalLog.RequestActionComment.ShouldContain("DG700MGR");
    }

    [Fact]
    public async Task An_Ordinary_Approval_Should_Not_Be_Annotated_With_Anybody_Else()
    {
        // The other half of the previous test. A comment claiming an approval was made on somebody's behalf
        // when it was not is worse than no annotation at all.
        var requesterId = CreateEmployee("DG710REQ");
        var managerId = CreateEmployee("DG710MGR");
        SetManager(requesterId, managerId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, WorkflowApproverPersonType.PersonManager);
        EnableCatalog(masterId);

        var requestId = CreateDraftRequest(requesterId);
        await _approval.SubmitAsync(requestId, requesterId);
        await _approval.ApproveAsync(requestId, managerId, "Fine by me.");

        var approvalLog = LogsFor(requestId).Single(l => l.ActionType == RequestActionType.Approved);

        approvalLog.ActionById.ShouldBe(managerId);
        approvalLog.RequestActionComment.ShouldBe("Fine by me.");
    }

    [Fact]
    public async Task A_Refusal_Made_Under_Cover_Should_Name_Both_People_Too()
    {
        // A rejection carries at least as much consequence as an approval, and "who killed this" is asked
        // more often, not less.
        var requesterId = CreateEmployee("DG720REQ");
        var managerId = CreateEmployee("DG720MGR");
        var standInId = CreateEmployee("DG720SUB");
        SetManager(requesterId, managerId);
        CreateDelegation(managerId, standInId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, WorkflowApproverPersonType.PersonManager);
        EnableCatalog(masterId);

        var requestId = CreateDraftRequest(requesterId);
        await _approval.SubmitAsync(requestId, requesterId);
        await _approval.RejectAsync(requestId, standInId, "Not this month.");

        var rejectionLog = LogsFor(requestId).Single(l => l.ActionType == RequestActionType.Rejected);

        rejectionLog.ActionById.ShouldBe(standInId);
        rejectionLog.RequestActionComment.ShouldContain("DG720MGR");
    }

    // ---------------------------------------------------------------- escalation

    [Fact]
    public void Escalation_Should_Respect_Cover_Too()
    {
        // An escalation that ignored cover would hand a stalled request to the one category of person
        // guaranteed not to act on it, and restart the escalation clock from zero.
        var requesterId = CreateEmployee("DG800REQ");
        var sittingApproverId = CreateEmployee("DG800SIT");
        var escalationRoleHolderId = CreateEmployee("DG800ESC");
        var standInId = CreateEmployee("DG800SUB");

        var escalateRoleId = CreateWorkflowRole("Escalation");
        GiveRole(escalationRoleHolderId, escalateRoleId);
        CreateDelegation(escalationRoleHolderId, standInId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, shouldEscalate: true, escalateAfterDays: 2, escalateRoleId: escalateRoleId);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, sittingApproverId);
        SetLastActionDaysAgo(requestId, requesterId, 5);

        using (var uow = _unitOfWorkManager.Begin())
        {
            AsyncHelper.RunSync(() => _escalation.EscalateStalledRequestsAsync());
            uow.Complete();
        }

        LiveApproverIds(requestId).ShouldBe(new[] { standInId });
    }

    // ---------------------------------------------------------------- overlapping cover

    [Fact]
    public void The_Most_Recently_Created_Cover_Should_Win_When_Two_Overlap()
    {
        // Nothing in the schema prevents this -- the app services and the workflow handler both refuse an
        // overlap, but imported and pre-rule rows can still collide. The previous reader took FirstOrDefault
        // with no ordering, so who held somebody's authority depended on the query plan. The last thing a
        // person said about their own cover is the thing they meant.
        var chain = ManagerChain("DG900", out _, out var managerId);
        var firstStandInId = CreateEmployee("DG900SUB1");
        var secondStandInId = CreateEmployee("DG900SUB2");

        CreateDelegation(managerId, firstStandInId);
        CreateDelegation(managerId, secondStandInId);

        Resolve(chain.RequestId, chain.MasterId, chain.DetailId).ShouldBe(new[] { secondStandInId });
    }

    // ---------------------------------------------------------------- fallback approver

    [Fact]
    public void Cover_Should_Apply_To_The_Chains_Default_Approver_As_Well()
    {
        // "The fallback is on leave" is the exact circumstance in which a chain's last resort gets used.
        var requesterId = CreateEmployee("DG950REQ");
        var fallbackId = CreateEmployee("DG950FALLBACK");
        var standInId = CreateEmployee("DG950SUB");
        CreateDelegation(fallbackId, standInId);

        var masterId = CreateMaster(defaultApproverId: fallbackId);
        var detailId = AddStep(masterId, 1, WorkflowApproverPersonType.PersonManager);
        var requestId = CreateOpenRequest(requesterId);

        Resolve(requestId, masterId, detailId).ShouldBe(new[] { standInId });
    }
}
