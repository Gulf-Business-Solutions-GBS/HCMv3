using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Uow;
using Abp.Threading;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.OrganizationManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Covers the configuration the engine used to ignore: manager-level routing, org scoping, signatory steps,
/// required comments, transfers, read logs, escalation and reminders.
///
/// <para>
/// Every one of these had a column on <c>RequestDetail</c> that an administrator could set and nothing would
/// read. That is a worse failure than an absent feature: the setup screen says escalation is on, so when a
/// request sits for three weeks nobody thinks to look at the engine. These tests exist to make sure the
/// columns keep meaning what the screen says they mean.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowRoutingAndEscalation_Tests : AppTestBase
{
    private readonly IWorkflowApproverResolver _resolver;
    private readonly IWorkflowEscalationService _escalation;
    private readonly IWorkflowApprovalService _approval;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public WorkflowRoutingAndEscalation_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _resolver = Resolve<IWorkflowApproverResolver>();
        _escalation = Resolve<IWorkflowEscalationService>();
        _approval = Resolve<IWorkflowApprovalService>();
        _unitOfWorkManager = Resolve<IUnitOfWorkManager>();
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

    private int CreateCostCentre(string name)
    {
        CostCenter costCentre = null;
        UsingDbContext(context =>
        {
            costCentre = context.Set<CostCenter>().Add(new CostCenter
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = name,
                NameAr = name
            }).Entity;
        });

        return costCentre.Id;
    }

    private void AssignCostCentre(int employeeId, int costCentreId)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2),
                CostCenterId = costCentreId
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
        WorkflowRoleGroup roleGroup = WorkflowRoleGroup.None,
        bool signatoryStep = false,
        bool commentRequired = false,
        bool shouldEscalate = false,
        int escalateAfterDays = 0,
        bool escalateNextLevel = false,
        int? escalateRoleId = null,
        int reminderDays = 0,
        bool enableReadLog = false)
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
                WorkflowRoleGroup = roleGroup,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true,
                SignatoryStep = signatoryStep,
                IsCommentRequire = commentRequired,
                ShouldEscalate = shouldEscalate,
                EscalateAfterDays = escalateAfterDays,
                EscalateNextLevel = escalateNextLevel,
                EscalateWorkflowRoleId = escalateRoleId,
                ReminderDays = reminderDays,
                EnableReadLog = enableReadLog
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

    private int CreateOpenRequest(int requesterId, int step = 1, string requestType = "EmployeeAbsence")
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
                RequestStatus = RequestStatus.Open,
                WorkflowStep = step,
                SummaryEn = "Something",
                SummaryAr = "Something"
            }).Entity;
        });

        return request.Id;
    }

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

    /// <summary>Backdates the request's last action, which is the clock escalation and reminders read.</summary>
    private void SetLastActionDaysAgo(int requestId, int actorId, int days, int step = 1)
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
                WorkflowStep = step,
                Platform = WorkflowPlatform.Core
            });
        });
    }

    /// <summary>
    /// Runs escalation inside a unit of work, which is where ABP repositories and data filters live -- the
    /// same wrapping <c>WorkflowEscalationWorker</c> does in production. Calling the service bare would hit a
    /// disposed context and prove nothing about the rule under test.
    /// </summary>
    private List<WorkflowEscalationResult> RunEscalation()
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var results = AsyncHelper.RunSync(() => _escalation.EscalateStalledRequestsAsync());
            uow.Complete();

            return results;
        }
    }

    private List<WorkflowReminderDue> RunReminders()
    {
        using (var uow = _unitOfWorkManager.Begin())
        {
            var due = AsyncHelper.RunSync(() => _escalation.GetRemindersDueAsync());
            uow.Complete();

            return due;
        }
    }

    /// <summary>
    /// The approver rows that still count.
    ///
    /// <para>
    /// Replacing approvers SOFT-deletes the old rows -- WorkFlowApprover is a FullAuditedEntity -- so the
    /// superseded ones are still physically present. Repository queries filter them out, but a raw context
    /// read does not, and asserting over both would report an escalation as having assigned two approvers.
    /// </para>
    /// </summary>
    private List<WorkFlowApprover> LiveApprovers(int requestId) =>
        UsingDbContext(c => c.Set<WorkFlowApprover>()
            .Where(a => a.WorkflowRequestId == requestId && !a.IsDeleted)
            .ToList());

    private WorkflowRequest RequestRow(int id) =>
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == id));

    private RequestDetail DetailRow(int id) =>
        UsingDbContext(c => c.Set<RequestDetail>().Single(d => d.Id == id));

    private RequestMaster MasterRow(int id) =>
        UsingDbContext(c => c.Set<RequestMaster>().Single(m => m.Id == id));

    // ---------------------------------------------------------------- manager-level routing

    [Fact]
    public void Should_Route_To_The_Direct_Manager()
    {
        var employeeId = CreateEmployee("RT100");
        var managerId = CreateEmployee("RT100MGR");
        SetManager(employeeId, managerId);

        var masterId = CreateMaster();
        var detailId = AddStep(masterId, 1, WorkflowApproverPersonType.PersonManager);
        var requestId = CreateOpenRequest(employeeId);

        using (_unitOfWorkManager.Begin())
        {
            var approvers = _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(detailId));
            approvers.ShouldBe(new[] { managerId });
        }
    }

    [Fact]
    public void Should_Route_Two_Levels_Up_The_Reporting_Line()
    {
        // ManagerLevel2..7 were configurable and unreachable: only the direct manager ever resolved, so a
        // step set to "skip to the second level" silently fell through to the default approver.
        var employeeId = CreateEmployee("RT110");
        var managerId = CreateEmployee("RT110MGR");
        var grandManagerId = CreateEmployee("RT110GRAND");

        SetManager(employeeId, managerId);
        SetManager(managerId, grandManagerId);

        var masterId = CreateMaster();
        var detailId = AddStep(masterId, 1, WorkflowApproverPersonType.ManagerLevel2);
        var requestId = CreateOpenRequest(employeeId);

        using (_unitOfWorkManager.Begin())
        {
            var approvers = _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(detailId));
            approvers.ShouldBe(new[] { grandManagerId });
        }
    }

    [Fact]
    public void A_Chain_Too_Short_Should_Fall_To_The_Default_Approver_Not_The_Top_Manager()
    {
        // Substituting whoever happens to be highest would route to somebody the configuration never
        // nominated, and they would have no way of knowing why it reached them.
        var employeeId = CreateEmployee("RT120");
        var managerId = CreateEmployee("RT120MGR");
        var fallbackId = CreateEmployee("RT120FALLBACK");

        SetManager(employeeId, managerId);

        var masterId = CreateMaster(defaultApproverId: fallbackId);
        var detailId = AddStep(masterId, 1, WorkflowApproverPersonType.ManagerLevel3);
        var requestId = CreateOpenRequest(employeeId);

        using (_unitOfWorkManager.Begin())
        {
            var approvers = _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(detailId));
            approvers.ShouldBe(new[] { fallbackId });
        }
    }

    [Fact]
    public void A_Cycle_In_The_Reporting_Line_Should_Not_Hang_The_Resolver()
    {
        // A reports to B reports to A is a data-entry mistake, not an impossibility.
        var aId = CreateEmployee("RT130A");
        var bId = CreateEmployee("RT130B");
        var fallbackId = CreateEmployee("RT130FALLBACK");

        SetManager(aId, bId);
        SetManager(bId, aId);

        var masterId = CreateMaster(defaultApproverId: fallbackId);
        var detailId = AddStep(masterId, 1, WorkflowApproverPersonType.ManagerLevel4);
        var requestId = CreateOpenRequest(aId);

        using (_unitOfWorkManager.Begin())
        {
            var approvers = _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(detailId));
            approvers.ShouldBe(new[] { fallbackId });
        }
    }

    // ---------------------------------------------------------------- org scoping

    [Fact]
    public void Should_Narrow_Role_Holders_To_The_Requesters_Cost_Centre()
    {
        var costCentreA = CreateCostCentre("A");
        var costCentreB = CreateCostCentre("B");

        var requesterId = CreateEmployee("RT200REQ");
        var sameCentreApproverId = CreateEmployee("RT200SAME");
        var otherCentreApproverId = CreateEmployee("RT200OTHER");

        AssignCostCentre(requesterId, costCentreA);
        AssignCostCentre(sameCentreApproverId, costCentreA);
        AssignCostCentre(otherCentreApproverId, costCentreB);

        var roleId = CreateWorkflowRole();
        GiveRole(sameCentreApproverId, roleId);
        GiveRole(otherCentreApproverId, roleId);

        var masterId = CreateMaster();
        var detailId = AddStep(masterId, 1, roleId: roleId, roleGroup: WorkflowRoleGroup.CostCenter);
        var requestId = CreateOpenRequest(requesterId);

        using (_unitOfWorkManager.Begin())
        {
            var approvers = _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(detailId));

            approvers.ShouldContain(sameCentreApproverId);
            approvers.ShouldNotContain(otherCentreApproverId);
        }
    }

    [Fact]
    public void Narrowing_To_Nobody_Should_Widen_Rather_Than_Stall_The_Request()
    {
        // If no one in the requester's cost centre holds the role, routing to the wider group beats a
        // request that cannot move at all. An approver can always reassign; a stuck request needs somebody
        // to notice it first.
        var costCentreA = CreateCostCentre("A");
        var costCentreB = CreateCostCentre("B");

        var requesterId = CreateEmployee("RT210REQ");
        var elsewhereId = CreateEmployee("RT210ELSEWHERE");

        AssignCostCentre(requesterId, costCentreA);
        AssignCostCentre(elsewhereId, costCentreB);

        var roleId = CreateWorkflowRole();
        GiveRole(elsewhereId, roleId);

        var masterId = CreateMaster();
        var detailId = AddStep(masterId, 1, roleId: roleId, roleGroup: WorkflowRoleGroup.CostCenter);
        var requestId = CreateOpenRequest(requesterId);

        using (_unitOfWorkManager.Begin())
        {
            var approvers = _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(detailId));
            approvers.ShouldBe(new[] { elsewhereId });
        }
    }

    [Fact]
    public void Company_Scope_Should_Not_Narrow_Anything()
    {
        // In HCMv3 a company IS a tenant, and every query is already tenant-scoped.
        var requesterId = CreateEmployee("RT220REQ");
        var approverId = CreateEmployee("RT220APP");

        AssignCostCentre(requesterId, CreateCostCentre("A"));
        AssignCostCentre(approverId, CreateCostCentre("B"));

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        var detailId = AddStep(masterId, 1, roleId: roleId, roleGroup: WorkflowRoleGroup.Company);
        var requestId = CreateOpenRequest(requesterId);

        using (_unitOfWorkManager.Begin())
        {
            var approvers = _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(detailId));
            approvers.ShouldBe(new[] { approverId });
        }
    }

    // ---------------------------------------------------------------- signatory steps

    [Fact]
    public void A_Signatory_Step_Should_Exclude_Whoever_Signed_An_Earlier_One()
    {
        // The point of a multi-signatory chain is that DIFFERENT people put their name to the document. One
        // person holding both roles would otherwise satisfy both steps by clicking twice.
        var requesterId = CreateEmployee("RT300REQ");
        var bothRolesId = CreateEmployee("RT300BOTH");
        var freshSignatoryId = CreateEmployee("RT300FRESH");

        var roleId = CreateWorkflowRole();
        GiveRole(bothRolesId, roleId);
        GiveRole(freshSignatoryId, roleId);

        var masterId = CreateMaster(steps: 2);
        AddStep(masterId, 1, roleId: roleId, signatoryStep: true);
        var step2Id = AddStep(masterId, 2, roleId: roleId, signatoryStep: true);

        var requestId = CreateOpenRequest(requesterId, step: 2);

        // Step 1 was signed by the person who holds both roles.
        UsingDbContext(context =>
        {
            context.Set<RequestLog>().Add(new RequestLog
            {
                TenantId = TenantId,
                WorkflowRequestId = requestId,
                ActionById = bothRolesId,
                ActionDateTime = DateTime.Now.AddDays(-1),
                ActionType = RequestActionType.Approved,
                WorkflowStep = 1,
                Platform = WorkflowPlatform.Core
            });
        });

        using (_unitOfWorkManager.Begin())
        {
            var approvers = _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(step2Id));

            approvers.ShouldNotContain(bothRolesId);
            approvers.ShouldContain(freshSignatoryId);
        }
    }

    [Fact]
    public void A_Non_Signatory_Step_Should_Not_Exclude_Previous_Approvers()
    {
        // Ordinary chains legitimately route back to the same person; only signatory steps demand a
        // different pen.
        var requesterId = CreateEmployee("RT310REQ");
        var approverId = CreateEmployee("RT310APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster(steps: 2);
        AddStep(masterId, 1, roleId: roleId);
        var step2Id = AddStep(masterId, 2, roleId: roleId);

        var requestId = CreateOpenRequest(requesterId, step: 2);
        SetLastActionDaysAgo(requestId, approverId, days: 1);

        using (_unitOfWorkManager.Begin())
        {
            var approvers = _resolver.ResolveForStep(RequestRow(requestId), MasterRow(masterId), DetailRow(step2Id));
            approvers.ShouldContain(approverId);
        }
    }

    // ---------------------------------------------------------------- required comment

    [Fact]
    public async Task A_Step_That_Requires_A_Comment_Should_Refuse_A_Bare_Approval()
    {
        var requesterId = CreateEmployee("RT400REQ");
        var approverId = CreateEmployee("RT400APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, commentRequired: true);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _approval.ApproveAsync(requestId, approverId, remark: "  "));
    }

    [Fact]
    public async Task Refusing_For_A_Missing_Comment_Should_Not_Half_Approve_The_Step()
    {
        // Checked before any approver row is marked. Otherwise this employee is recorded as having acted on
        // a request that did not move, and can never act on it again.
        var requesterId = CreateEmployee("RT410REQ");
        var approverId = CreateEmployee("RT410APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, commentRequired: true);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _approval.ApproveAsync(requestId, approverId, remark: null));

        var stillPending = UsingDbContext(c => c.Set<WorkFlowApprover>()
            .Single(a => a.WorkflowRequestId == requestId && a.ApproverId == approverId));

        stillPending.IsApproved.ShouldBeFalse();
        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task A_Step_That_Requires_A_Comment_Should_Accept_One()
    {
        var requesterId = CreateEmployee("RT420REQ");
        var approverId = CreateEmployee("RT420APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, commentRequired: true);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);

        await _approval.ApproveAsync(requestId, approverId, remark: "Checked with finance.");

        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    // ---------------------------------------------------------------- transfer

    [Fact]
    public async Task Reassigning_Should_Log_A_Transfer_Not_An_Assignment()
    {
        // Assigned is an ad-hoc feedback request that leaves the approver in place; Transferred hands the
        // decision itself to somebody else. Logging them alike makes the history unable to answer who was
        // responsible for the step.
        var requesterId = CreateEmployee("RT500REQ");
        var approverId = CreateEmployee("RT500APP");
        var newApproverId = CreateEmployee("RT500NEW");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);

        await _approval.ReassignAsync(requestId, approverId, newApproverId, "Away next week");

        var logged = UsingDbContext(c => c.Set<RequestLog>()
            .Where(l => l.WorkflowRequestId == requestId && l.ActionType == RequestActionType.Transferred)
            .ToList());

        logged.Count.ShouldBe(1);
        logged.Single().RequestActionComment.ShouldBe("Away next week");

        var approverRow = LiveApprovers(requestId).Single();
        approverRow.ApproverId.ShouldBe(newApproverId);
    }

    // ---------------------------------------------------------------- read log

    [Fact]
    public async Task Reading_Should_Be_Logged_Only_Where_The_Step_Asks_For_It()
    {
        var requesterId = CreateEmployee("RT600REQ");
        var approverId = CreateEmployee("RT600APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, enableReadLog: true);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);

        await _approval.MarkAsReadAsync(requestId, approverId);

        UsingDbContext(c => c.Set<WFReadLog>().Count(l => l.WorkflowRequestId == requestId)).ShouldBe(1);
    }

    [Fact]
    public async Task Reading_Should_Not_Be_Logged_When_The_Step_Does_Not_Ask()
    {
        // Logging every view of every request would bury the answer this feature exists to give.
        var requesterId = CreateEmployee("RT610REQ");
        var approverId = CreateEmployee("RT610APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, enableReadLog: false);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);

        await _approval.MarkAsReadAsync(requestId, approverId);

        UsingDbContext(c => c.Set<WFReadLog>().Count(l => l.WorkflowRequestId == requestId)).ShouldBe(0);
    }

    [Fact]
    public async Task Reading_Twice_Should_Record_One_Read()
    {
        // One row per approver per step answers "did they look at it". Repeat views add nothing.
        var requesterId = CreateEmployee("RT620REQ");
        var approverId = CreateEmployee("RT620APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, enableReadLog: true);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);

        await _approval.MarkAsReadAsync(requestId, approverId);
        await _approval.MarkAsReadAsync(requestId, approverId);

        UsingDbContext(c => c.Set<WFReadLog>().Count(l => l.WorkflowRequestId == requestId)).ShouldBe(1);
    }

    // ---------------------------------------------------------------- escalation

    [Fact]
    public async Task A_Stalled_Step_Should_Escalate_To_The_Next_Level()
    {
        var requesterId = CreateEmployee("RT700REQ");
        var slowApproverId = CreateEmployee("RT700SLOW");
        var nextLevelId = CreateEmployee("RT700NEXT");

        var step1Role = CreateWorkflowRole("Step1");
        var step2Role = CreateWorkflowRole("Step2");
        GiveRole(slowApproverId, step1Role);
        GiveRole(nextLevelId, step2Role);

        var masterId = CreateMaster(steps: 2);
        AddStep(masterId, 1, roleId: step1Role, shouldEscalate: true, escalateAfterDays: 3, escalateNextLevel: true);
        AddStep(masterId, 2, roleId: step2Role);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, slowApproverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 5);

        var results = RunEscalation();

        results.Count.ShouldBe(1);
        results.Single().MovedToNextLevel.ShouldBeTrue();

        var request = RequestRow(requestId);
        request.WorkflowStep.ShouldBe(2);

        // Still Open. Escalation moves a request along; only a human ever completes it.
        request.RequestStatus.ShouldBe(RequestStatus.Open);

        var approverRow = LiveApprovers(requestId).Single();
        approverRow.ApproverId.ShouldBe(nextLevelId);
    }

    [Fact]
    public async Task A_Stalled_Step_Should_Escalate_Sideways_When_Not_Configured_To_Advance()
    {
        var requesterId = CreateEmployee("RT710REQ");
        var slowApproverId = CreateEmployee("RT710SLOW");
        var escalationTargetId = CreateEmployee("RT710TARGET");

        var stepRole = CreateWorkflowRole("Step");
        var escalateRole = CreateWorkflowRole("Escalate");
        GiveRole(slowApproverId, stepRole);
        GiveRole(escalationTargetId, escalateRole);

        var masterId = CreateMaster(steps: 2);
        AddStep(masterId, 1, roleId: stepRole, shouldEscalate: true, escalateAfterDays: 2,
            escalateNextLevel: false, escalateRoleId: escalateRole);
        AddStep(masterId, 2, roleId: stepRole);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, slowApproverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 4);

        var results = RunEscalation();

        results.Count.ShouldBe(1);
        results.Single().MovedToNextLevel.ShouldBeFalse();

        // The step did NOT advance -- the decision still needs making, just by somebody else.
        RequestRow(requestId).WorkflowStep.ShouldBe(1);

        var approverRow = LiveApprovers(requestId).Single();
        approverRow.ApproverId.ShouldBe(escalationTargetId);
    }

    [Fact]
    public async Task A_Step_Inside_Its_Deadline_Should_Not_Escalate()
    {
        var requesterId = CreateEmployee("RT720REQ");
        var approverId = CreateEmployee("RT720APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster(steps: 2);
        AddStep(masterId, 1, roleId: roleId, shouldEscalate: true, escalateAfterDays: 5, escalateNextLevel: true);
        AddStep(masterId, 2, roleId: roleId);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 2);

        var results = RunEscalation();

        results.ShouldBeEmpty();
        RequestRow(requestId).WorkflowStep.ShouldBe(1);
    }

    [Fact]
    public async Task Escalation_Should_Never_Approve_A_Request_On_Its_Final_Step()
    {
        // Approval by timeout is the one outcome escalation must never produce -- it would turn an
        // unstaffed inbox into silent blanket approval.
        var requesterId = CreateEmployee("RT730REQ");
        var approverId = CreateEmployee("RT730APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster(steps: 1);
        AddStep(masterId, 1, roleId: roleId, shouldEscalate: true, escalateAfterDays: 1, escalateNextLevel: true);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 10);

        var results = RunEscalation();

        results.ShouldBeEmpty();
        RequestRow(requestId).RequestStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task Sideways_Escalation_Should_Leave_Approvers_Alone_When_Nobody_Can_Be_Resolved()
    {
        // Clearing them would leave the request assigned to nobody at all, which is worse than a slow
        // approver.
        var requesterId = CreateEmployee("RT740REQ");
        var slowApproverId = CreateEmployee("RT740SLOW");

        var stepRole = CreateWorkflowRole("Step");
        var emptyEscalateRole = CreateWorkflowRole("EscalateNobody");
        GiveRole(slowApproverId, stepRole);

        var masterId = CreateMaster(steps: 2);
        AddStep(masterId, 1, roleId: stepRole, shouldEscalate: true, escalateAfterDays: 1,
            escalateNextLevel: false, escalateRoleId: emptyEscalateRole);
        AddStep(masterId, 2, roleId: stepRole);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, slowApproverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 9);

        var results = RunEscalation();

        results.ShouldBeEmpty();

        var approverRow = LiveApprovers(requestId).Single();
        approverRow.ApproverId.ShouldBe(slowApproverId);
    }

    [Fact]
    public async Task Escalation_Should_Not_Land_Back_On_The_Approver_Who_Has_Not_Acted()
    {
        // Escalating to the person already sitting on it is exactly what the feature exists to avoid.
        var requesterId = CreateEmployee("RT750REQ");
        var slowApproverId = CreateEmployee("RT750SLOW");
        var otherHolderId = CreateEmployee("RT750OTHER");

        var stepRole = CreateWorkflowRole("Step");
        var escalateRole = CreateWorkflowRole("Escalate");
        GiveRole(slowApproverId, stepRole);

        // The slow approver ALSO holds the escalation role.
        GiveRole(slowApproverId, escalateRole);
        GiveRole(otherHolderId, escalateRole);

        var masterId = CreateMaster(steps: 2);
        AddStep(masterId, 1, roleId: stepRole, shouldEscalate: true, escalateAfterDays: 1,
            escalateNextLevel: false, escalateRoleId: escalateRole);
        AddStep(masterId, 2, roleId: stepRole);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, slowApproverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 6);

        RunEscalation();

        var approverIds = LiveApprovers(requestId).Select(a => a.ApproverId).ToList();

        approverIds.ShouldNotContain(slowApproverId);
        approverIds.ShouldContain(otherHolderId);
    }

    [Fact]
    public async Task Escalation_Should_Be_Logged_Against_The_Step_It_Left()
    {
        var requesterId = CreateEmployee("RT760REQ");
        var slowApproverId = CreateEmployee("RT760SLOW");
        var nextLevelId = CreateEmployee("RT760NEXT");

        var step1Role = CreateWorkflowRole("Step1");
        var step2Role = CreateWorkflowRole("Step2");
        GiveRole(slowApproverId, step1Role);
        GiveRole(nextLevelId, step2Role);

        var masterId = CreateMaster(steps: 2);
        AddStep(masterId, 1, roleId: step1Role, shouldEscalate: true, escalateAfterDays: 1, escalateNextLevel: true);
        AddStep(masterId, 2, roleId: step2Role);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, slowApproverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 4);

        RunEscalation();

        var log = UsingDbContext(c => c.Set<RequestLog>()
            .Single(l => l.WorkflowRequestId == requestId && l.ActionType == RequestActionType.Escalated));

        // Against step 1, the step that stalled -- not step 2, which would make the new approver look like
        // they did something.
        log.WorkflowStep.ShouldBe(1);
    }

    // ---------------------------------------------------------------- reminders

    [Fact]
    public async Task An_Approver_Sitting_Past_The_Reminder_Interval_Should_Be_Due_A_Reminder()
    {
        var requesterId = CreateEmployee("RT800REQ");
        var approverId = CreateEmployee("RT800APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, reminderDays: 2);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 4);

        var due = RunReminders();

        var reminder = due.Single(d => d.WorkflowRequestId == requestId);
        reminder.PendingDays.ShouldBe(4);
        reminder.ApproverEmployeeIds.ShouldContain(approverId);
    }

    [Fact]
    public async Task Reminders_Should_Repeat_On_The_Interval_Not_Every_Day()
    {
        // A daily nag past day one is how people learn to filter these to a folder they never open.
        var requesterId = CreateEmployee("RT810REQ");
        var approverId = CreateEmployee("RT810APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, reminderDays: 3);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);

        // Day 4 of a 3-day interval is not a reminder day; day 3 and day 6 are.
        SetLastActionDaysAgo(requestId, requesterId, days: 4);

        var due = RunReminders();

        due.Any(d => d.WorkflowRequestId == requestId).ShouldBeFalse();
    }

    [Fact]
    public async Task A_Step_Inside_Its_Reminder_Interval_Should_Not_Be_Due()
    {
        var requesterId = CreateEmployee("RT820REQ");
        var approverId = CreateEmployee("RT820APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, reminderDays: 5);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 2);

        var due = RunReminders();

        due.Any(d => d.WorkflowRequestId == requestId).ShouldBeFalse();
    }

    [Fact]
    public async Task A_Step_With_No_Reminder_Configured_Should_Never_Be_Due()
    {
        var requesterId = CreateEmployee("RT830REQ");
        var approverId = CreateEmployee("RT830APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster();
        AddStep(masterId, 1, roleId: roleId, reminderDays: 0);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 90);

        var due = RunReminders();

        due.Any(d => d.WorkflowRequestId == requestId).ShouldBeFalse();
    }

    [Fact]
    public async Task A_Decided_Request_Should_Neither_Escalate_Nor_Remind()
    {
        var requesterId = CreateEmployee("RT840REQ");
        var approverId = CreateEmployee("RT840APP");

        var roleId = CreateWorkflowRole();
        GiveRole(approverId, roleId);

        var masterId = CreateMaster(steps: 2);
        AddStep(masterId, 1, roleId: roleId, shouldEscalate: true, escalateAfterDays: 1,
            escalateNextLevel: true, reminderDays: 1);
        AddStep(masterId, 2, roleId: roleId);
        EnableCatalog(masterId);

        var requestId = CreateOpenRequest(requesterId);
        SetApprover(requestId, approverId);
        SetLastActionDaysAgo(requestId, requesterId, days: 30);

        UsingDbContext(context =>
        {
            context.Set<WorkflowRequest>().Single(r => r.Id == requestId).RequestStatus = RequestStatus.Approved;
        });

        (RunEscalation()).ShouldBeEmpty();
        (RunReminders()).Any(d => d.WorkflowRequestId == requestId).ShouldBeFalse();
    }
}
