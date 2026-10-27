using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.GovtRelationsManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Save as draft, for the exit permit that the pattern was built on.
///
/// <para>
/// A draft is defined by what it is NOT: not submitted, not effective, not numbered, not anybody else's
/// business. Each of those is a separate way for it to leak, and each is tested separately here rather than
/// assumed to follow from the status column. A draft that payroll can see is a half-finished record acted
/// on as if it were real; a draft in an approver's inbox is a decision asked of somebody about something
/// nobody sent them; a draft that consumed a request number leaves a gap in a sequence that is supposed to
/// be one per request.
/// </para>
///
/// <para>
/// The last test is the one that ties the feature together -- saved, changed, submitted -- because that is
/// the sequence the user described and the only one that proves the draft was still editable when they came
/// back to it.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyExitPermitDraft_Tests : AppTestBase
{
    private readonly IMyExitPermitAppService _exitPermits;

    public MyExitPermitDraft_Tests()
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

    /// <summary>Turns workflow on for exit permits, pointing them at a one-step chain with a real approver.</summary>
    private int EnableExitPermitWorkflow(string approverCode)
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

        return approverId;
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private static SaveExitPermitDraftInput DraftInput(string number, DateTime? exitDate) =>
        new SaveExitPermitDraftInput
        {
            ExternalRequestNumber = number,
            TravelReason = TravelReason.PlannedLeave,
            ExitDate = exitDate,
            StartDate = exitDate,
            Comments = "Still waiting for the permit"
        };

    // ---------------------------------------------------------------- what a draft is not

    [Fact]
    public async Task A_Draft_Should_Not_Raise_A_Workflow_Request()
    {
        // The design decision, pinned. A draft has no approver, no step and no number, so a WorkflowRequest
        // for one would be an empty row that every inbox and activity query then has to learn to exclude.
        CreateCurrentUserEmployee("DRAFT100");
        EnableExitPermitWorkflow("DRAFT100APP");

        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput("KW-1000", DateTime.Today.AddDays(10)));

        saved.SavedAsDraft.ShouldBeTrue();
        saved.SubmittedForApproval.ShouldBeFalse();
        saved.WorkflowRequestId.ShouldBeNull();

        var requests = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Where(r => r.TargetEntityId == saved.TransactionId && r.RequestType == "EmployeeExitPermit")
            .ToList());

        requests.ShouldBeEmpty("A draft raised a workflow request, so it has already cost a request number " +
                               "and appears in the workflow tables for something nobody has sent.");

        var row = UsingDbContext(c => c.Set<EmployeeExitPermit>().IgnoreQueryFilters()
            .Single(p => p.Id == saved.TransactionId));

        row.WorkflowStatus.ShouldBe(RequestStatus.Draft);
        row.WorkflowRequestId.ShouldBeNull();
    }

    [Fact]
    public async Task A_Draft_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        // The ApprovedOnly filter is what keeps payroll, quota and every report away from an unfinished
        // row. Read through a repository inside a unit of work, because that is where ABP data filters
        // live -- a raw DbContext query would bypass the filter and pass regardless of what it did.
        CreateCurrentUserEmployee("DRAFT110");

        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput("KW-1100", DateTime.Today.AddDays(10)));

        var repository = Resolve<IRepository<EmployeeExitPermit>>();
        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            repository.GetAll().Any(p => p.Id == saved.TransactionId)
                .ShouldBeFalse("A draft exit permit was visible to the rest of the system.");
        }
    }

    [Fact]
    public async Task A_Draft_Should_Not_Reach_An_Approvers_Inbox()
    {
        // Follows from there being no WorkflowRequest, but asserted against the approver tables directly:
        // the inbox is built from WorkFlowApprover rows, so that is where a leak would actually show.
        CreateCurrentUserEmployee("DRAFT120");
        var approverId = EnableExitPermitWorkflow("DRAFT120APP");

        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput("KW-1200", DateTime.Today.AddDays(10)));

        var waitingOnApprover = UsingDbContext(c => c.Set<WorkFlowApprover>()
            .Any(a => a.ApproverId == approverId && !a.IsApproved));

        waitingOnApprover.ShouldBeFalse("A draft landed in an approver's inbox. Nobody sent it to them.");

        // And nothing at all was raised for the row, which is the reason why.
        UsingDbContext(c => c.Set<WorkflowRequest>().Any(r => r.TargetEntityId == saved.TransactionId
                                                             && r.RequestType == "EmployeeExitPermit"))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task A_Draft_Should_Not_Consume_A_Request_Number()
    {
        // One number per request, and a draft is not a request yet. If drafts took numbers, a tenant that
        // abandons half of them would have a sequence full of holes pointing at nothing.
        CreateCurrentUserEmployee("DRAFT130");
        EnableExitPermitWorkflow("DRAFT130APP");

        await _exitPermits.SaveMyExitPermitDraft(DraftInput("KW-1300", DateTime.Today.AddDays(10)));
        await _exitPermits.SaveMyExitPermitDraft(DraftInput("KW-1301", DateTime.Today.AddDays(11)));

        var submitted = await _exitPermits.RequestExitPermit(new RequestExitPermitInput
        {
            ExternalRequestNumber = "KW-1302",
            TravelReason = TravelReason.PlannedLeave,
            ExitDate = DateTime.Today.AddDays(12),
            StartDate = DateTime.Today.AddDays(12)
        });

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == submitted.WorkflowRequestId.Value));

        // The first number in the tenant, not the third: the two drafts took none. "1", with no prefix and
        // no padding, is the whole of a request number now.
        request.RequestNumber.ShouldBe("1");
    }

    // ---------------------------------------------------------------- what a draft is

    [Fact]
    public async Task A_Draft_Should_Be_Visible_To_Its_Owner()
    {
        // The requester's own list is the one place it must appear -- it is the only way back to it.
        CreateCurrentUserEmployee("DRAFT200");

        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput("KW-2000", DateTime.Today.AddDays(10)));

        var mine = await _exitPermits.GetMyExitPermits(ListInput());

        var row = mine.Items.SingleOrDefault(i => i.Id == saved.TransactionId);

        row.ShouldNotBeNull("The requester cannot see their own draft, so they can never finish it.");
        row.IsDraft.ShouldBeTrue();
        row.CanEdit.ShouldBeTrue();
        row.CanCancel.ShouldBeTrue();
        row.WorkflowStatus.ShouldBe(RequestStatus.Draft);
    }

    [Fact]
    public async Task A_Draft_With_No_Dates_Should_Still_Appear_In_The_Default_List()
    {
        // The default list hides permits whose travel date has passed. A draft is allowed to hold no date
        // at all, which reads as an unset date far in the past -- so without an explicit exception the one
        // row the employee came back for would be the one row hidden from them.
        CreateCurrentUserEmployee("DRAFT210");

        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput(null, null));

        var upcomingOnly = await _exitPermits.GetMyExitPermits(new GetMyRequestsInput
        {
            IncludeHistory = false,
            MaxResultCount = 50,
            SkipCount = 0
        });

        upcomingOnly.Items.Select(i => i.Id).ShouldContain(saved.TransactionId);
    }

    [Fact]
    public async Task A_Draft_Should_Be_Saveable_With_Nothing_Filled_In()
    {
        // The whole reason to save rather than submit. An employee who has applied for the permit but has
        // not been given the number yet has nothing to type in the field the submit path insists on.
        CreateCurrentUserEmployee("DRAFT220");

        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput(null, null));

        saved.SavedAsDraft.ShouldBeTrue();

        var row = UsingDbContext(c => c.Set<EmployeeExitPermit>().IgnoreQueryFilters()
            .Single(p => p.Id == saved.TransactionId));

        row.ExternalRequestNumber.ShouldBe(string.Empty);
        row.ExitDate.ShouldBe(default(DateTime));
    }

    // ---------------------------------------------------------------- whose draft it is

    [Fact]
    public async Task Somebody_Elses_Draft_Should_Not_Be_Editable()
    {
        // A write, so ownership is checked on the write. A scoped list paired with an unscoped write is how
        // one employee ends up editing another's request.
        CreateCurrentUserEmployee("DRAFT300");
        var otherId = CreateEmployee("DRAFT300OTHER");

        var theirDraftId = 0;

        UsingDbContext(context =>
        {
            var permit = context.Set<EmployeeExitPermit>().Add(new EmployeeExitPermit
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                ExternalRequestNumber = "KW-3000",
                TravelReason = TravelReason.PlannedLeave,
                ExitDate = DateTime.Today.AddDays(10),
                StartDate = DateTime.Today.AddDays(10),
                WorkflowStatus = RequestStatus.Draft
            }).Entity;

            context.SaveChanges();
            theirDraftId = permit.Id;
        });

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _exitPermits.GetMyExitPermitDraftForEdit(new EntityDto(theirDraftId)));

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _exitPermits.SaveMyExitPermitDraft(new SaveExitPermitDraftInput
            {
                Id = theirDraftId,
                ExternalRequestNumber = "KW-STOLEN",
                TravelReason = TravelReason.PersonalTravel
            }));

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _exitPermits.SubmitMyExitPermitDraft(new EntityDto(theirDraftId)));
    }

    [Fact]
    public async Task A_Request_Already_Submitted_Should_Not_Be_Editable_As_A_Draft()
    {
        // Once it is with an approver, changing it is an amendment -- a different transaction with its own
        // rules. Letting the draft form reopen it would change what an approver is looking at underneath
        // them, with no record that it had happened.
        CreateCurrentUserEmployee("DRAFT310");
        EnableExitPermitWorkflow("DRAFT310APP");

        var submitted = await _exitPermits.RequestExitPermit(new RequestExitPermitInput
        {
            ExternalRequestNumber = "KW-3100",
            TravelReason = TravelReason.PlannedLeave,
            ExitDate = DateTime.Today.AddDays(10),
            StartDate = DateTime.Today.AddDays(10)
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exitPermits.GetMyExitPermitDraftForEdit(new EntityDto(submitted.TransactionId)));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exitPermits.SubmitMyExitPermitDraft(new EntityDto(submitted.TransactionId)));
    }

    // ---------------------------------------------------------------- saved, changed, submitted

    [Fact]
    public async Task A_Draft_Should_Be_Editable_And_Then_Submittable()
    {
        // The sequence the whole feature exists for, end to end: file it half-finished, come back, fill it
        // in, send it. Only at the last step does it get a number and an approver -- which is the assertion
        // that proves the earlier steps really did keep it out of the workflow.
        CreateCurrentUserEmployee("DRAFT400");
        var approverId = EnableExitPermitWorkflow("DRAFT400APP");

        var exitDate = DateTime.Today.AddDays(14);

        // Saved with nothing but a reason: the permit has been applied for, not yet issued.
        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput(null, null));

        // Reopened, and it comes back as the draft it was.
        var reopened = await _exitPermits.GetMyExitPermitDraftForEdit(new EntityDto(saved.TransactionId));

        reopened.Id.ShouldBe(saved.TransactionId);
        reopened.IsDraft.ShouldBeTrue();
        reopened.CanEdit.ShouldBeTrue();

        // The permit number has arrived, so the draft is completed.
        await _exitPermits.SaveMyExitPermitDraft(new SaveExitPermitDraftInput
        {
            Id = saved.TransactionId,
            ExternalRequestNumber = "KW-4000",
            TravelReason = TravelReason.EmergencyLeave,
            ExitDate = exitDate,
            StartDate = exitDate,
            Comments = "Permit now issued"
        });

        var edited = UsingDbContext(c => c.Set<EmployeeExitPermit>().IgnoreQueryFilters()
            .Single(p => p.Id == saved.TransactionId));

        edited.ExternalRequestNumber.ShouldBe("KW-4000");
        edited.TravelReason.ShouldBe(TravelReason.EmergencyLeave);
        edited.ExitDate.ShouldBe(exitDate.Date);
        edited.WorkflowStatus.ShouldBe(RequestStatus.Draft, "Editing a draft must not submit it.");
        edited.WorkflowRequestId.ShouldBeNull();

        // And now it is sent.
        var submitted = await _exitPermits.SubmitMyExitPermitDraft(new EntityDto(saved.TransactionId));

        submitted.TransactionId.ShouldBe(saved.TransactionId, "Submitting a draft must send the row that " +
                                                              "already exists, not create a second one.");
        submitted.SubmittedForApproval.ShouldBeTrue();
        submitted.WorkflowRequestId.ShouldNotBeNull();

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == submitted.WorkflowRequestId.Value));

        // The number appears here and nowhere earlier -- the first in the tenant, because the draft took none.
        request.RequestNumber.ShouldBe("1");
        request.RequestStatus.ShouldBe(RequestStatus.Open);

        var row = UsingDbContext(c => c.Set<EmployeeExitPermit>().IgnoreQueryFilters()
            .Single(p => p.Id == saved.TransactionId));

        row.WorkflowStatus.ShouldBe(RequestStatus.Open);

        // And an approver is finally waiting on it.
        UsingDbContext(c => c.Set<WorkFlowApprover>().Any(a => a.ApproverId == approverId && !a.IsApproved))
            .ShouldBeTrue("Submitting a draft did not put it in front of anybody.");

        // Only one permit row throughout -- the draft became the request rather than spawning one.
        UsingDbContext(c => c.Set<EmployeeExitPermit>().IgnoreQueryFilters()
                .Count(p => p.ExternalRequestNumber == "KW-4000"))
            .ShouldBe(1);
    }

    [Fact]
    public async Task Submitting_A_Draft_Should_Run_The_Types_Own_Rules()
    {
        // Nothing was validated when it was saved, so submit is the first and only gate. A draft that could
        // be submitted incomplete would put the rules one press further away than they were before drafts
        // existed.
        CreateCurrentUserEmployee("DRAFT410");
        EnableExitPermitWorkflow("DRAFT410APP");

        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput(null, null));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exitPermits.SubmitMyExitPermitDraft(new EntityDto(saved.TransactionId)));
    }

    [Fact]
    public async Task Submitting_A_Draft_With_Workflow_Off_Should_Simply_Make_It_Effective()
    {
        // The catalog seeds every type dormant, so this is the behaviour on a fresh install. With nobody to
        // approve it, submitting means the record takes effect -- status null, exactly as a row created
        // directly would be. A draft left Draft here would be permanently invisible with no way out.
        CreateCurrentUserEmployee("DRAFT420");

        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput("KW-4200", DateTime.Today.AddDays(9)));

        var submitted = await _exitPermits.SubmitMyExitPermitDraft(new EntityDto(saved.TransactionId));

        submitted.SubmittedForApproval.ShouldBeFalse();
        submitted.WorkflowRequestId.ShouldBeNull();

        var row = UsingDbContext(c => c.Set<EmployeeExitPermit>().IgnoreQueryFilters()
            .Single(p => p.Id == saved.TransactionId));

        row.WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task A_Draft_Should_Be_Discardable()
    {
        // Withdraw on a submitted request, Discard on a draft -- the same call, because in both cases the
        // row goes. Worth pinning because CanCancel has to keep covering Draft now that a draft can sit
        // there for days rather than for the length of one transaction.
        CreateCurrentUserEmployee("DRAFT430");

        var saved = await _exitPermits.SaveMyExitPermitDraft(DraftInput("KW-4300", DateTime.Today.AddDays(9)));

        await _exitPermits.CancelMyExitPermit(new EntityDto(saved.TransactionId));

        var mine = await _exitPermits.GetMyExitPermits(ListInput());

        mine.Items.Select(i => i.Id).ShouldNotContain(saved.TransactionId);
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked to an employee, so this is the state the shell must handle rather
        // than an edge case.
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exitPermits.SaveMyExitPermitDraft(DraftInput("KW-9999", DateTime.Today.AddDays(9))));
    }
}
