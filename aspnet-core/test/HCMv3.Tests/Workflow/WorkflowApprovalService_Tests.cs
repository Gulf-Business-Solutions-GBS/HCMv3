using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

// ReSharper disable once InconsistentNaming
/// <summary>
/// Unit tests for <see cref="WorkflowApprovalService"/> -- the generic (type-agnostic) approval engine
/// ported from legacy XAF <c>RequestProcessApproval</c>/<c>WorkFlowServices</c>/<c>RequestActionService</c>.
/// Uses the same bespoke Sqlite/FakeRepository harness as the other Workflow app-service tests (see
/// <see cref="WorkflowAppServiceTestBase"/>), since <see cref="WorkflowApprovalService"/> is a plain
/// repository-driven domain service (no ApplicationService/AbpSession dependency), it is constructed
/// directly rather than via <c>CreateService</c>.
/// </summary>
public class WorkflowApprovalService_Tests : WorkflowAppServiceTestBase
{
    private WorkflowApprovalService CreateService()
    {
        return new WorkflowApprovalService(
            FakeRepository.For<WorkflowRequest>(Context),
            FakeRepository.For<WorkFlowApprover>(Context),
            FakeRepository.For<RequestLog>(Context),
            FakeRepository.For<RequestDetail>(Context),
            FakeRepository.For<WFReadLog>(Context),
            FakeRepository.For<Employee>(Context),
            CreateApproverResolver(),
            CreateResolver(),
            CreateHandlerResolver(),
            CreateNotificationDispatcher(),
            CreateStepSequence(),
            CreateEmployeeScopeProvider());
    }

    /// <summary>
    /// The REAL step sequencer, over the same fake <c>RequestDetail</c> repository. It is what decides which
    /// step a submission lands on and which step an approval moves to, which is precisely what the tests
    /// below assert -- a stub would make them pass whatever the engine did.
    /// </summary>
    private IWorkflowStepSequence CreateStepSequence()
    {
        return new WorkflowStepSequence(FakeRepository.For<RequestDetail>(Context));
    }

    /// <summary>
    /// An actor with no linked employee, which is how these tests have always run: they drive the engine by
    /// passing an employee id in, without a session behind it.
    ///
    /// <para>
    /// That is also the state in which the creator-scope rule deliberately does nothing -- there is no
    /// employee to scope from -- so these tests keep exercising the engine's routing alone. The scope rule
    /// itself is covered by <c>WorkflowCreatorScope_Tests</c>, which runs against a real session.
    /// </para>
    /// </summary>
    private static IEmployeeScopeProvider CreateEmployeeScopeProvider()
    {
        var provider = Substitute.For<IEmployeeScopeProvider>();
        provider.GetCurrentEmployeeIdAsync().Returns(Task.FromResult((int?)null));

        return provider;
    }

    /// <summary>
    /// A substitute, unlike the approver and delegation resolvers above.
    ///
    /// <para>
    /// Notifications are deliberately outside what these tests assert: the engine's contract is that they
    /// cannot affect an approval either way, so a real dispatcher here would add a dependency on email
    /// configuration and the notification framework to every test of the routing itself. Who gets told what
    /// is covered by <c>WorkflowNotifications_Tests</c>.
    /// </para>
    /// </summary>
    private static IWorkflowNotificationDispatcher CreateNotificationDispatcher()
    {
        return Substitute.For<IWorkflowNotificationDispatcher>();
    }

    /// <summary>
    /// The REAL delegation resolver, over the same fake repositories and with no delegation rows in them.
    ///
    /// <para>
    /// Stubbing it would be easier and would quietly make these tests unable to notice if substitution ever
    /// started firing when nobody had arranged cover -- which is precisely the regression that would take an
    /// approver's own queue away from them. Delegation's own behaviour is covered by
    /// <c>WorkflowDelegationSubstitution_Tests</c>.
    /// </para>
    /// </summary>
    private IWorkflowDelegationResolver CreateDelegationResolver()
    {
        return new WorkflowDelegationResolver(
            FakeRepository.For<EmployeeDelegation>(Context),
            FakeRepository.For<Employee>(Context));
    }

