using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using Abp.Localization;
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
        var service = BuildService();

        // The engine flushes the unit of work before dispatching notifications, so the approvers it has just
        // inserted are visible to the dispatcher's query rather than being tracked entities nobody can see.
        // Constructed with new(), it gets none of the property injection the container would do, so this has
        // to be handed over explicitly -- otherwise every test here dies on CurrentUnitOfWork.
        service.UnitOfWorkManager = CreateFakeUnitOfWorkManager();
        service.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();

        return service;
    }

    private WorkflowApprovalService BuildService()
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
            CreateEmployeeScopeProvider(),
            CreatePlatformProvider());
    }

    /// <summary>
    /// Reports whatever <see cref="_platform"/> currently holds, so a test can say "this action arrived
    /// from the mobile app" without an HTTP request to carry the header.
    /// </summary>
    private IWorkflowPlatformProvider CreatePlatformProvider()
    {
        var provider = Substitute.For<IWorkflowPlatformProvider>();
        provider.Current.Returns(_ => _platform);

        return provider;
    }

    /// <summary>
    /// The platform the next action will be recorded against. Core by default, which is what a test with
    /// no HTTP context behind it would really see.
    /// </summary>
    private WorkflowPlatform _platform = WorkflowPlatform.Core;

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

    /// <summary>
    /// Legacy AssignNewApprover only offered employees not already approving the step. Handing it to one of
    /// them would merge two approvers into one and lose a vote, so the server refuses it.
    /// </summary>
    [Fact]
    public async Task Should_Refuse_Reassigning_To_Someone_Already_Approving_The_Step()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var otherApproverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);
        AssignEmployeeToRole(otherApproverId, roleId);

        var masterId = CreateRequestMaster("LeaveRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "LeaveRequest");
        var service = CreateService();
        await service.SubmitAsync(requestId, submitterId);

        await Should.ThrowAsync<UserFriendlyException>(
            () => service.ReassignAsync(requestId, approverId, otherApproverId, "to a colleague already on it"));
        await Should.ThrowAsync<UserFriendlyException>(
            () => service.ReassignAsync(requestId, approverId, approverId, "to myself"));
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

    // ---------------------------------------------------------------- the two columns the log never filled

    /// <summary>
    /// Both columns existed on the entity from the port and neither was ever given a real value:
    /// <c>ActionDays</c> was written as 0 and <c>Platform</c> as Core at every site. A column that always
    /// says the same thing is worse than a missing one, because a report will quote it.
    /// </summary>
    [Fact]
    public async Task Should_Record_The_Platform_An_Action_Came_From()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("PlatformRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "PlatformRequest");
        var service = CreateService();

        // Raised on the phone, approved at a desk -- which is the ordinary case, and the one a single
        // hardcoded value cannot represent.
        _platform = WorkflowPlatform.Mobile;
        await service.SubmitAsync(requestId, submitterId);

        _platform = WorkflowPlatform.ESS;
        await service.ApproveAsync(requestId, approverId, "fine");

        var logs = Context.RequestLogs
            .Where(l => l.WorkflowRequestId == requestId)
            .OrderBy(l => l.Id)
            .ToList();

        logs.First(l => l.ActionType == RequestActionType.Created).Platform.ShouldBe(WorkflowPlatform.Mobile);
        logs.First(l => l.ActionType == RequestActionType.Approved).Platform.ShouldBe(WorkflowPlatform.ESS);
    }

    /// <summary>
    /// The first action has nothing before it, so it is zero rather than "days since the epoch".
    /// </summary>
    [Fact]
    public async Task Should_Record_Zero_Action_Days_For_The_First_Action()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("FirstActionRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "FirstActionRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);

        var created = Context.RequestLogs.Single(l => l.WorkflowRequestId == requestId);
        created.ActionType.ShouldBe(RequestActionType.Created);
        created.ActionDays.ShouldBe(0);
    }

    /// <summary>
    /// Measured from the PREVIOUS action, not from submission.
    ///
    /// <para>
    /// This is the distinction the column exists to make. Backdating the step-1 approval by four days and
    /// leaving submission six days back means an implementation measuring from the start would say 6 for
    /// step 2; measuring from the step it actually waited on says 4. The wrong one makes every later
    /// approver inherit the delays of everyone before them.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Should_Count_Action_Days_From_The_Previous_Action_Not_From_Submission()
    {
        var submitterId = CreateEmployee();
        var step1ApproverId = CreateEmployee();
        var step2ApproverId = CreateEmployee();
        var role1Id = CreateWorkflowRole();
        var role2Id = CreateWorkflowRole();
        AssignEmployeeToRole(step1ApproverId, role1Id);
        AssignEmployeeToRole(step2ApproverId, role2Id);

        var masterId = CreateRequestMaster("ActionDaysRequest", steps: 2);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: role1Id);
        CreateRequestDetail(masterId, step: 2, workflowRoleId: role2Id);

        var requestId = CreateDraftRequest(submitterId, "ActionDaysRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.ApproveAsync(requestId, step1ApproverId, "step1 ok");

        // Age the two rows that exist, so the step-2 approval lands four days after step 1 and six after
        // submission. Done to the stored rows because the engine stamps DateTime.Now and a test cannot
        // wait four days for it.
        var created = Context.RequestLogs.Single(l => l.WorkflowRequestId == requestId && l.ActionType == RequestActionType.Created);
        var step1 = Context.RequestLogs.Single(l => l.WorkflowRequestId == requestId && l.ActionType == RequestActionType.Approved);

        created.ActionDateTime = DateTime.Now.AddDays(-6);
        step1.ActionDateTime = DateTime.Now.AddDays(-4);
        Context.SaveChanges();

        await service.ApproveAsync(requestId, step2ApproverId, "step2 ok");

        var step2 = Context.RequestLogs
            .Where(l => l.WorkflowRequestId == requestId && l.ActionType == RequestActionType.Approved)
            .OrderByDescending(l => l.Id)
            .First();

        step2.ActionDays.ShouldBe(4);
    }

    // ---------------------------------------------------------------- approver rows survive a step change

    /// <summary>
    /// The defect this column exists to fix: the engine used to delete every approver row on a request
    /// before inserting the next step's, so the moment a request reached step 2 there was nothing left
    /// saying who had been entitled to approve step 1, nor that they had.
    /// </summary>
    [Fact]
    public async Task Should_Keep_Past_Steps_Approver_Rows_After_Advancing()
    {
        var submitterId = CreateEmployee();
        var step1ApproverId = CreateEmployee();
        var step2ApproverId = CreateEmployee();
        var role1Id = CreateWorkflowRole();
        var role2Id = CreateWorkflowRole();
        AssignEmployeeToRole(step1ApproverId, role1Id);
        AssignEmployeeToRole(step2ApproverId, role2Id);

        var masterId = CreateRequestMaster("KeepRowsRequest", steps: 2);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: role1Id);
        CreateRequestDetail(masterId, step: 2, workflowRoleId: role2Id);

        var requestId = CreateDraftRequest(submitterId, "KeepRowsRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.ApproveAsync(requestId, step1ApproverId, "step1 ok");

        // !IsDeleted throughout: WorkFlowApprover is a FullAuditedEntity, so the engine soft-deletes. The
        // application only ever sees live rows because ABP's repositories filter them; this bare test
        // DbContext has no such filter and would otherwise assert against rows nothing can read.
        var rows = Context.WorkFlowApprovers
            .Where(a => a.WorkflowRequestId == requestId && !a.IsDeleted)
            .ToList();

        // Step 1's row is still there, still saying who approved it.
        var step1Row = rows.SingleOrDefault(a => a.WorkflowStep == 1);
        step1Row.ShouldNotBeNull("Step 1's approver row must survive the move to step 2.");
        step1Row.ApproverId.ShouldBe(step1ApproverId);
        step1Row.IsApproved.ShouldBeTrue();

        // And step 2 has its own, pending.
        var step2Row = rows.SingleOrDefault(a => a.WorkflowStep == 2);
        step2Row.ShouldNotBeNull();
        step2Row.ApproverId.ShouldBe(step2ApproverId);
        step2Row.IsApproved.ShouldBeFalse();
    }

    /// <summary>
    /// Keeping the rows must not let their owners act again.
    ///
    /// <para>
    /// The step-1 approver has a row on this request forever now. Without the step filter on the
    /// current-approver check, that row would make them a valid approver of step 2 as well -- and of every
    /// step after it. This is the failure mode that makes "just stop deleting" wrong on its own.
    /// </para>
    /// </summary>
    [Fact]
    public async Task Should_Not_Let_A_Past_Step_Approver_Act_On_The_Current_Step()
    {
        var submitterId = CreateEmployee();
        var step1ApproverId = CreateEmployee();
        var step2ApproverId = CreateEmployee();
        var role1Id = CreateWorkflowRole();
        var role2Id = CreateWorkflowRole();
        AssignEmployeeToRole(step1ApproverId, role1Id);
        AssignEmployeeToRole(step2ApproverId, role2Id);

        var masterId = CreateRequestMaster("PastApproverRequest", steps: 2);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: role1Id);
        CreateRequestDetail(masterId, step: 2, workflowRoleId: role2Id);

        var requestId = CreateDraftRequest(submitterId, "PastApproverRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.ApproveAsync(requestId, step1ApproverId, "step1 ok");

        await Should.ThrowAsync<UserFriendlyException>(
            () => service.ApproveAsync(requestId, step1ApproverId, "and step 2 as well"));

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.WorkflowStep.ShouldBe(2);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
    }

    /// <summary>
    /// A step can be assigned twice -- send back, then re-submit -- and must not end up with two sets of
    /// approvers. Two rows where ApprovalRequire.All applies would silently double what the step requires.
    /// </summary>
    [Fact]
    public async Task Should_Supersede_Rather_Than_Duplicate_When_A_Step_Is_Assigned_Again()
    {
        var submitterId = CreateEmployee();
        var approverId = CreateEmployee();
        var roleId = CreateWorkflowRole();
        AssignEmployeeToRole(approverId, roleId);

        var masterId = CreateRequestMaster("ResubmitRequest", steps: 1);
        CreateRequestDetail(masterId, step: 1, workflowRoleId: roleId);

        var requestId = CreateDraftRequest(submitterId, "ResubmitRequest");
        var service = CreateService();

        await service.SubmitAsync(requestId, submitterId);
        await service.SendBackAsync(requestId, approverId, "Please correct the dates");
        await service.SubmitAsync(requestId, submitterId);

        var rows = Context.WorkFlowApprovers
            .Where(a => a.WorkflowRequestId == requestId && a.WorkflowStep == 1 && !a.IsDeleted)
            .ToList();

        rows.Count.ShouldBe(1, "Re-assigning a step must replace its rows, not add a second set.");
        rows[0].IsApproved.ShouldBeFalse();

        // Nothing asserted about soft-deletion here. WorkFlowApprover is a FullAuditedEntity, so a real
        // Abp repository keeps the superseded row with IsDeleted set, but this harness removes it outright
        // -- and a test that asserted the harness's behaviour would be pinning the double, not the engine.
        // What matters either way is the count the application can see, which is what is checked above.
    }
}
