using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSettings;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service loan recovery exemptions -- and, above all, what approving one does to the loan.
///
/// <para>
/// The test to read first is
/// <see cref="Approving_An_Exemption_Should_Push_The_Loan_Recovery_End_Date_Out_By_A_Month"/>. Everything
/// else here is plumbing by comparison. Legacy's <c>UpdateRecoveryEndDate</c> cascade was deliberately not
/// ported when this entity was first brought over, and without it an exemption does not DEFER an instalment
/// -- it forgives it. The employee pays one instalment fewer, the loan is never fully recovered, and nothing
/// anywhere reports a problem. That test asserts the actual resulting date rather than merely that something
/// changed, because "it moved" would pass just as happily if the arithmetic were wrong.
/// </para>
///
/// <para>
/// Its three companions matter almost as much and for the same reason: the date must NOT move while the
/// request is pending, must NOT move when the tenant has the payroll setting switched off, and must walk
/// back when an approved exemption is removed.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyLoanExemptionAppService_Tests : AppTestBase
{
    private readonly IMyLoanExemptionAppService _exemptions;

    public MyLoanExemptionAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _exemptions = Resolve<IMyLoanExemptionAppService>();
    }

    private int TenantId => AbpSession.TenantId ?? 1;

    // A twelve-instalment loan with figures chosen so the schedule can be stated in full rather than
    // recomputed by the test. Legacy's month-by-month walk (EmployeeLoan.CalculateRecoveryDate) recovers 100
    // a month from January 2026, so December clears the 1200: 31 Dec 2026, the end date the loan is created
    // with. One exempted month recovers nothing, so January 2027 clears it instead: 31 Jan 2027, written out
    // literally below.
    private static readonly DateTime RecoveryStart = new DateTime(2026, 1, 10);
    private static readonly DateTime ScheduledEnd = new DateTime(2026, 12, 31);
    private static readonly DateTime EndAfterOneSkip = new DateTime(2027, 1, 31);

    private static readonly DateTime SkipMonth = new DateTime(2026, 6, 15);
    private static readonly DateTime SkipMonthSameMonthOtherDay = new DateTime(2026, 6, 25);
    private static readonly DateTime AnotherSkipMonth = new DateTime(2026, 7, 15);
    private static readonly DateTime OutsideTheLoanPeriod = new DateTime(2029, 3, 15);

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

    private int CreateLoan(int employeeId)
    {
        LoanType loanType = null;
        UsingDbContext(context =>
        {
            loanType = context.Set<LoanType>().Add(new LoanType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 6),
                NameEn = "Car Loan",
                NameAr = "قرض سيارة"
            }).Entity;
        });

        EmployeeLoan loan = null;
        UsingDbContext(context =>
        {
            loan = context.Set<EmployeeLoan>().Add(new EmployeeLoan
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                LoanTypeId = loanType.Id,
                StartDate = new DateTime(2026, 1, 1),
                RecoveryStartDate = RecoveryStart,
                EndDate = ScheduledEnd,
                ActualAmount = 1200,
                DueAmount = 1200,
                BalanceDueAmount = 1200,
                FixedAmount = 100,
                IncludeInLoanPay = true
            }).Entity;
        });

        return loan.Id;
    }

    /// <summary>The tenant-wide switch the whole cascade hangs off. Off (or absent) means an operator owns
    /// the recovery end date and nothing may move it.</summary>
    private void SetAutoUpdateRecoveryEndDate(bool enabled)
    {
        UsingDbContext(context =>
        {
            context.Set<ManagePayrollSettings>().Add(new ManagePayrollSettings
            {
                TenantId = TenantId,
                AutoUpdateLoanRecoveryEndDate = enabled
            });
        });
    }

    /// <summary>Turns workflow on for the loan-exemption type, pointing it at a one-step chain with a real
    /// approver. Returns the approver's employee id.</summary>
    private int EnableLoanExemptionWorkflow(string approverCode)
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
                Code = "LEX-STD-" + Guid.NewGuid().ToString("N").Substring(0, 4),
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
                EndDate = DateTime.Today.AddYears(5)
            });

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeLoanExemption");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static RequestLoanExemptionInput ExemptionFor(int loanId, DateTime on) =>
        new RequestLoanExemptionInput
        {
            EmployeeLoanId = loanId,
            ExemptionDate = on,
            Comments = "Tight month"
        };

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private DateTime LoanEndDate(int loanId) =>
        UsingDbContext(c => c.Set<EmployeeLoan>().IgnoreQueryFilters().Single(l => l.Id == loanId).EndDate);

    private async Task ApproveTheOutstandingRequestAsync(int approverId)
    {
        var requestId = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.RequestType == "EmployeeLoanExemption").Id);

        await Resolve<IWorkflowApprovalService>().ApproveAsync(requestId, approverId, "Agreed");
    }

    // ---------------------------------------------------------------- the end-date cascade

    [Fact]
    public async Task Approving_An_Exemption_Should_Push_The_Loan_Recovery_End_Date_Out_By_A_Month()
    {
        // THE one. Legacy's UpdateRecoveryEndDate cascade, ported: a skipped instalment is DEFERRED, so the
        // loan runs a month longer. Without this the employee simply pays 1100 of a 1200 loan and nothing
        // reports it. The expected date is written out literally rather than recomputed here, so a change to
        // the arithmetic has to fail this test rather than move with it.
        var meId = CreateCurrentUserEmployee("LEX100");
        var loanId = CreateLoan(meId);
        SetAutoUpdateRecoveryEndDate(true);
        var approverId = EnableLoanExemptionWorkflow("LEX100APP");

        var submission = await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));
        submission.SubmittedForApproval.ShouldBeTrue();

        await ApproveTheOutstandingRequestAsync(approverId);

        LoanEndDate(loanId).ShouldBe(EndAfterOneSkip);

        var row = UsingDbContext(c => c.Set<EmployeeLoanExemption>().IgnoreQueryFilters()
            .Single(e => e.Id == submission.TransactionId));
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task The_Loan_End_Date_Should_Not_Move_While_The_Exemption_Is_Still_Pending()
    {
        // The other half of the same rule, and the one that would be easy to get wrong by recomputing at
        // submit time. Nobody has agreed to defer anything yet, so the repayment schedule -- which payroll
        // reads -- must be exactly as it was.
        var meId = CreateCurrentUserEmployee("LEX110");
        var loanId = CreateLoan(meId);
        SetAutoUpdateRecoveryEndDate(true);
        EnableLoanExemptionWorkflow("LEX110APP");

        var submission = await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));

        var row = UsingDbContext(c => c.Set<EmployeeLoanExemption>().IgnoreQueryFilters()
            .Single(e => e.Id == submission.TransactionId));
        row.WorkflowStatus.ShouldBe(RequestStatus.Open,
            "The exemption must still be in flight for this test to be testing anything.");

        LoanEndDate(loanId).ShouldBe(ScheduledEnd);
    }

    [Fact]
    public async Task Should_Not_Move_The_End_Date_When_The_Payroll_Setting_Is_Off()
    {
        // AutoUpdateLoanRecoveryEndDate off means the tenant maintains recovery end dates by hand, and this
        // cascade must keep its hands off them -- legacy makes the same check before doing anything. The
        // exemption is still granted; it simply does not reschedule the loan.
        var meId = CreateCurrentUserEmployee("LEX120");
        var loanId = CreateLoan(meId);
        SetAutoUpdateRecoveryEndDate(false);
        var approverId = EnableLoanExemptionWorkflow("LEX120APP");

        var submission = await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));

        await ApproveTheOutstandingRequestAsync(approverId);

        LoanEndDate(loanId).ShouldBe(ScheduledEnd);

        var row = UsingDbContext(c => c.Set<EmployeeLoanExemption>().IgnoreQueryFilters()
            .Single(e => e.Id == submission.TransactionId));
        row.WorkflowStatus.ShouldBe(RequestStatus.Approved,
            "The exemption is still granted when the setting is off -- only the reschedule is suppressed.");
    }

    [Fact]
    public async Task Removing_An_Approved_Exemption_Should_Walk_The_End_Date_Back()
    {
        // The reversal, and the reason the recompute is a full recalculation rather than an increment: with
        // the removed row excluded it lands back on exactly the date the loan had before. An exemption
        // deleted in error must not leave the loan permanently a month long.
        var meId = CreateCurrentUserEmployee("LEX130");
        var loanId = CreateLoan(meId);
        SetAutoUpdateRecoveryEndDate(true);
        var approverId = EnableLoanExemptionWorkflow("LEX130APP");

        var submission = await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));
        await ApproveTheOutstandingRequestAsync(approverId);

        LoanEndDate(loanId).ShouldBe(EndAfterOneSkip);

        await Resolve<IEmployeeLoanExemptionsAppService>()
            .Delete(new Abp.Application.Services.Dto.EntityDto(submission.TransactionId));

        LoanEndDate(loanId).ShouldBe(ScheduledEnd);
    }

    // ---------------------------------------------------------------- one exemption per loan per month

    [Fact]
    public async Task A_Second_Exemption_In_The_Same_Month_Should_Be_Refused_While_The_First_Is_Still_Pending()
    {
        // The first exemption is Open, which the ApprovedOnly filter hides -- so a guard that did not look
        // past that filter would accept the duplicate silently, exactly when a resubmission is most likely:
        // the employee did not see their first submission land. Two approvals would then push the loan out by
        // two months for one skipped instalment.
        var meId = CreateCurrentUserEmployee("LEX200");
        var loanId = CreateLoan(meId);
        SetAutoUpdateRecoveryEndDate(true);
        EnableLoanExemptionWorkflow("LEX200APP");

        var first = await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));
        first.SubmittedForApproval.ShouldBeTrue();

        var exception = await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonthSameMonthOtherDay)));

        // The month is named in the refusal, from the date rather than from legacy's forty-line switch.
        exception.Message.ShouldContain("2026");
    }

    [Fact]
    public async Task A_Second_Exemption_In_The_Same_Month_Should_Be_Refused_When_The_First_Is_Already_Granted()
    {
        // The HR-entered case: WorkflowStatus null, i.e. effective immediately. Still the same month.
        var meId = CreateCurrentUserEmployee("LEX210");
        var loanId = CreateLoan(meId);

        UsingDbContext(context =>
        {
            context.Set<EmployeeLoanExemption>().Add(new EmployeeLoanExemption
            {
                TenantId = TenantId,
                EmployeeId = meId,
                EmployeeLoanId = loanId,
                ExemptionDate = SkipMonth
            });
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonthSameMonthOtherDay)));
    }

    [Fact]
    public async Task An_Exemption_In_A_Different_Month_Should_Be_Allowed()
    {
        // The rule is per month, not per loan -- which is exactly why the exemption is a child row of the
        // loan rather than an amendment to it. A loan can legitimately be exempted more than once.
        var meId = CreateCurrentUserEmployee("LEX220");
        var loanId = CreateLoan(meId);

        await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));

        var second = await _exemptions.RequestLoanExemption(ExemptionFor(loanId, AnotherSkipMonth));

        second.TransactionId.ShouldBeGreaterThan(0);

        var mine = await _exemptions.GetMyLoanExemptions(ListInput());
        mine.TotalCount.ShouldBe(2);
    }

    // ---------------------------------------------------------------- the recovery period

    [Fact]
    public async Task An_Exemption_Outside_The_Loans_Own_Recovery_Period_Should_Be_Refused()
    {
        // Not a legacy rule. Legacy accepted this and then its own recompute -- which counts only exemptions
        // inside the recovery window -- quietly ignored it. An exemption that is recorded and does nothing is
        // worse than one that is refused, because the employee believes the instalment was skipped.
        var meId = CreateCurrentUserEmployee("LEX300");
        var loanId = CreateLoan(meId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exemptions.RequestLoanExemption(ExemptionFor(loanId, OutsideTheLoanPeriod)));
    }

    // ---------------------------------------------------------------- the shared contract

    [Fact]
    public async Task A_Submitted_Exemption_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        // The single-table design's premise: the pending row lives in the real table, so it must be the
        // ApprovedOnly filter -- not a separate request table -- that keeps it away from payroll.
        var meId = CreateCurrentUserEmployee("LEX400");
        var loanId = CreateLoan(meId);
        EnableLoanExemptionWorkflow("LEX400APP");

        var result = await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));

        // Read through a repository inside a unit of work, because that is where ABP data filters live. A
        // raw DbContext query would bypass ApprovedOnly entirely and pass whatever the filter did.
        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeLoanExemption>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            var visibleToOthers = repository.GetAll().Any(e => e.Id == result.TransactionId);
            visibleToOthers.ShouldBeFalse("A pending loan exemption was visible to the rest of the system.");
        }

        // But the employee sees their own.
        var mine = await _exemptions.GetMyLoanExemptions(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(result.TransactionId);
    }

    [Fact]
    public async Task An_Exemption_Should_Be_Attributed_To_The_Signed_In_Employee_And_Numbered_With_Its_Own_Prefix()
    {
        // There is no employee field on the input at all, deliberately -- and the employee id on the row is
        // taken from the LOAN's owner, so whose exemption it is always agrees with whose loan it is.
        var meId = CreateCurrentUserEmployee("LEX410");
        var loanId = CreateLoan(meId);
        EnableLoanExemptionWorkflow("LEX410APP");

        var result = await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));

        var row = UsingDbContext(c => c.Set<EmployeeLoanExemption>().IgnoreQueryFilters()
            .Single(e => e.Id == result.TransactionId));
        row.EmployeeId.ShouldBe(meId);

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        // A number of its own, and nothing but digits: the per-type prefix went when the counter became
        // tenant-wide, so "which type is this" is no longer something the number answers.
        request.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        request.RequestNumber.ShouldNotContain("-");
        int.Parse(request.RequestNumber).ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Refuse_An_Exemption_Against_Somebody_Elses_Loan()
    {
        // The loan is scoped to the caller rather than merely looked up: another employee's loan id must read
        // as "not found". An exemption defers real money on a real repayment schedule.
        CreateCurrentUserEmployee("LEX420");
        var otherId = CreateEmployee("LEX420OTHER");
        var otherLoanId = CreateLoan(otherId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exemptions.RequestLoanExemption(ExemptionFor(otherLoanId, SkipMonth)));

        var loans = await _exemptions.GetMyLoansForExemption();
        loans.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Offer_The_Employees_Own_Loans_To_Pick_From()
    {
        var meId = CreateCurrentUserEmployee("LEX430");
        var loanId = CreateLoan(meId);

        var loans = await _exemptions.GetMyLoansForExemption();

        loans.Count.ShouldBe(1);
        loans[0].EmployeeLoanId.ShouldBe(loanId);
        loans[0].FixedAmount.ShouldBe(100);
        loans[0].EndDate.ShouldBe(ScheduledEnd);
    }

    [Fact]
    public async Task Should_Record_Immediately_When_Workflow_Is_Off_For_The_Type()
    {
        // Every type is seeded dormant, so this is the behaviour on a fresh install. Note the duplicate and
        // recovery-period rules are STILL enforced with routing off (see the tests above, which run without
        // enabling workflow) -- turning approval off must not turn the rules off with it.
        var meId = CreateCurrentUserEmployee("LEX440");
        var loanId = CreateLoan(meId);
        SetAutoUpdateRecoveryEndDate(true);

        var result = await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));

        result.SubmittedForApproval.ShouldBeFalse();
        result.WorkflowRequestId.ShouldBeNull();

        var row = UsingDbContext(c => c.Set<EmployeeLoanExemption>().IgnoreQueryFilters()
            .Single(e => e.Id == result.TransactionId));
        row.WorkflowStatus.ShouldBeNull();

        // In force at once, so the instalment is deferred at once: no approval will ever run to do it later.
        LoanEndDate(loanId).ShouldBe(EndAfterOneSkip);
    }

    /// <summary>
    /// The amount shown is what the exemption actually defers: the instalment in force in the exemption month
    /// (150 from June), not the loan's original fixed amount of 100.
    /// </summary>
    [Fact]
    public async Task The_Instalment_Shown_Should_Be_The_One_In_Force_In_The_Exemption_Month()
    {
        var meId = CreateCurrentUserEmployee("LEX450");
        var loanId = CreateLoan(meId);
        SeedInstallment(loanId, new DateTime(2026, 1, 1), new DateTime(2026, 5, 31), 100);
        SeedInstallment(loanId, new DateTime(2026, 6, 1), new DateTime(9999, 12, 31), 150);

        await _exemptions.RequestLoanExemption(ExemptionFor(loanId, SkipMonth));

        var list = await _exemptions.GetMyLoanExemptions(ListInput());
        list.Items.ShouldHaveSingleItem().InstalmentAmount.ShouldBe(150);
    }

    [Fact]
    public async Task The_Loan_Picker_Should_Show_The_Instalment_In_Force_Now()
    {
        var meId = CreateCurrentUserEmployee("LEX460");
        var loanId = CreateLoan(meId);
        SeedInstallment(loanId, new DateTime(2026, 1, 1), new DateTime(9999, 12, 31), 175);

        var loans = await _exemptions.GetMyLoansForExemption();

        loans.ShouldHaveSingleItem().FixedAmount.ShouldBe(175);
    }

    private void SeedInstallment(int loanId, DateTime start, DateTime end, double amount) =>
        UsingDbContext(c => c.Set<EmployeeLoanInstallment>().Add(new EmployeeLoanInstallment
        {
            TenantId = TenantId, EmployeeLoanId = loanId, StartDate = start, EndDate = end, Amount = amount
        }));

    [Fact]
    public async Task Should_Not_List_Somebody_Elses_Exemptions()
    {
        CreateCurrentUserEmployee("LEX450");
        var otherId = CreateEmployee("LEX450OTHER");
        var otherLoanId = CreateLoan(otherId);

        UsingDbContext(context =>
        {
            context.Set<EmployeeLoanExemption>().Add(new EmployeeLoanExemption
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                EmployeeLoanId = otherLoanId,
                ExemptionDate = SkipMonth
            });
        });

        var mine = await _exemptions.GetMyLoanExemptions(ListInput());

        mine.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked, so this is a state the shell must handle rather than an edge case.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _exemptions.GetMyLoanExemptions(ListInput()));

        await Assert.ThrowsAsync<UserFriendlyException>(() => _exemptions.GetMyLoansForExemption());

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _exemptions.RequestLoanExemption(ExemptionFor(1, SkipMonth)));
    }
}
