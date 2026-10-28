using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Localization;
using Abp.UI;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// A "Person on Request" step: the approver is whoever the request names in one of its fields -- for a leave,
/// the responsibility delegate. The step goes to that person, and a request that names nobody (or names the
/// requester) passes straight over it instead of falling to the chain's default approver.
///
/// <para>
/// The chain used throughout is the realistic one: creator, then the delegate, then the requester's manager.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class WorkflowApprovalService_PersonOnRequest_Tests : WorkflowAppServiceTestBase
{
    private const string RequestType = "LeaveRequest";

    private const string PrimaryDelegateField = "PrimaryDelegateId";
    private const string SecondaryDelegateField = "SecondaryDelegateId";
    private const string ApproverDelegateField = "ApproverDelegateId";

    private int? _delegateOnRequest;

    private WorkflowApprovalService CreateService()
    {
        var service = new WorkflowApprovalService(
            FakeRepository.For<WorkflowRequest>(Context),
            FakeRepository.For<WorkFlowApprover>(Context),
            FakeRepository.For<RequestLog>(Context),
            FakeRepository.For<RequestDetail>(Context),
            FakeRepository.For<WFReadLog>(Context),
            FakeRepository.For<Employee>(Context),
            CreateApproverResolver(),
            CreateMasterResolver(),
            CreateHandlerResolver(),
            Substitute.For<IWorkflowNotificationDispatcher>(),
            new WorkflowStepSequence(FakeRepository.For<RequestDetail>(Context)),
            CreateEmployeeScopeProvider(),
            Substitute.For<IWorkflowPlatformProvider>());

        service.UnitOfWorkManager = CreateFakeUnitOfWorkManager();
        service.LocalizationManager = LocalIocManager.Resolve<ILocalizationManager>();

        return service;
    }

    private WorkflowApproverResolver CreateApproverResolver()
    {
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
            new WorkflowDelegationResolver(
                FakeRepository.For<EmployeeDelegation>(Context),
                FakeRepository.For<Employee>(Context)),
            settingsAccessor,
            Substitute.For<HCMv3.OrganizationManagement.Services.IOrgHierarchyService>(),
            IocResolving(CreateHandlerResolver()));
    }

    /// <summary>The container, as the resolver sees it: it looks the handlers up on first use.</summary>
    private static Abp.Dependency.IIocResolver IocResolving(IWorkflowTransactionHandlerResolver handlerResolver)
    {
        var iocResolver = Substitute.For<Abp.Dependency.IIocResolver>();
        iocResolver.Resolve<IWorkflowTransactionHandlerResolver>().Returns(handlerResolver);

        return iocResolver;
    }

    /// <summary>A leave handler that names <see cref="_delegateOnRequest"/> as the responsibility delegate.</summary>
    private IWorkflowTransactionHandlerResolver CreateHandlerResolver()
    {
        var handler = Substitute.For<IWorkflowTransactionHandler>();
        handler.RequestType.Returns(RequestType);
        handler.RequestPersonFields.Returns(new[] { PrimaryDelegateField });
        handler.GetRequestPersonAsync(Arg.Any<WorkflowTransactionContext>(), PrimaryDelegateField)
            .Returns(_ => Task.FromResult(_delegateOnRequest));
        handler.GetRequestPersonAsync(Arg.Any<WorkflowTransactionContext>(), SecondaryDelegateField)
            .Returns(Task.FromResult<int?>(null));

        var resolver = Substitute.For<IWorkflowTransactionHandlerResolver>();
        resolver.Resolve(RequestType).Returns(handler);

        return resolver;
    }

    private IRequestMasterResolver CreateMasterResolver()
    {
        var resolver = Substitute.For<IRequestMasterResolver>();
        resolver.ResolveAsync(Arg.Any<WorkflowRequest>())
            .Returns(callInfo => Task.FromResult(
                Context.RequestMasters.First(m => m.Code == callInfo.Arg<WorkflowRequest>().RequestType)));

        return resolver;
    }

    private static IEmployeeScopeProvider CreateEmployeeScopeProvider()
    {
        var provider = Substitute.For<IEmployeeScopeProvider>();
        provider.GetCurrentEmployeeIdAsync().Returns(Task.FromResult((int?)null));

        return provider;
    }

    private int CreateChain(string requestPersonField = PrimaryDelegateField, int? defaultApproverId = null)
    {
        var master = new RequestMaster
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = RequestType,
            NameEn = RequestType,
            NameAr = RequestType,
            WorkflowSteps = 3,
            DefaultApproverId = defaultApproverId
        };
        Context.RequestMasters.Add(master);
        Context.SaveChanges();

        AddStep(master.Id, 1, WorkflowApproverPersonType.None);
        AddStep(master.Id, 2, WorkflowApproverPersonType.PersonOnRequest, requestPersonField);
        AddStep(master.Id, 3, WorkflowApproverPersonType.PersonManager);

        return master.Id;
    }

    private void AddStep(int masterId, int step, WorkflowApproverPersonType approverPerson, string requestPersonField = null)
    {
        Context.RequestDetails.Add(new RequestDetail
        {
            TenantId = AbpSession.TenantId ?? 1,
            RequestMasterId = masterId,
            Step = step,
            ApproverPerson = approverPerson,
            RequestPersonField = requestPersonField == null || requestPersonField.Contains(':')
                ? requestPersonField
                : WorkflowRequestPersonFields.Compose(RequestType, requestPersonField),
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

    private int CreateDraftRequest(int employeeId)
    {
        var request = new WorkflowRequest
        {
            TenantId = AbpSession.TenantId ?? 1,
            EmployeeId = employeeId,
            RequestType = RequestType,
            RequestNumber = "REQ-" + Guid.NewGuid().ToString("N").Substring(0, 8),
            RequestStatus = RequestStatus.Draft,
            WorkflowStep = 0
        };
        Context.WorkflowRequests.Add(request);
        Context.SaveChanges();

        return request.Id;
    }

    [Fact]
    public async Task Should_Send_The_Request_To_The_Delegate_Named_On_It()
    {
        var requesterId = CreateEmployee();
        var delegateId = CreateEmployee();
        SetManager(requesterId, CreateEmployee());
        CreateChain();
        _delegateOnRequest = delegateId;

        var requestId = CreateDraftRequest(requesterId);
        await CreateService().SubmitAsync(requestId, requesterId);

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.WorkflowStep.ShouldBe(2);
        request.ApproverId.ShouldBe(delegateId);
    }

    [Fact]
    public async Task Should_Move_On_To_The_Manager_Once_The_Delegate_Approves()
    {
        var requesterId = CreateEmployee();
        var delegateId = CreateEmployee();
        var managerId = CreateEmployee();
        SetManager(requesterId, managerId);
        CreateChain();
        _delegateOnRequest = delegateId;

        var requestId = CreateDraftRequest(requesterId);
        var service = CreateService();
        await service.SubmitAsync(requestId, requesterId);

        await service.ApproveAsync(requestId, delegateId, "I will cover");

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.WorkflowStep.ShouldBe(3);
        request.ApproverId.ShouldBe(managerId);
    }

    [Fact]
    public async Task Should_Reject_The_Whole_Request_When_The_Delegate_Rejects()
    {
        var requesterId = CreateEmployee();
        var delegateId = CreateEmployee();
        SetManager(requesterId, CreateEmployee());
        CreateChain();
        _delegateOnRequest = delegateId;

        var requestId = CreateDraftRequest(requesterId);
        var service = CreateService();
        await service.SubmitAsync(requestId, requesterId);

        await service.RejectAsync(requestId, delegateId, "Cannot cover those dates");

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Rejected);
    }

    [Fact]
    public async Task Should_Skip_The_Step_When_The_Request_Names_No_Delegate()
    {
        var requesterId = CreateEmployee();
        var managerId = CreateEmployee();
        SetManager(requesterId, managerId);

        // A default approver is configured, and must NOT be used: an empty field means nobody has a handover
        // to accept, not that the chain's catch-all should approve one.
        CreateChain(defaultApproverId: CreateEmployee());
        _delegateOnRequest = null;

        var requestId = CreateDraftRequest(requesterId);
        await CreateService().SubmitAsync(requestId, requesterId);

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.WorkflowStep.ShouldBe(3);
        request.ApproverId.ShouldBe(managerId);
    }

    [Fact]
    public async Task Should_Skip_The_Step_When_The_Requester_Names_Themselves()
    {
        var requesterId = CreateEmployee();
        var managerId = CreateEmployee();
        SetManager(requesterId, managerId);
        CreateChain();
        _delegateOnRequest = requesterId;

        var requestId = CreateDraftRequest(requesterId);
        await CreateService().SubmitAsync(requestId, requesterId);

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.WorkflowStep.ShouldBe(3);
        request.ApproverId.ShouldBe(managerId);
    }

    [Fact]
    public async Task Should_Complete_The_Request_When_The_Skipped_Step_Was_The_Last()
    {
        var requesterId = CreateEmployee();
        var managerId = CreateEmployee();
        SetManager(requesterId, managerId);

        var master = new RequestMaster
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = RequestType,
            NameEn = RequestType,
            NameAr = RequestType,
            WorkflowSteps = 2
        };
        Context.RequestMasters.Add(master);
        Context.SaveChanges();
        AddStep(master.Id, 1, WorkflowApproverPersonType.PersonManager);
        AddStep(master.Id, 2, WorkflowApproverPersonType.PersonOnRequest, PrimaryDelegateField);
        _delegateOnRequest = null;

        var requestId = CreateDraftRequest(requesterId);
        var service = CreateService();
        await service.SubmitAsync(requestId, requesterId);
        await service.ApproveAsync(requestId, managerId, "OK");

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task Should_Refuse_Submission_When_Every_Step_Is_Skipped()
    {
        var requesterId = CreateEmployee();

        var master = new RequestMaster
        {
            TenantId = AbpSession.TenantId ?? 1,
            Code = RequestType,
            NameEn = RequestType,
            NameAr = RequestType,
            WorkflowSteps = 1
        };
        Context.RequestMasters.Add(master);
        Context.SaveChanges();
        AddStep(master.Id, 1, WorkflowApproverPersonType.PersonOnRequest, PrimaryDelegateField);
        _delegateOnRequest = null;

        var requestId = CreateDraftRequest(requesterId);

        // Nobody at all would approve it: refused, never waved through as approved.
        await Should.ThrowAsync<UserFriendlyException>(() => CreateService().SubmitAsync(requestId, requesterId));
    }

    [Fact]
    public async Task Should_Skip_The_Step_When_The_Type_Does_Not_Fill_The_Chosen_Field()
    {
        var requesterId = CreateEmployee();
        var managerId = CreateEmployee();
        SetManager(requesterId, managerId);
        CreateChain(requestPersonField: ApproverDelegateField);
        _delegateOnRequest = CreateEmployee();

        var requestId = CreateDraftRequest(requesterId);
        await CreateService().SubmitAsync(requestId, requesterId);

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.WorkflowStep.ShouldBe(3);
        request.ApproverId.ShouldBe(managerId);
    }

    [Fact]
    public async Task Should_Skip_A_Step_That_Names_Another_Transaction_Types_Field()
    {
        var requesterId = CreateEmployee();
        var managerId = CreateEmployee();
        SetManager(requesterId, managerId);

        // A shared chain whose step routes to the passport withdrawal's guarantor: a leave has no guarantor.
        CreateChain(requestPersonField: WorkflowRequestPersonFields.Compose("EmployeeDocumentMvmt", "GuarantorId"));
        _delegateOnRequest = CreateEmployee();

        var requestId = CreateDraftRequest(requesterId);
        await CreateService().SubmitAsync(requestId, requesterId);

        var request = await Context.WorkflowRequests.FindAsync(requestId);
        request.WorkflowStep.ShouldBe(3);
        request.ApproverId.ShouldBe(managerId);
    }
}
