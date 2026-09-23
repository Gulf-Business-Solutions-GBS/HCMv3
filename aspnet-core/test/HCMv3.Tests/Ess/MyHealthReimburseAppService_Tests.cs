using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service health reimbursement -- and, mostly, its balance check.
///
/// <para>
/// This is the transaction type where getting the arithmetic wrong costs somebody money in both directions:
/// too strict and a legitimate medical claim is refused after the employee has already paid the doctor; too
/// lax and they over-claim their annual entitlement. So the tests here are weighted towards the balance
/// rather than the plumbing.
/// </para>
///
/// <para>
/// The one to read first is <see cref="A_Pending_Claim_Should_Reduce_What_Can_Still_Be_Claimed"/>. It pins
/// the Remaining-versus-Claimable distinction this whole design rests on: a claim awaiting approval must NOT
/// move the used figure -- payroll has not paid it and it may yet be rejected -- but it must reduce what the
/// employee can go on to claim, or the same money could be claimed twice over with each submission passing
/// its own check independently. That is only computable because the pending claim sits in the real table
/// next to the approved ones; in the legacy two-table design it sat in HealthReimburseRequest, where the
/// balance arithmetic never looked.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyHealthReimburseAppService_Tests : AppTestBase
{
    private readonly IMyHealthReimburseAppService _claims;

    public MyHealthReimburseAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _claims = Resolve<IMyHealthReimburseAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    /// <summary>Mid-year, so the payment date and the day after it always fall in the same entitlement
    /// window whatever day the suite happens to run.</summary>
    private static DateTime ClaimDate => new DateTime(DateTime.Today.Year, 6, 15);

    private static DateTime OtherClaimDate => new DateTime(DateTime.Today.Year, 6, 16);

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

                // 1 January, so the deduction window computed by GetHealthReimbAccrualStartEnd is a plain
                // calendar year -- the tests can then state expected figures without restating that logic.
                JoiningDate = new DateTime(2020, 1, 1),
                ProbationEndDate = new DateTime(2020, 4, 1),
                UserId = userId,
                NationalityId = nationality.Id
            }).Entity;
        });

        return employee.Id;
    }

    private int CreateCurrentUserEmployee(string code) => CreateEmployee(code, AbpSession.UserId);

    /// <summary>
    /// Gives the employee a salary profile that carries <paramref name="amount"/> of health-reimbursement
    /// entitlement: an employee group, a sub-group, one tier row, and the org assignment that ties the
    /// employee to it on the claim date.
    /// </summary>
    private void GiveEntitlement(int employeeId, double amount, double cap = 0)
    {
        UsingDbContext(context =>
        {
            var group = context.Set<EmployeeGroup>().Add(new EmployeeGroup
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "G",
                NameAr = "G"
            }).Entity;

            var dependantType = context.Set<DependantType>().Add(new DependantType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Self",
                NameAr = "Self",
                IsEmployee = true
            }).Entity;

            context.SaveChanges();

            var subGroup = context.Set<EmployeeSubGroup>().Add(new EmployeeSubGroup
            {
                TenantId = TenantId,
                EmployeeGroupId = group.Id,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "SG",
                NameAr = "SG",
                MaxHealthReimburseAmount = cap,
                HealthInsurancePeriod = CustomStartMonth.None
            }).Entity;

            context.SaveChanges();

            context.Set<EmployeeSubGroupHealthReimburse>().Add(new EmployeeSubGroupHealthReimburse
            {
                TenantId = TenantId,
                EmployeeSubGroupId = subGroup.Id,
                DependantTypeId = dependantType.Id,
                StartDate = new DateTime(2000, 1, 1),
                EndDate = new DateTime(2100, 12, 31),
                Amount = amount,
                AgeLimit = 0
            });

            context.Set<EmployeeOrgAssignment>().Add(new EmployeeOrgAssignment
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                StartDate = new DateTime(2020, 1, 1),
                EndDate = new DateTime(2100, 12, 31),
                EmployeeGroupId = group.Id,
                EmployeeSubGroupId = subGroup.Id
            });
        });
    }

    /// <summary>Turns workflow on for the health-reimbursement type, pointing it at a one-step chain with a
    /// real approver. Returns the approver's employee id.</summary>
    private int EnableHealthReimburseWorkflow(string approverCode)
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
                Code = "HRB-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
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

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeHealthReimburse");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static RequestHealthReimburseInput ClaimFor(double amount, DateTime? on = null) =>
        new RequestHealthReimburseInput
        {
            PaymentDate = on ?? ClaimDate,
            Amount = amount,
            Comments = "Dental"
        };

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private Task<MyHealthReimburseBalanceDto> BalanceAsync() =>
        _claims.GetMyHealthReimburseBalance(new GetMyHealthReimburseBalanceInput { AsOf = ClaimDate });

    // ---------------------------------------------------------------- the balance

    [Fact]
    public async Task Should_Accept_A_Claim_Within_The_Balance()
    {
        var meId = CreateCurrentUserEmployee("HRB100");
        GiveEntitlement(meId, 500);

        var result = await _claims.RequestHealthReimburse(ClaimFor(200));

        var row = UsingDbContext(c => c.Set<EmployeeHealthReimburse>().IgnoreQueryFilters()
            .Single(r => r.Id == result.TransactionId));

        row.EmployeeId.ShouldBe(meId);
        row.Amount.ShouldBe(200);
    }

    [Fact]
    public async Task Should_Refuse_A_Claim_That_Exceeds_The_Balance()
    {
        // 500 of entitlement, 600 claimed. Legacy put the figures in the refusal and so does this port --
        // told only "not enough", an employee cannot tell whether they misread their entitlement or have
        // already spent it.
        var meId = CreateCurrentUserEmployee("HRB110");
        GiveEntitlement(meId, 500);

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _claims.RequestHealthReimburse(ClaimFor(600)));

        exception.Message.ShouldContain("500");
        exception.Message.ShouldContain("600");
    }

    [Fact]
    public async Task A_Pending_Claim_Should_Reduce_What_Can_Still_Be_Claimed()
    {
        // THE one. A claim awaiting approval must not move Used -- payroll has not paid it and it may still
        // be rejected -- but it must reduce Claimable, or the same entitlement could be claimed twice with
        // each submission passing its own check.
        var meId = CreateCurrentUserEmployee("HRB120");
        GiveEntitlement(meId, 500);
        EnableHealthReimburseWorkflow("HRB120APP");

        var first = await _claims.RequestHealthReimburse(ClaimFor(300));
        first.SubmittedForApproval.ShouldBeTrue();

        var pendingRow = UsingDbContext(c => c.Set<EmployeeHealthReimburse>().IgnoreQueryFilters()
            .Single(r => r.Id == first.TransactionId));
        pendingRow.WorkflowStatus.ShouldBe(RequestStatus.Open,
            "The first claim must still be in flight for this test to be testing anything.");

        var balance = await BalanceAsync();

        balance.Used.ShouldBe(0, "A pending claim has not been paid, so nothing may be marked as used.");
        balance.Pending.ShouldBe(300);
        balance.Remaining.ShouldBe(500, "Remaining ignores pending claims -- that is the whole distinction.");
        balance.Claimable.ShouldBe(200, "Claimable is what is left after what is already in flight.");

        // And the second claim is judged against Claimable, not Remaining. A different payment date, so this
        // is the balance refusing it rather than the duplicate guard.
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _claims.RequestHealthReimburse(ClaimFor(300, OtherClaimDate)));
    }

    [Fact]
    public async Task Approving_A_Claim_Should_Move_It_From_Pending_Into_Used()
    {
        // The other half of the same rule: approval is what turns a claim on the balance into a deduction
        // from it -- and it does so with no stored total to update, purely by becoming visible to the
        // ApprovedOnly filter.
        var meId = CreateCurrentUserEmployee("HRB130");
        GiveEntitlement(meId, 500);
        var approverId = EnableHealthReimburseWorkflow("HRB130APP");

        var submission = await _claims.RequestHealthReimburse(ClaimFor(300));

        var requestId = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.RequestType == "EmployeeHealthReimburse").Id);

        await Resolve<IWorkflowApprovalService>().ApproveAsync(requestId, approverId, "Agreed");

        var balance = await BalanceAsync();

        balance.Used.ShouldBe(300);
        balance.Pending.ShouldBe(0);
        balance.Remaining.ShouldBe(200);
        balance.Claimable.ShouldBe(200);

        var row = UsingDbContext(c => c.Set<EmployeeHealthReimburse>().IgnoreQueryFilters()
            .Single(r => r.Id == submission.TransactionId));
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task An_Employee_With_No_Entitlement_Should_Be_Refused_Clearly()
    {
        // No org assignment at all, so no sub-group and no entitlement. The dangerous failure mode is the
        // opposite of a refusal: a balance calculation that cannot find an entitlement and therefore imposes
        // no limit would let anyone with a misconfigured salary profile claim without bound, and nobody
        // would notice until payroll.
        CreateCurrentUserEmployee("HRB140");

        var balance = await BalanceAsync();
        balance.HasEntitlement.ShouldBeFalse();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _claims.RequestHealthReimburse(ClaimFor(100)));

        exception.Message.ShouldContain("entitlement");
    }

    // ---------------------------------------------------------------- the duplicate guard

    [Fact]
    public async Task A_Second_Claim_For_The_Same_Payment_Date_Should_Be_Refused_While_The_First_Is_Still_Pending()
    {
        // The first claim is Open, which the ApprovedOnly filter hides -- so a guard that did not disable
        // that filter would accept this duplicate silently, exactly when a resubmission is most likely: the
        // employee did not see their first submission land.
        var meId = CreateCurrentUserEmployee("HRB200");
        GiveEntitlement(meId, 5000);
        EnableHealthReimburseWorkflow("HRB200APP");

        var first = await _claims.RequestHealthReimburse(ClaimFor(100));
        first.SubmittedForApproval.ShouldBeTrue();

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _claims.RequestHealthReimburse(ClaimFor(100)));
    }

    [Fact]
    public async Task A_Second_Claim_For_The_Same_Payment_Date_Should_Be_Refused_When_The_First_Is_Already_Recorded()
    {
        // The HR-entered case: WorkflowStatus null, i.e. effective immediately. Still a duplicate.
        var meId = CreateCurrentUserEmployee("HRB210");
        GiveEntitlement(meId, 5000);

        UsingDbContext(context =>
        {
            context.Set<EmployeeHealthReimburse>().Add(new EmployeeHealthReimburse
            {
                TenantId = TenantId,
                EmployeeId = meId,
                PaymentDate = ClaimDate,
                Amount = 100
            });
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _claims.RequestHealthReimburse(ClaimFor(100)));
    }

    [Fact]
    public async Task A_Claim_For_A_Different_Payment_Date_Should_Be_Allowed()
    {
        // The guard is per date, not per employee. Two visits to the doctor in one month are ordinary.
        var meId = CreateCurrentUserEmployee("HRB220");
        GiveEntitlement(meId, 5000);

        await _claims.RequestHealthReimburse(ClaimFor(100));

        var second = await _claims.RequestHealthReimburse(ClaimFor(100, OtherClaimDate));

        second.TransactionId.ShouldBeGreaterThan(0);
    }

    // ---------------------------------------------------------------- the shared contract

    [Fact]
    public async Task A_Submitted_Claim_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        // The single-table design's whole premise: the pending row lives in the real table, so it must be
        // the ApprovedOnly filter -- not a separate request table -- that keeps it out of payroll.
        var meId = CreateCurrentUserEmployee("HRB300");
        GiveEntitlement(meId, 5000);
        EnableHealthReimburseWorkflow("HRB300APP");

        var result = await _claims.RequestHealthReimburse(ClaimFor(150));

        // Read through a repository inside a unit of work, because that is where ABP data filters live. A
        // raw DbContext query would bypass ApprovedOnly entirely and pass whatever the filter did.
        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeHealthReimburse>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            var visibleToOthers = repository.GetAll().Any(c => c.Id == result.TransactionId);
            visibleToOthers.ShouldBeFalse("A pending health reimbursement claim was visible to the rest of the system.");
        }

        // But the employee sees their own.
        var mine = await _claims.GetMyHealthReimburses(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(result.TransactionId);
    }

    [Fact]
    public async Task A_Claim_Should_Be_Attributed_To_The_Signed_In_Employee_And_Numbered_With_Its_Own_Prefix()
    {
        // There is no employee field on the input at all, deliberately -- a self-service form must not be
        // able to claim out of somebody else's entitlement.
        var meId = CreateCurrentUserEmployee("HRB310");
        CreateEmployee("HRB310OTHER");
        GiveEntitlement(meId, 5000);
        EnableHealthReimburseWorkflow("HRB310APP");

        var result = await _claims.RequestHealthReimburse(ClaimFor(120));

        var row = UsingDbContext(c => c.Set<EmployeeHealthReimburse>().IgnoreQueryFilters()
            .Single(r => r.Id == result.TransactionId));
        row.EmployeeId.ShouldBe(meId);

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        request.RequestNumber.ShouldStartWith("HRB-");
    }

    [Fact]
    public async Task Should_Record_Immediately_When_Workflow_Is_Off_For_The_Type()
    {
        // Every type is seeded dormant, so this is the behaviour on a fresh install -- and note the balance
        // is STILL enforced (see Should_Refuse_A_Claim_That_Exceeds_The_Balance, which runs with workflow
        // off). Turning routing off must not turn the quota off with it.
        var meId = CreateCurrentUserEmployee("HRB320");
        GiveEntitlement(meId, 5000);

        var result = await _claims.RequestHealthReimburse(ClaimFor(100));

        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();

        var row = UsingDbContext(c => c.Set<EmployeeHealthReimburse>().IgnoreQueryFilters()
            .Single(r => r.Id == result.TransactionId));
        row.WorkflowStatus.ShouldBeNull();
    }

    [Fact]
    public async Task Should_Not_List_Somebody_Elses_Claims()
    {
        CreateCurrentUserEmployee("HRB330");
        var otherId = CreateEmployee("HRB330OTHER");

        UsingDbContext(context =>
        {
            context.Set<EmployeeHealthReimburse>().Add(new EmployeeHealthReimburse
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                PaymentDate = ClaimDate,
                Amount = 100
            });
        });

        var mine = await _claims.GetMyHealthReimburses(ListInput());

        mine.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked, so this is the state the shell must handle, not an edge case.
        // The balance endpoint is included: it reports somebody's entitlement, so it must not answer for a
        // caller it cannot identify.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _claims.GetMyHealthReimburses(ListInput()));

        await Assert.ThrowsAsync<UserFriendlyException>(() => BalanceAsync());

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _claims.RequestHealthReimburse(ClaimFor(100)));
    }
}