    /// <summary>
    /// The REAL approver resolver, on the same fake repositories.
    ///
    /// <para>
    /// Substituting it would be easier and wrong: these tests assert that a step lands on the right person,
    /// which is precisely what the resolver decides. A stub would make them pass whatever routing did.
    /// </para>
    /// </summary>
    private IWorkflowApproverResolver CreateApproverResolver()
    {
        // Org-unit-type scoping off, as it is for a tenant that never switched it on: these tests are about
        // the engine, and the scoping rule has its own tests in WorkflowRoutingAndEscalation_Tests.
        var settingsAccessor = Substitute.For<HCMv3.SystemSettings.Services.ISysSettingsAccessor>();
        settingsAccessor.GetWorkflowSettingsAsync()
            .Returns(Task.FromResult(new HCMv3.SystemSettings.ManageWorkflowSettings()));

        return new WorkflowApproverResolver(
            FakeRepository.For<EmployeeWorkflowRole>(Context),
            FakeRepository.For<EmployeeManager>(Context),
            FakeRepository.For<EmployeeOrgAssignment>(Context),
            FakeRepository.For<RequestDetail>(Context),
            FakeRepository.For<RequestLog>(Context),
            Substitute.For<Abp.Domain.Repositories.IRepository<HCMv3.OrganizationManagement.OrganizationUnitType>>(),
            CreateDelegationResolver(),
            settingsAccessor,
            Substitute.For<HCMv3.OrganizationManagement.Services.IOrgHierarchyService>());
    }

    /// <summary>
    /// No handler for any type, which is a supported production state: a transaction can be catalogued and
    /// routed for approval while having no type-specific validation or side effects. These tests then keep
    /// asserting purely on the engine's own behaviour.
    ///
    /// The handler contract itself is covered by <c>LeaveWorkflowTransactionHandler_Tests</c>, and the engine's
    /// interaction with a handler by <c>WorkflowApprovalService_Handler_Tests</c>.
    /// </summary>
    private static IWorkflowTransactionHandlerResolver CreateHandlerResolver()
    {
        var resolver = Substitute.For<IWorkflowTransactionHandlerResolver>();
        resolver.Resolve(Arg.Any<string>()).Returns((IWorkflowTransactionHandler)null);

        return resolver;
    }

    /// <summary>
    /// These tests exercise the approval ENGINE -- step advancing, approver assignment, logging -- not
    /// routing. The resolver is stubbed to the simple <c>RequestMaster.Code == RequestType</c> lookup the
    /// engine used to perform inline, so every scenario below keeps exactly the meaning it had before
    /// routing moved out into <see cref="IRequestMasterResolver"/>.
    ///
    /// The real cascade (workflow group, sub-type, catalog default) is covered by
    /// <c>RequestMasterResolver_Tests</c> instead. Splitting them keeps a routing bug from showing up as
    /// eight confusing failures in the engine's tests.
    /// </summary>
    private IRequestMasterResolver CreateResolver()
    {
        var resolver = Substitute.For<IRequestMasterResolver>();

        resolver.ResolveAsync(Arg.Any<WorkflowRequest>()).Returns(callInfo =>
        {
            var request = callInfo.Arg<WorkflowRequest>();
            var master = Context.RequestMasters.FirstOrDefault(m => m.Code == request.RequestType);

            if (master == null)
            {
                throw new UserFriendlyException("No workflow configuration found for " + request.RequestType);
            }

            return Task.FromResult(master);
        });

        return resolver;
    }

