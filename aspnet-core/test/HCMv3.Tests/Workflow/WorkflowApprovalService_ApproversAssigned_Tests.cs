using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// The engine's "approvers assigned to a step" moment (<see cref="IWorkflowTransactionHandler.OnApproversAssignedAsync"/>):
/// when it fires, with what, and that it is part of the action rather than an observer of it.
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowApprovalService_ApproversAssigned_Tests : WorkflowAppServiceTestBase
{
    private readonly IWorkflowTransactionHandler _handler = Substitute.For<IWorkflowTransactionHandler>();

    private WorkflowApprovalService CreateService()
    {
        var handlerResolver = Substitute.For<IWorkflowTransactionHandlerResolver>();
        handlerResolver.Resolve(Arg.Any<string>()).Returns(_handler);

        var masterResolver = Substitute.For<IRequestMasterResolver>();
        masterResolver.ResolveAsync(Arg.Any<WorkflowRequest>())
            .Returns(call => Task.FromResult(Context.RequestMasters.First(m => m.Code == call.Arg<WorkflowRequest>().RequestType)));

        var settings = Substitute.For<HCMv3.SystemSettings.Services.ISysSettingsAccessor>();
        settings.GetWorkflowSettingsAsync().Returns(Task.FromResult(new HCMv3.SystemSettings.ManageWorkflowSettings()));

        var approverResolver = new WorkflowApproverResolver(
            FakeRepository.For<EmployeeWorkflowRole>(Context),
            FakeRepository.For<EmployeeManager>(Context),
            FakeRepository.For<EmployeeOrgAssignment>(Context),
            FakeRepository.For<RequestDetail>(Context),
            FakeRepository.For<RequestLog>(Context),
            Substitute.For<Abp.Domain.Repositories.IRepository<HCMv3.OrganizationManagement.OrganizationUnitType>>(),
            new WorkflowDelegationResolver(FakeRepository.For<EmployeeDelegation>(Context), FakeRepository.For<Employee>(Context)),
            settings,
            Substitute.For<HCMv3.OrganizationManagement.Services.IOrgHierarchyService>(),
            Substitute.For<Abp.Dependency.IIocResolver>());

        var scope = Substitute.For<IEmployeeScopeProvider>();
        scope.GetCurrentEmployeeIdAsync().Returns(Task.FromResult((int?)null));

        var platform = Substitute.For<IWorkflowPlatformProvider>();
        platform.Current.Returns(WorkflowPlatform.Core);

        var service = new WorkflowApprovalService(
            FakeRepository.For<WorkflowRequest>(Context),
            FakeRepository.For<WorkFlowApprover>(Context),
            FakeRepository.For<RequestLog>(Context),
            FakeRepository.For<RequestDetail>(Context),
            FakeRepository.For<WFReadLog>(Context),
            FakeRepository.For<Employee>(Context),
            approverResolver,
            masterResolver,
            handlerResolver,
            Substitute.For<IWorkflowNotificationDispatcher>(),
            new WorkflowStepSequence(FakeRepository.For<RequestDetail>(Context)),
            scope,
            platform)
        {
            UnitOfWorkManager = CreateFakeUnitOfWorkManager(),
            LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>()
        };

        return service;
    }

    private int RoleHeldBy(int employeeId)
    {
        var role = new WorkflowRole { TenantId = AbpSession.TenantId ?? 1, Code = Guid.NewGuid().ToString("N")[..6], NameEn = "R", NameAr = "R" };
        Context.WorkflowRoles.Add(role);
        Context.SaveChanges();

        Context.EmployeeWorkflowRoles.Add(new EmployeeWorkflowRole
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            WorkflowRoleId = role.Id,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(1)
        });
        Context.SaveChanges();

        return role.Id;
    }

    private void Chain(string code, params int[] roleIdsByStep)
    {
        var master = new RequestMaster { TenantId = AbpSession.TenantId ?? 1, Code = code, NameEn = code, NameAr = code, WorkflowSteps = roleIdsByStep.Length };
        Context.RequestMasters.Add(master);
        Context.SaveChanges();

        for (var i = 0; i < roleIdsByStep.Length; i++)
        {
            Context.RequestDetails.Add(new RequestDetail
            {
                TenantId = AbpSession.TenantId ?? 1,
                RequestMasterId = master.Id,
                Step = i + 1,
                WorkflowRoleId = roleIdsByStep[i],
                ApproverPerson = WorkflowApproverPersonType.Person,
                ApprovalRequire = ApprovalRequire.Single,
                CanReject = true,
                CanSendBack = true
            });
        }

        Context.SaveChanges();
    }

    private int Draft(int employeeId, string requestType)
    {
        var request = new WorkflowRequest
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            RequestType = requestType,
            RequestNumber = Guid.NewGuid().ToString("N")[..8],
            RequestStatus = RequestStatus.Draft
        };
        Context.WorkflowRequests.Add(request);
        Context.SaveChanges();
        return request.Id;
    }

    [Fact]
    public async Task Submitting_Should_Report_The_First_Steps_Approvers()
    {
        var requester = CreateEmployee();
        var approver = CreateEmployee();
        Chain("PAEmployee", RoleHeldBy(approver));
        var requestId = Draft(requester, "PAEmployee");

        await CreateService().SubmitAsync(requestId, requester);

        await _handler.Received(1).OnApproversAssignedAsync(
            Arg.Is<WorkflowTransactionContext>(c => c.Request.Id == requestId),
            1,
            Arg.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { approver })));
    }

    [Fact]
    public async Task Moving_To_The_Next_Step_Should_Report_That_Steps_Approvers()
    {
        var requester = CreateEmployee();
        var first = CreateEmployee();
        var second = CreateEmployee();
        Chain("PAEmployee", RoleHeldBy(first), RoleHeldBy(second));
        var requestId = Draft(requester, "PAEmployee");
        var service = CreateService();

        await service.SubmitAsync(requestId, requester);
        await service.ApproveAsync(requestId, first, "ok");

        await _handler.Received(1).OnApproversAssignedAsync(
            Arg.Any<WorkflowTransactionContext>(),
            2,
            Arg.Is<IReadOnlyList<int>>(ids => ids.SequenceEqual(new[] { second })));
    }

    [Fact]
    public async Task The_Final_Approval_Should_Assign_Nobody()
    {
        var requester = CreateEmployee();
        var approver = CreateEmployee();
        Chain("PAEmployee", RoleHeldBy(approver));
        var requestId = Draft(requester, "PAEmployee");
        var service = CreateService();

        await service.SubmitAsync(requestId, requester);
        _handler.ClearReceivedCalls();

        await service.ApproveAsync(requestId, approver, "ok");

        await _handler.DidNotReceive().OnApproversAssignedAsync(
            Arg.Any<WorkflowTransactionContext>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<int>>());
    }

    [Fact]
    public async Task A_Failure_Should_Fail_The_Submission()
    {
        // Unlike the advisory step observer, this one is part of the action: a step must not open with half
        // its reviewers, so the submission fails with the handler's reason.
        var requester = CreateEmployee();
        var approver = CreateEmployee();
        Chain("PAEmployee", RoleHeldBy(approver));
        var requestId = Draft(requester, "PAEmployee");

        _handler.OnApproversAssignedAsync(Arg.Any<WorkflowTransactionContext>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<int>>())
            .ThrowsAsync(new UserFriendlyException("reviewer limit"));

        var exception = await Should.ThrowAsync<UserFriendlyException>(() => CreateService().SubmitAsync(requestId, requester));
        exception.Message.ShouldBe("reviewer limit");
    }
}
