using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service delegation of duties -- "cover for me while I am away".
///
/// <para>
/// The rule this file exists to protect is the one in
/// <see cref="A_Pending_Delegation_Should_Not_Be_Visible_To_The_Lookup_The_Approval_System_Uses"/>. Unlike
/// most workflow-enabled rows, an <see cref="EmployeeDelegation"/> is read BY the approval machinery itself:
/// <c>EmailService.ResolveActiveDelegate</c> copies the delegate on the delegator's notifications today, and
/// a future approver-substitution branch would read the same table the same way. If a pending delegation
/// were visible to that lookup, asking for cover and being granted cover would be the same event -- somebody
/// would start receiving another employee's approval traffic the instant they were nominated, with nobody
/// having agreed.
/// </para>
///
/// <para>
/// The other rules are legacy <c>DelegationRequest.PerformValidation</c> parity (overlap, end-before-start)
/// plus one addition: delegating to yourself. Legacy permitted it; it produces a row that reads as cover
/// everywhere downstream while handing the delegator's authority back to the delegator.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyDelegationAppService_Tests : AppTestBase
{
    private readonly IMyDelegationAppService _delegations;

    public MyDelegationAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _delegations = Resolve<IMyDelegationAppService>();
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

    /// <summary>Turns workflow on for the delegation type, pointing it at a one-step chain, and returns the
    /// approver's employee id.</summary>
    private int EnableDelegationWorkflow(string approverCode)
    {
        var approverId = CreateEmployee(approverCode);

        UsingDbContext(context =>
        {
            var role = context.Set<WorkflowRole>().Add(new WorkflowRole
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Approver",
                NameAr = "Approver"
            }).Entity;

            var master = context.Set<RequestMaster>().Add(new RequestMaster
            {
                TenantId = TenantId,
                Code = "DEL-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
                NameEn = "Standard",
                NameAr = "Standard",
                WorkflowSteps = 1
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
                CanReject = true,
                CanSendBack = true
            });

            context.Set<EmployeeWorkflowRole>().Add(new EmployeeWorkflowRole
            {
                TenantId = TenantId,
                EmployeeId = approverId,
                WorkflowRoleId = role.Id,
                StartDate = DateTime.Today.AddYears(-1),
                EndDate = DateTime.Today.AddYears(2)
            });

            var type = context.Set<WorkflowTransactionType>()
                .First(t => t.Code == DelegationWorkflowTransactionHandler.DelegationRequestType);
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private static RequestDelegationInput DelegationInput(int delegateToId, DateTime start, DateTime end) =>
        new RequestDelegationInput
        {
            DelegateToId = delegateToId,
            StartDate = start,
            EndDate = end,
            Comments = "Annual leave cover"
        };

    /// <summary>
    /// The lookup the approval system performs: "who is covering for this employee on this date". Shaped
    /// exactly like <c>EmailService.ResolveActiveDelegate</c> -- a repository read inside a unit of work,
    /// which is where ABP data filters live. A raw DbContext query would bypass ApprovedOnly and prove
    /// nothing.
    /// </summary>
    private int? ResolveActiveDelegateAsTheEngineWould(int employeeId, DateTime on)
    {
        var repository = Resolve<IRepository<EmployeeDelegation>>();
        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            return repository.GetAll()
                .Where(d => d.EmployeeId == employeeId && d.StartDate <= on && d.EndDate >= on)
                .Select(d => (int?)d.DelegateToId)
                .FirstOrDefault();
        }
    }

    // ------------------------------------------------- the rule this whole file exists for

    [Fact]
    public async Task A_Pending_Delegation_Should_Not_Be_Visible_To_The_Lookup_The_Approval_System_Uses()
    {
        // THE important one. This table is not merely data about the employee -- it is read by the approval
        // machinery to decide who receives and (in the substitution branch) who may act on another person's
        // requests. A delegation still sitting in an approver's inbox must therefore be worth nothing at
        // all: nominating a colleague cannot be what hands them the authority, or approving it would be
        // ceremonial.
        var meId = CreateCurrentUserEmployee("DEL100");
        var delegateId = CreateEmployee("DEL100DELEGATE");
        EnableDelegationWorkflow("DEL100APP");

        var today = DateTime.Today;

        var submission = await _delegations.RequestDelegation(
            DelegationInput(delegateId, today.AddDays(-1), today.AddDays(5)));

        submission.SubmittedForApproval.ShouldBeTrue(
            "Workflow was not actually on, so this test would pass for the wrong reason.");

        var pending = UsingDbContext(c => c.Set<EmployeeDelegation>().IgnoreQueryFilters()
            .Single(d => d.Id == submission.TransactionId));
        pending.WorkflowStatus.ShouldBe(RequestStatus.Open,
            "The delegation must still be in flight for this test to be testing anything.");

        ResolveActiveDelegateAsTheEngineWould(meId, today).ShouldBeNull(
            "A delegation that nobody has approved yet was treated as active cover. The nominated colleague " +
            "would receive this employee's approval traffic, and would be substitutable for them, purely " +
            "because somebody typed their name.");
    }

    [Fact]
    public async Task An_Approved_Delegation_Should_Be_Visible_To_The_Lookup_The_Approval_System_Uses()
    {
        // The other half, and the reason the first half is not simply "delegation never works". Approval is
        // the single write that makes the cover real: nothing is created, copied or activated anywhere else
        // -- the row merely stops being hidden.
        var meId = CreateCurrentUserEmployee("DEL110");
        var delegateId = CreateEmployee("DEL110DELEGATE");
        var approverId = EnableDelegationWorkflow("DEL110APP");

        var today = DateTime.Today;

        var submission = await _delegations.RequestDelegation(
            DelegationInput(delegateId, today.AddDays(-1), today.AddDays(5)));

        ResolveActiveDelegateAsTheEngineWould(meId, today).ShouldBeNull();

        await Resolve<IWorkflowApprovalService>()
            .ApproveAsync(submission.WorkflowRequestId.Value, approverId, "Agreed");

        var row = UsingDbContext(c => c.Set<EmployeeDelegation>().IgnoreQueryFilters()
            .Single(d => d.Id == submission.TransactionId));
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);

        ResolveActiveDelegateAsTheEngineWould(meId, today).ShouldBe(delegateId,
            "An approved delegation is not being seen by the lookup the approval system uses, so granting " +
            "cover would achieve nothing.");
    }

    [Fact]
    public async Task A_Nominated_Delegate_Should_Not_Be_Able_To_Act_On_The_Delegators_Requests_While_The_Delegation_Is_Pending()
    {
        // Stated at the engine rather than the table, because that is where somebody would feel the bug.
        // Note what this test does NOT assert: that an approved delegation lets the delegate act. Delegation
        // -based approver substitution is deliberately not implemented in this port (see
        // WorkflowApprovalService.EnsureOpenAndCurrentApprover's remarks), so the nominee cannot act either
        // way today. This pins the pending half so that whoever adds substitution has to keep it.
        var requesterId = CreateCurrentUserEmployee("DEL120");
        var delegateId = CreateEmployee("DEL120DELEGATE");
        var approverId = EnableDelegationWorkflow("DEL120APP");

        var today = DateTime.Today;

        var submission = await _delegations.RequestDelegation(
            DelegationInput(delegateId, today.AddDays(-1), today.AddDays(5)));

        // The approver's own inbox item is the delegation request itself; the delegate is not on it.
        await Assert.ThrowsAsync<UserFriendlyException>(() => Resolve<IWorkflowApprovalService>()
            .ApproveAsync(submission.WorkflowRequestId.Value, delegateId, "I am covering"));

        // ...and the real approver still can, so the refusal above was about the delegate and not about the
        // request being unapprovable.
        await Resolve<IWorkflowApprovalService>()
            .ApproveAsync(submission.WorkflowRequestId.Value, approverId, "Agreed");

        ResolveActiveDelegateAsTheEngineWould(requesterId, today).ShouldBe(delegateId);
    }

    // ------------------------------------------------- legacy PerformValidation parity

    [Fact]
    public async Task A_Second_Delegation_Overlapping_The_First_Should_Be_Refused_While_The_First_Is_Still_Pending()
    {
        // Legacy DelegationRequest.PerformValidation lines 169-174, with the part legacy never had to think
        // about: the ApprovedOnly filter hides the in-flight first delegation, so a guard that did not
        // disable it would accept this second one silently -- and two colleagues would each end up believing
        // they held the same authority for the same days.
        CreateCurrentUserEmployee("DEL200");
        var firstDelegate = CreateEmployee("DEL200D1");
        var secondDelegate = CreateEmployee("DEL200D2");
        EnableDelegationWorkflow("DEL200APP");

        var start = DateTime.Today.AddDays(10);

        var first = await _delegations.RequestDelegation(
            DelegationInput(firstDelegate, start, start.AddDays(10)));

        var pending = UsingDbContext(c => c.Set<EmployeeDelegation>().IgnoreQueryFilters()
            .Single(d => d.Id == first.TransactionId));
        pending.WorkflowStatus.ShouldBe(RequestStatus.Open);

        await Assert.ThrowsAsync<UserFriendlyException>(() => _delegations.RequestDelegation(
            DelegationInput(secondDelegate, start.AddDays(5), start.AddDays(15))));
    }

    [Fact]
    public async Task A_Second_Delegation_Overlapping_One_Already_Recorded_Should_Be_Refused()
    {
        // The HR-entered case: WorkflowStatus null, i.e. effective immediately. Still a conflict.
        var meId = CreateCurrentUserEmployee("DEL210");
        var otherDelegate = CreateEmployee("DEL210D");
        EnableDelegationWorkflow("DEL210APP");

        var start = DateTime.Today.AddDays(20);

        UsingDbContext(context =>
        {
            context.Set<EmployeeDelegation>().Add(new EmployeeDelegation
            {
                TenantId = TenantId,
                EmployeeId = meId,
                DelegateToId = otherDelegate,
                StartDate = start,
                EndDate = start.AddDays(5)
            });
        });

        var newDelegate = CreateEmployee("DEL210D2");

        await Assert.ThrowsAsync<UserFriendlyException>(() => _delegations.RequestDelegation(
            DelegationInput(newDelegate, start.AddDays(3), start.AddDays(9))));
    }

    [Fact]
    public async Task A_Delegation_For_Dates_That_Do_Not_Overlap_Should_Be_Allowed()
    {
        // The guard is about overlapping days, not about ever having delegated before. Two separate trips
        // are ordinary.
        CreateCurrentUserEmployee("DEL220");
        var firstDelegate = CreateEmployee("DEL220D1");
        var secondDelegate = CreateEmployee("DEL220D2");
        EnableDelegationWorkflow("DEL220APP");

        var start = DateTime.Today.AddDays(30);

        await _delegations.RequestDelegation(DelegationInput(firstDelegate, start, start.AddDays(4)));

        var second = await _delegations.RequestDelegation(
            DelegationInput(secondDelegate, start.AddDays(10), start.AddDays(14)));

        second.SubmittedForApproval.ShouldBeTrue();
    }

    [Fact]
    public async Task Somebody_Elses_Delegation_Over_The_Same_Dates_Should_Not_Block_Mine()
    {
        // Scoped by delegator: a colleague away the same fortnight is not a conflict.
        CreateCurrentUserEmployee("DEL230");
        var otherEmployee = CreateEmployee("DEL230OTHER");
        var theirDelegate = CreateEmployee("DEL230THEIRD");
        var myDelegate = CreateEmployee("DEL230MYD");
        EnableDelegationWorkflow("DEL230APP");

        var start = DateTime.Today.AddDays(40);

        UsingDbContext(context =>
        {
            context.Set<EmployeeDelegation>().Add(new EmployeeDelegation
            {
                TenantId = TenantId,
                EmployeeId = otherEmployee,
                DelegateToId = theirDelegate,
                StartDate = start,
                EndDate = start.AddDays(5)
            });
        });

        var mine = await _delegations.RequestDelegation(
            DelegationInput(myDelegate, start, start.AddDays(5)));

        mine.SubmittedForApproval.ShouldBeTrue();
    }

    [Fact]
    public async Task Delegating_To_Yourself_Should_Be_Refused()
    {
        // Not a legacy rule -- legacy allowed it. A self-delegation reads as cover to everything downstream
        // while handing the delegator's authority back to the delegator, so requests would sit waiting on
        // somebody who is away. An outage that looks like cover is worse than no cover, because nobody goes
        // looking for it.
        var meId = CreateCurrentUserEmployee("DEL300");
        EnableDelegationWorkflow("DEL300APP");

        await Assert.ThrowsAsync<UserFriendlyException>(() => _delegations.RequestDelegation(
            DelegationInput(meId, DateTime.Today, DateTime.Today.AddDays(3))));
    }

    [Fact]
    public void The_Handler_Should_Refuse_A_Self_Delegation_Whoever_Raised_It()
    {
        // Driven through the handler directly, because the service-level check above only guards the ESS
        // form. The rule has to hold for an HR-raised delegation and for an import too, so it is tested
        // where it actually lives.
        var meId = CreateCurrentUserEmployee("DEL310");

        var delegationId = 0;
        var requestId = 0;

        UsingDbContext(context =>
        {
            var delegation = context.Set<EmployeeDelegation>().Add(new EmployeeDelegation
            {
                TenantId = TenantId,
                EmployeeId = meId,
                DelegateToId = meId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(3),
                WorkflowStatus = RequestStatus.Draft
            }).Entity;

            context.SaveChanges();
            delegationId = delegation.Id;

            var request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = meId,
                RequestType = DelegationWorkflowTransactionHandler.DelegationRequestType,
                TargetEntityId = delegationId,
                RequestNumber = "DEL-TEST-00001",
                RequestStatus = RequestStatus.Draft,
                WorkflowStep = 0
            }).Entity;

            context.SaveChanges();
            requestId = request.Id;
        });

        var handler = Resolve<IWorkflowTransactionHandlerResolver>()
            .Resolve(DelegationWorkflowTransactionHandler.DelegationRequestType);

        handler.ShouldNotBeNull("The delegation type has no handler, so it would route with no validation.");

        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (var uow = unitOfWorkManager.Begin())
        {
            var request = Resolve<IRepository<WorkflowRequest>>().Get(requestId);
            var context = new WorkflowTransactionContext(request, meId);

            Should.Throw<UserFriendlyException>(() => handler.ValidateBeforeSubmitAsync(context));

            uow.Complete();
        }
    }

    [Fact]
    public async Task A_Delegation_Ending_Before_It_Starts_Should_Be_Refused()
    {
        CreateCurrentUserEmployee("DEL320");
        var delegateId = CreateEmployee("DEL320D");
        EnableDelegationWorkflow("DEL320APP");

        await Assert.ThrowsAsync<UserFriendlyException>(() => _delegations.RequestDelegation(
            DelegationInput(delegateId, DateTime.Today.AddDays(10), DateTime.Today.AddDays(3))));
    }

    // ------------------------------------------------- the shared contract

    [Fact]
    public async Task A_Submitted_Delegation_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        // The single-table design's premise: the pending row lives in the real EmployeeDelegations table, so
        // it must be the ApprovedOnly filter -- not a separate DelegationRequest table -- that keeps it out
        // of everything else.
        CreateCurrentUserEmployee("DEL400");
        var delegateId = CreateEmployee("DEL400D");
        EnableDelegationWorkflow("DEL400APP");

        var result = await _delegations.RequestDelegation(
            DelegationInput(delegateId, DateTime.Today.AddDays(2), DateTime.Today.AddDays(6)));

        var repository = Resolve<IRepository<EmployeeDelegation>>();
        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            repository.GetAll().Any(d => d.Id == result.TransactionId)
                .ShouldBeFalse("A pending delegation was visible to the rest of the system.");
        }

        // But the delegator sees their own, and it is not yet marked active.
        var mine = await _delegations.GetMyDelegations(ListInput());
        var row = mine.Items.Single(i => i.Id == result.TransactionId);
        row.DelegateToId.ShouldBe(delegateId);
        row.IsCurrentlyActive.ShouldBeFalse();
        row.CanCancel.ShouldBeTrue();
    }

    [Fact]
    public async Task A_Delegation_Should_Be_Attributed_To_The_Signed_In_Employee_And_Numbered_With_Its_Own_Prefix()
    {
        // There is no delegator field on the input at all, deliberately -- a self-service form must not be
        // able to give away somebody else's authority.
        var meId = CreateCurrentUserEmployee("DEL410");
        CreateEmployee("DEL410OTHER");
        var delegateId = CreateEmployee("DEL410D");
        EnableDelegationWorkflow("DEL410APP");

        var result = await _delegations.RequestDelegation(
            DelegationInput(delegateId, DateTime.Today.AddDays(3), DateTime.Today.AddDays(7)));

        var row = UsingDbContext(c => c.Set<EmployeeDelegation>().IgnoreQueryFilters()
            .Single(d => d.Id == result.TransactionId));

        row.EmployeeId.ShouldBe(meId);
        row.DelegateToId.ShouldBe(delegateId);

        var request = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.Id == result.WorkflowRequestId.Value));
        request.RequestNumber.ShouldStartWith("DEL-" + DateTime.Today.Year);
        request.SummaryEn.ShouldNotBeNullOrWhiteSpace();
        request.SummaryEn.Contains("DEL410D T").ShouldBeTrue(
            "The summary does not name the delegate, so the approver is being asked to hand over authority " +
            "without being shown to whom. Summary was: " + request.SummaryEn);
    }

    [Fact]
    public async Task Should_Record_Immediately_When_Workflow_Is_Off_For_The_Type()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install: the delegation
        // is effective at once and no request is raised -- exactly how EmployeeDelegation behaved before it
        // was routed at all.
        CreateCurrentUserEmployee("DEL420");
        var delegateId = CreateEmployee("DEL420D");

        var result = await _delegations.RequestDelegation(
            DelegationInput(delegateId, DateTime.Today.AddDays(1), DateTime.Today.AddDays(4)));

        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();

        var row = UsingDbContext(c => c.Set<EmployeeDelegation>().IgnoreQueryFilters()
            .Single(d => d.Id == result.TransactionId));
        row.WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_List_Somebody_Elses_Delegations()
    {
        CreateCurrentUserEmployee("DEL430");
        var otherId = CreateEmployee("DEL430OTHER");
        var theirDelegate = CreateEmployee("DEL430D");

        UsingDbContext(context =>
        {
            context.Set<EmployeeDelegation>().Add(new EmployeeDelegation
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                DelegateToId = theirDelegate,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(3)
            });
        });

        var mine = await _delegations.GetMyDelegations(ListInput());

        mine.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked to an employee, so this is the state the shell must handle rather
        // than an edge case.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _delegations.GetMyDelegations(ListInput()));

        var delegateId = CreateEmployee("DEL500D");

        await Assert.ThrowsAsync<UserFriendlyException>(() => _delegations.RequestDelegation(
            DelegationInput(delegateId, DateTime.Today, DateTime.Today.AddDays(2))));
    }
}
