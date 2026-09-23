using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Runtime.Validation;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.GovtRelationsManagement.Workflow;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service exit permits.
///
/// <para>
/// The rules worth protecting here are the two that are specific to this type, plus the shared contract that
/// must not have been bypassed. Specific: a permit record with no government permit number is unverifiable
/// and must be refused, and a second permit for the same employee on the same exit date is a resubmission
/// rather than a second trip. Shared: the row is hidden until approved, and a user with no employee record
/// is refused.
/// </para>
///
/// <para>
/// The duplicate test deliberately files its second permit while the first is still <em>pending</em>. That is
/// the case that would slip through a naive check: the <c>ApprovedOnly</c> filter hides an in-flight row, so
/// a guard that did not opt out of it would happily accept the duplicate exactly when a resubmission is most
/// likely.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyExitPermitAppService_Tests : AppTestBase
{
    private readonly IMyExitPermitAppService _exitPermits;

    public MyExitPermitAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _exitPermits = Resolve<IMyExitPermitAppService>();
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

    /// <summary>Turns workflow on for the exit permit type, pointing it at a one-step chain with a real approver.</summary>
    private void EnableExitPermitWorkflow(string approverCode)
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
                Code = "EXP-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
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

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeExitPermit");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private static RequestExitPermitInput PermitInput(string number, DateTime exitDate) => new RequestExitPermitInput
    {
        ExternalRequestNumber = number,
        TravelReason = TravelReason.PlannedLeave,
        ExitDate = exitDate,
        StartDate = exitDate,
        Comments = "Visiting family"
    };

    // ---------------------------------------------------------------- the permit number

    [Fact]
    public async Task A_Permit_With_No_Number_Should_Be_Refused()
    {
        // The number is the government's, not ours. Without it the record points at no document and an
        // approver is being asked to sign off something nobody can check.
        CreateCurrentUserEmployee("EXP100");

        await Assert.ThrowsAsync<AbpValidationException>(() =>
            _exitPermits.RequestExitPermit(PermitInput(null, DateTime.Today.AddDays(7))));
    }

    [Fact]
    public async Task A_Permit_Numbered_With_Blanks_Should_Be_Refused()
    {
        // AbpValidationException, not UserFriendlyException: ABP's method-invocation validator normalises
        // the argument before the [Required] check, so whitespace is refused before the service is entered
        // -- the same reason the attendance-comment test expects this type. The service-level and handler
        // checks behind it are defence in depth for any caller that bypasses DTO validation (the workflow
        // engine calling the handler directly, for one), not the gate that normally fires.
        CreateCurrentUserEmployee("EXP110");

        await Assert.ThrowsAsync<AbpValidationException>(() =>
            _exitPermits.RequestExitPermit(PermitInput("   ", DateTime.Today.AddDays(7))));
    }

    [Fact]
    public void The_Handler_Should_Refuse_A_Permit_With_No_Number_Whoever_Raised_It()
    {
        // Driven through the handler directly, because the DTO annotation above only guards the ESS form.
        // The rule has to hold for anything that reaches the engine -- an HR-entered row later submitted for
        // approval, or an import -- so it is tested where it actually lives.
        var meId = CreateCurrentUserEmployee("EXP120");

        var permitId = 0;
        var requestId = 0;

        UsingDbContext(context =>
        {
            var permit = context.Set<EmployeeExitPermit>().Add(new EmployeeExitPermit
            {
                TenantId = TenantId,
                EmployeeId = meId,
                ExternalRequestNumber = "   ",
                TravelReason = TravelReason.PlannedLeave,
                ExitDate = DateTime.Today.AddDays(8),
                StartDate = DateTime.Today.AddDays(8),
                WorkflowStatus = RequestStatus.Draft
            }).Entity;

            context.SaveChanges();
            permitId = permit.Id;

            var request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = meId,
                RequestType = ExitPermitWorkflowTransactionHandler.ExitPermitRequestType,
                TargetEntityId = permitId,
                RequestNumber = "EXP-TEST-00001",
                RequestStatus = RequestStatus.Draft,
                WorkflowStep = 0
            }).Entity;

            context.SaveChanges();
            requestId = request.Id;
        });

        var handler = Resolve<IWorkflowTransactionHandlerResolver>()
            .Resolve(ExitPermitWorkflowTransactionHandler.ExitPermitRequestType);

        handler.ShouldNotBeNull("The exit permit type has no handler, so it would route with no validation.");

        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (var uow = unitOfWorkManager.Begin())
        {
            var request = Resolve<Abp.Domain.Repositories.IRepository<WorkflowRequest>>().Get(requestId);
            var context = new WorkflowTransactionContext(request, meId);

            Should.Throw<UserFriendlyException>(() => handler.ValidateBeforeSubmitAsync(context));

            uow.Complete();
        }
    }

    // ---------------------------------------------------------------- the duplicate guard

    [Fact]
    public async Task A_Second_Permit_For_The_Same_Exit_Date_Should_Be_Refused_While_The_First_Is_Still_Pending()
    {
        // THE important one. The first permit is Open, which the ApprovedOnly filter hides -- so a guard
        // that did not disable that filter would accept this duplicate silently, and the employee would end
        // up with two permits in two approvers' inboxes for the same trip.
        CreateCurrentUserEmployee("EXP200");
        EnableExitPermitWorkflow("EXP200APP");

        var exitDate = DateTime.Today.AddDays(10);

        var first = await _exitPermits.RequestExitPermit(PermitInput("KW-11111", exitDate));
        first.SubmittedForApproval.ShouldBeTrue();

        var pending = UsingDbContext(c => c.Set<EmployeeExitPermit>().IgnoreQueryFilters()
            .Single(p => p.Id == first.TransactionId));
        pending.WorkflowStatus.ShouldBe(RequestStatus.Open, "The first permit must still be in flight for " +
                                                           "this test to be testing anything.");

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exitPermits.RequestExitPermit(PermitInput("KW-22222", exitDate)));
    }

    [Fact]
    public async Task A_Second_Permit_For_The_Same_Exit_Date_Should_Be_Refused_When_The_First_Is_Already_Recorded()
    {
        // The HR-entered case: WorkflowStatus null, i.e. effective immediately. Still a duplicate.
        var meId = CreateCurrentUserEmployee("EXP210");
        EnableExitPermitWorkflow("EXP210APP");

        var exitDate = DateTime.Today.AddDays(12);

        UsingDbContext(context =>
        {
            context.Set<EmployeeExitPermit>().Add(new EmployeeExitPermit
            {
                TenantId = TenantId,
                EmployeeId = meId,
                ExternalRequestNumber = "KW-33333",
                TravelReason = TravelReason.PersonalTravel,
                ExitDate = exitDate.Date,
                StartDate = exitDate.Date
            });
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exitPermits.RequestExitPermit(PermitInput("KW-44444", exitDate)));
    }

    [Fact]
    public async Task A_Permit_For_A_Different_Exit_Date_Should_Be_Allowed()
    {
        // The guard is per date, not per employee. Two trips in the same month are ordinary.
        CreateCurrentUserEmployee("EXP220");
        EnableExitPermitWorkflow("EXP220APP");

        await _exitPermits.RequestExitPermit(PermitInput("KW-55555", DateTime.Today.AddDays(10)));

        var second = await _exitPermits.RequestExitPermit(PermitInput("KW-66666", DateTime.Today.AddDays(20)));

        second.SubmittedForApproval.ShouldBeTrue();
    }

    [Fact]
    public async Task Somebody_Elses_Permit_On_The_Same_Date_Should_Not_Block_Mine()
    {
        // Scoped by employee: a colleague flying out the same day is not a duplicate.
        CreateCurrentUserEmployee("EXP230");
        var otherId = CreateEmployee("EXP230OTHER");
        EnableExitPermitWorkflow("EXP230APP");

        var exitDate = DateTime.Today.AddDays(15);

        UsingDbContext(context =>
        {
            context.Set<EmployeeExitPermit>().Add(new EmployeeExitPermit
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                ExternalRequestNumber = "KW-77777",
                TravelReason = TravelReason.PlannedLeave,
                ExitDate = exitDate.Date,
                StartDate = exitDate.Date
            });
        });

        var mine = await _exitPermits.RequestExitPermit(PermitInput("KW-88888", exitDate));

        mine.SubmittedForApproval.ShouldBeTrue();
    }

    // ---------------------------------------------------------------- the shared contract

    [Fact]
    public async Task A_Submitted_Permit_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        // The single-table design's whole premise: the pending row lives in the real table, so it must be
        // the ApprovedOnly filter -- not a separate request table -- that keeps it out of everything else.
        CreateCurrentUserEmployee("EXP300");
        EnableExitPermitWorkflow("EXP300APP");

        var result = await _exitPermits.RequestExitPermit(PermitInput("KW-99999", DateTime.Today.AddDays(9)));

        // Read through a repository inside a unit of work, because that is where ABP data filters live. A
        // raw DbContext query would bypass ApprovedOnly entirely and pass whatever the filter did.
        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeExitPermit>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            var visibleToOthers = repository.GetAll().Any(p => p.Id == result.TransactionId);
            visibleToOthers.ShouldBeFalse("A pending exit permit was visible to the rest of the system.");
        }

        // But the employee sees their own.
        var mine = await _exitPermits.GetMyExitPermits(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(result.TransactionId);
    }

    [Fact]
    public async Task A_Permit_Should_Be_Attributed_To_The_Signed_In_Employee_And_Numbered_With_Its_Own_Prefix()
    {
        // There is no employee field on the input at all, deliberately -- a self-service form must not be
        // able to raise a request on somebody else's behalf.
        var meId = CreateCurrentUserEmployee("EXP310");
        CreateEmployee("EXP310OTHER");
        EnableExitPermitWorkflow("EXP310APP");

        var result = await _exitPermits.RequestExitPermit(PermitInput("KW-12345", DateTime.Today.AddDays(5)));

        var row = UsingDbContext(c => c.Set<EmployeeExitPermit>().IgnoreQueryFilters()
            .Single(p => p.Id == result.TransactionId));

        row.EmployeeId.ShouldBe(meId);
        row.ExternalRequestNumber.ShouldBe("KW-12345");

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        request.RequestNumber.ShouldStartWith("EXP-");
    }

    [Fact]
    public async Task Should_Record_Immediately_When_Workflow_Is_Off_For_The_Type()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install: the record is
        // effective at once and no request is raised. Turning workflow off has to remain a real option.
        CreateCurrentUserEmployee("EXP320");

        var result = await _exitPermits.RequestExitPermit(PermitInput("KW-54321", DateTime.Today.AddDays(4)));

        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();

        var row = UsingDbContext(c => c.Set<EmployeeExitPermit>().IgnoreQueryFilters()
            .Single(p => p.Id == result.TransactionId));
        row.WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_List_Somebody_Elses_Permits()
    {
        CreateCurrentUserEmployee("EXP330");
        var otherId = CreateEmployee("EXP330OTHER");

        UsingDbContext(context =>
        {
            context.Set<EmployeeExitPermit>().Add(new EmployeeExitPermit
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                ExternalRequestNumber = "KW-00000",
                TravelReason = TravelReason.EmergencyLeave,
                ExitDate = DateTime.Today.AddDays(3),
                StartDate = DateTime.Today.AddDays(3)
            });
        });

        var mine = await _exitPermits.GetMyExitPermits(ListInput());

        mine.Items.ShouldBeEmpty();
    }

    // ---------------------------------------------------------------- the submission comment

    [Fact]
    public async Task Submitting_Should_Carry_The_Requesters_Comment_Onto_The_Created_Log_Entry()
    {
        // The requester's own note used to be written to EmployeeExitPermit.Comments and nowhere else, while
        // the Created log row was stamped with remark: null. So the activity feed -- the thing an approver
        // actually reads -- opened with a bare "Created" line and the one sentence explaining WHY the
        // request exists was not in it. The engine now asks the type's handler for that note at submit.
        CreateCurrentUserEmployee("EXP400");
        EnableExitPermitWorkflow("EXP400APP");

        var submission = await _exitPermits.RequestExitPermit(PermitInput("KW-77712", DateTime.Today.AddDays(12)));
        submission.SubmittedForApproval.ShouldBeTrue();

        var created = UsingDbContext(c => c.Set<RequestLog>()
            .Single(l => l.WorkflowRequestId == submission.WorkflowRequestId
                         && l.ActionType == RequestActionType.Created));

        created.RequestActionComment.ShouldBe("Visiting family");
    }

    [Fact]
    public async Task Submitting_Should_Still_Succeed_When_There_Is_No_Comment_To_Carry()
    {
        // The note is optional, and reading it must not become a new way for a submission to fail.
        CreateCurrentUserEmployee("EXP410");
        EnableExitPermitWorkflow("EXP410APP");

        var input = PermitInput("KW-77713", DateTime.Today.AddDays(13));
        input.Comments = null;

        var submission = await _exitPermits.RequestExitPermit(input);

        submission.SubmittedForApproval.ShouldBeTrue();

        var created = UsingDbContext(c => c.Set<RequestLog>()
            .Single(l => l.WorkflowRequestId == submission.WorkflowRequestId
                         && l.ActionType == RequestActionType.Created));

        created.RequestActionComment.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked, so this is the state the shell must handle, not an edge case.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _exitPermits.GetMyExitPermits(ListInput()));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exitPermits.RequestExitPermit(PermitInput("KW-13579", DateTime.Today.AddDays(6))));
    }
}
