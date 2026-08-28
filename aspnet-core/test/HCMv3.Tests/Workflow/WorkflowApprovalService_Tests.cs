using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.PersonnelAdministration;
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
            FakeRepository.For<EmployeeWorkflowRole>(Context),
            FakeRepository.For<EmployeeManager>(Context),
            CreateResolver(),
            CreateHandlerResolver());
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
}