    private int CreateWorkflowRole()
    {
        var role = new WorkflowRole
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = "R1",
            NameEn = "Role",
            NameAr = "Role"
        };
        Context.WorkflowRoles.Add(role);
        Context.SaveChanges();
        return role.Id;
    }

    private void AssignEmployeeToRole(int employeeId, int workflowRoleId)
    {
        Context.EmployeeWorkflowRoles.Add(new EmployeeWorkflowRole
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            WorkflowRoleId = workflowRoleId,
            StartDate = DateTime.Now.Date.AddDays(-1),
            EndDate = DateTime.Now.Date.AddDays(1)
        });
        Context.SaveChanges();
    }

    private int CreateRequestMaster(string code, int steps)
    {
        var master = new RequestMaster
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = code,
            NameEn = code,
            NameAr = code,
            WorkflowSteps = steps
        };
        Context.RequestMasters.Add(master);
        Context.SaveChanges();
        return master.Id;
    }

    private void CreateRequestDetail(int requestMasterId, int step, int workflowRoleId, ApprovalRequire approvalRequire = ApprovalRequire.Single, bool canReject = true, bool canSendBack = true)
    {
        Context.RequestDetails.Add(new RequestDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            RequestMasterId = requestMasterId,
            Step = step,
            WorkflowRoleId = workflowRoleId,
            ApproverPerson = WorkflowApproverPersonType.Person,
            ApprovalRequire = approvalRequire,
            CanReject = canReject,
            CanSendBack = canSendBack
        });
        Context.SaveChanges();
    }

    /// <summary>
    /// A legacy-style CREATOR row: step 1 with no approver at all, carrying only the rule for who may raise
    /// the request. Legacy hid <c>Person</c> from step 2 onward and showed <c>ApproverPerson</c> only from
    /// step 2, so this is what a step 1 was meant to look like.
    /// </summary>
    private void CreateCreatorStep(int requestMasterId, int step = 1, WorkflowPersonScope person = WorkflowPersonScope.None)
    {
        Context.RequestDetails.Add(new RequestDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            RequestMasterId = requestMasterId,
            Step = step,
            ApproverPerson = WorkflowApproverPersonType.None,
            Person = person,
            ApprovalRequire = ApprovalRequire.Single,
            CanReject = true,
            CanSendBack = true
        });
        Context.SaveChanges();
    }

    /// <summary>A step routed to the requester's own line manager rather than to a role.</summary>
    private void CreateManagerStep(int requestMasterId, int step)
    {
        Context.RequestDetails.Add(new RequestDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            RequestMasterId = requestMasterId,
            Step = step,
            ApproverPerson = WorkflowApproverPersonType.PersonManager,
            ApprovalRequire = ApprovalRequire.Single,
            CanReject = true,
            CanSendBack = true
        });
        Context.SaveChanges();
    }

    private void SetManager(int employeeId, int managerId)
    {
        Context.EmployeeManagers.Add(new EmployeeManager
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            ManagerId = managerId,
            StartDate = DateTime.Today.AddYears(-1),
            EndDate = DateTime.Today.AddYears(2)
        });
        Context.SaveChanges();
    }

    private int CreateDraftRequest(int employeeId, string requestType)
    {
        var request = new WorkflowRequest
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            RequestType = requestType,
            RequestNumber = "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            RequestStatus = RequestStatus.Draft,
            WorkflowStep = 0
        };
        Context.WorkflowRequests.Add(request);
        Context.SaveChanges();
        return request.Id;
    }

    [Fact]
    public async Task Should_Submit_And_Assign_Step1_Approver()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("LeaveRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "LeaveRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.WorkflowStep.ShouldBe(1);
        request.ApproverId.ShouldBe(approverId);
    }

    [Fact]
    public async Task Should_Approve_Single_Step_Request_To_Completion()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("LeaveRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "LeaveRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.ApproveAsync(requestId, approverId, "OK");

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Should_Advance_To_Next_Step_On_Approve_When_Not_Last_Step()
    {
        var submitterId = CreateEmployee();
        var step1ApproverId = CreateEmployee();
        var step2ApproverId = CreateEmployee();
        var role1Id = CreateWorkflowRole();
        var role2Id = CreateWorkflowRole();
        AssignEmployeeToRole(step1ApproverId, role1Id);
        AssignEmployeeToRole(step2ApproverId, role2Id);

        var masterId = CreateRequestMaster("LoanRequest", steps: 2);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: role1Id);
        CreateRequestDetail(masterId, step: 2, workflowRoleId: role2Id);

        var requestId = CreateDraftRequest(submitterId, "LoanRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.ApproveAsync(requestId, step1ApproverId, "step1 ok");

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.WorkflowStep.ShouldBe(2);
        request.ApproverId.ShouldBe(step2ApproverId);

        await service.ApproveAsync(requestId, step2ApproverId, "step2 ok");

        request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Should_Reject_Request()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("LeaveRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "LeaveRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.RejectAsync(requestId, approverId, "Not valid");

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Rejected);
    }

    [Fact]
    public async Task Should_SendBack_Request_To_Draft()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("LeaveRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "LeaveRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.SendBackAsync(requestId, approverId, "Please correct dates");

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Draft);
        request.WorkflowStep.ShouldBe(1);
    }

    [Fact]
    public async Task Should_Reassign_Pending_Approval_To_New_Approver()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var newApproverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("LeaveRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "LeaveRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.ReassignAsync(requestId, approverId, newApproverId, "Delegating to you");

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.ApproverId.ShouldBe(newApproverId);

        // The new approver can now approve; the old one no longer can.
        await Should.ThrowAsync<UserFriendlyException>(() => service.ApproveAsync(requestId, approverId, "x"));
        await service.ApproveAsync(requestId, newApproverId, "Approved by delegate");

        request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Should_Throw_When_NonApprover_Tries_To_Approve()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var randomEmployeeId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("LeaveRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "LeaveRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);

        await Should.ThrowAsync<UserFriendlyException>(() => service.ApproveAsync(requestId, randomEmployeeId, "hijack"));
    }

    [Fact]
    public async Task Should_Throw_When_Submitting_Non_Draft_Request()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("LeaveRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "LeaveRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);

        await Should.ThrowAsync<UserFriendlyException>(() => service.SubmitAsync(requestId, submitterId));
    }

    // ------------------------------------------------- step 1 as the CREATOR row (legacy convention)

    [Fact]
    public async Task Submitting_Should_Skip_A_Step_1_That_Names_No_Approver()
    {
        // The legacy convention: step 1 carries WHO MAY RAISE the request and nobody approves it, so the
        // approval chain starts at step 2. Before this, the engine sent every request to step 1 and the
        // creator row resolved no approver at all -- falling through to the default approver, or to the
        // "no approver could be resolved" refusal when the master had none.
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("CreatorRowLeave", steps: 2);
        CreateCreatorStep(masterId, step: 1);
        CreateRequestDetail(masterId, step: 2, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "CreatorRowLeave");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.WorkflowStep.ShouldBe(2);
        request.ApproverId.ShouldBe(approverId);
    }

    [Fact]
    public async Task Submitting_Should_Still_Land_On_Step_1_When_Step_1_Is_The_Approver()
    {
        // The backward-compatibility case, and the one that matters most: LEAVE-STD and MANPOWER-STD are
        // configured in production with a real approver ON step 1 and no step 2 at all. A rule of "start at
        // step 2" would send both hunting for a step that does not exist and strand every request.
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("LegacyShapeLeave", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "LegacyShapeLeave");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.WorkflowStep.ShouldBe(1);
        request.ApproverId.ShouldBe(approverId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task A_Creator_Row_Should_Not_Add_An_Approval_To_The_Chain()
    {
        // 1 = creator, 2 = the requester's manager, 3 = a role. Two approvals, not three: approving at 2
        // advances to 3, and approving at 3 completes -- the creator row is never waited on.
        var submitterId = CreateEmployee();
        var managerId = CreateEmployee();
        var roleApproverId = CreateEmployee();
        SetManager(submitterId, managerId);

        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(roleApproverId, roleId);

        var masterId = CreateRequestMaster("ThreeRowChain", steps: 3);
        CreateCreatorStep(masterId, step: 1);
        CreateManagerStep(masterId, step: 2);
        CreateRequestDetail(masterId, step: 3, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "ThreeRowChain");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.WorkflowStep.ShouldBe(2);
        request.ApproverId.ShouldBe(managerId);

        await service.ApproveAsync(requestId, managerId, "manager ok");

        request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.WorkflowStep.ShouldBe(3);
        request.ApproverId.ShouldBe(roleApproverId);

        await service.ApproveAsync(requestId, roleApproverId, "role ok");

        request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task A_SentBack_Request_Should_Resubmit_Onto_The_First_Approver_Step()
    {
        // Send-back used to reset WorkflowStep to 1. With a creator row at 1 that is a step nobody
        // approves, so the resubmitted request would sit on a row that resolves no approver.
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("CreatorRowSendBack", steps: 2);
        CreateCreatorStep(masterId, step: 1);
        CreateRequestDetail(masterId, step: 2, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "CreatorRowSendBack");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.SendBackAsync(requestId, approverId, "Please correct the dates");

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Draft);
        request.WorkflowStep.ShouldBe(2);

        await service.SubmitAsync(requestId, submitterId);

        request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.WorkflowStep.ShouldBe(2);
        request.ApproverId.ShouldBe(approverId);
    }
}
