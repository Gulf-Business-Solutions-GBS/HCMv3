using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSettings;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Self-service passport (document) withdrawal -- the ninth type onto the workflow engine, and the one that
/// replaced a whole legacy request table rather than merely gaining two columns.
///
/// <para>
/// Legacy kept <c>PassportWithdrawalRequest</c> beside <c>EmployeeDocumentMvmt</c> and copied the approved
/// request into the movement log at the last approval step. The tests here protect what replaced that: one
/// row, hidden until approved, and the one piece of genuinely passport-specific behaviour that came with it
/// -- a later request supersedes an earlier open one instead of being blocked by it.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyPassportWithdrawalAppService_Tests : AppTestBase
{
    private readonly IMyPassportWithdrawalAppService _withdrawals;

    public MyPassportWithdrawalAppService_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _withdrawals = Resolve<IMyPassportWithdrawalAppService>();
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

    private int CreatePassportFor(int employeeId)
    {
        DocumentType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = "Passport",
                NameAr = "جواز سفر"
            }).Entity;
        });

        EmployeeDocument document = null;
        UsingDbContext(context =>
        {
            document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = type.Id,
                DocumentNum = "P" + Guid.NewGuid().ToString("N").Substring(0, 6),
                IssueDate = DateTime.Today.AddYears(-2),
                ExpiryDate = DateTime.Today.AddYears(5)
            }).Entity;
        });

        return document.Id;
    }

    /// <summary>Turns workflow on for the passport type, pointing it at a one-step chain with a real approver.</summary>
    private int EnableWorkflow(string approverCode)
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
                Code = "PPW-STD",
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

            var type = context.Set<WorkflowTransactionType>().First(t => t.Code == "EmployeeDocumentMvmt");
            type.IsWorkflowEnabled = true;
            type.IsEssEnabled = true;
            type.IsActive = true;
            type.DefaultRequestMasterId = master.Id;
        });

        return approverId;
    }

    private static GetMyRequestsInput ListInput() =>
        new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 };

    private static RequestPassportWithdrawalInput Input(int documentId, int outInDays = 1, int backInDays = 14) =>
        new RequestPassportWithdrawalInput
        {
            EmployeeDocumentId = documentId,
            WithdrawDate = DateTime.Today.AddDays(outInDays),
            ReturnDate = DateTime.Today.AddDays(backInDays),
            WithdrawalReason = "Visa application"
        };

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        // Admin accounts are never linked to an employee, so this is the state the screen must handle
        // rather than an edge case.
        await Assert.ThrowsAsync<UserFriendlyException>(() => _withdrawals.GetMyPassportWithdrawals(ListInput()));
    }

    [Fact]
    public async Task Should_Refuse_A_Return_Date_Before_The_Withdrawal_Date()
    {
        // A document that comes back before it goes out is not a period anybody can act on -- the approver
        // would be agreeing to nothing, and the movement log would show a negative custody window.
        var meId = CreateCurrentUserEmployee("PPW100");
        var documentId = CreatePassportFor(meId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _withdrawals.RequestPassportWithdrawal(new RequestPassportWithdrawalInput
            {
                EmployeeDocumentId = documentId,
                WithdrawDate = DateTime.Today.AddDays(10),
                ReturnDate = DateTime.Today.AddDays(3),
                WithdrawalReason = "Visa application"
            }));
    }

    [Fact]
    public async Task Should_Refuse_A_Document_That_Belongs_To_Somebody_Else()
    {
        // This endpoint hands over an original document. Another employee's document id must read as "not
        // found" rather than as a valid target.
        CreateCurrentUserEmployee("PPW110");
        var otherId = CreateEmployee("PPW110OTHER");
        var theirDocumentId = CreatePassportFor(otherId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _withdrawals.RequestPassportWithdrawal(Input(theirDocumentId)));
    }

    [Fact]
    public async Task Should_Require_A_Reason_Only_When_The_Tenant_Asks_For_One()
    {
        // Legacy read this flag from a JSON file on the ESS server, so it could not differ per tenant. It is
        // a tenant setting here, which is the whole reason it is not a [Required] annotation on the DTO.
        var meId = CreateCurrentUserEmployee("PPW120");
        var documentId = CreatePassportFor(meId);

        // Off (no settings row at all -- a tenant that has never opened the self-service screen).
        var allowed = await _withdrawals.RequestPassportWithdrawal(new RequestPassportWithdrawalInput
        {
            EmployeeDocumentId = documentId,
            WithdrawDate = DateTime.Today,
            ReturnDate = DateTime.Today.AddDays(7),
            WithdrawalReason = "   "
        });

        allowed.TransactionId.ShouldBeGreaterThan(0);

        // On.
        UsingDbContext(context =>
        {
            context.Set<ManageSelfServiceSettings>().Add(new ManageSelfServiceSettings
            {
                TenantId = TenantId,
                IsWithdrawalReasonMandatory = true
            });
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _withdrawals.RequestPassportWithdrawal(new RequestPassportWithdrawalInput
            {
                EmployeeDocumentId = documentId,
                WithdrawDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(7),
                WithdrawalReason = null
            }));
    }

    [Fact]
    public async Task A_Submitted_Withdrawal_Should_Be_Hidden_Until_It_Is_Approved()
    {
        // The point of the single-row design, and the reason the legacy copy step is gone: the pending
        // request lives in the movement table itself, so it must be the ApprovedOnly filter -- not a second
        // table -- that keeps HR's movement log from showing a passport as out before anyone agreed to it.
        var meId = CreateCurrentUserEmployee("PPW200");
        EnableWorkflow("PPW200APP");
        var documentId = CreatePassportFor(meId);

        var result = await _withdrawals.RequestPassportWithdrawal(Input(documentId));

        result.SubmittedForApproval.ShouldBeTrue();
        result.WorkflowRequestId.ShouldNotBeNull();

        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeDocumentMvmt>>();
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();

        using (unitOfWorkManager.Begin())
        {
            var visibleToOthers = repository.GetAll().Any(m => m.Id == result.TransactionId);
            visibleToOthers.ShouldBeFalse("A pending withdrawal was visible as a real document movement.");
        }

        // But the employee sees their own.
        var mine = await _withdrawals.GetMyPassportWithdrawals(ListInput());
        mine.Items.Select(i => i.Id).ShouldContain(result.TransactionId);
    }

    [Fact]
    public async Task An_Approved_Withdrawal_Should_Become_A_Real_Movement()
    {
        var meId = CreateCurrentUserEmployee("PPW210");
        var approverId = EnableWorkflow("PPW210APP");
        var documentId = CreatePassportFor(meId);

        var result = await _withdrawals.RequestPassportWithdrawal(Input(documentId));

        await Resolve<IWorkflowApprovalService>().ApproveAsync(result.WorkflowRequestId.Value, approverId, "ok");

        var row = UsingDbContext(c => c.Set<EmployeeDocumentMvmt>().IgnoreQueryFilters()
            .Single(m => m.Id == result.TransactionId));

        row.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        row.EmployeeId.ShouldBe(meId);
        row.EmployeeDocumentId.ShouldBe(documentId);
    }

    [Fact]
    public async Task Approving_An_Extension_Should_Close_The_Earlier_Open_Request()
    {
        // The legacy "extension" rule, ported deliberately. Somebody whose passport is already out and who
        // needs it another week raises a second request rather than being blocked; approving it supersedes
        // the first, which must not be left sitting in an approver's inbox for a period that has been
        // overtaken.
        var meId = CreateCurrentUserEmployee("PPW300");
        var approverId = EnableWorkflow("PPW300APP");
        var documentId = CreatePassportFor(meId);

        var first = await _withdrawals.RequestPassportWithdrawal(Input(documentId, 1, 10));
        var second = await _withdrawals.RequestPassportWithdrawal(Input(documentId, 1, 24));

        // Raising the second is allowed while the first is still open -- that is the extension, and blocking
        // it would leave "withdraw the first and start again" as the only route, losing its history.
        first.WorkflowRequestId.ShouldNotBeNull();
        second.WorkflowRequestId.ShouldNotBeNull();
        UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == first.WorkflowRequestId.Value))
            .RequestStatus.ShouldBe(RequestStatus.Open);

        await Resolve<IWorkflowApprovalService>().ApproveAsync(second.WorkflowRequestId.Value, approverId, "extended");

        var firstRequest = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == first.WorkflowRequestId.Value));
        var secondRequest = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == second.WorkflowRequestId.Value));

        firstRequest.RequestStatus.ShouldBe(RequestStatus.Closed,
            "the superseded request must be closed, not left waiting on an approver");
        secondRequest.RequestStatus.ShouldBe(RequestStatus.Approved);

        // And the superseded row stays hidden. Stamping it Closed to match its request would be the tempting
        // symmetry and would be wrong -- Closed counts as effective, so the employee would end up with two
        // live withdrawal periods for one passport, one of which nobody ever approved.
        var supersededRow = UsingDbContext(c => c.Set<EmployeeDocumentMvmt>().IgnoreQueryFilters()
            .Single(m => m.Id == first.TransactionId));

        supersededRow.WorkflowStatus.ShouldNotBe(RequestStatus.Approved);
        supersededRow.WorkflowStatus.ShouldNotBe(RequestStatus.Closed);
    }

    [Fact]
    public async Task Approving_Should_Not_Touch_Another_Employees_Open_Request()
    {
        // The closing rule is per employee. A withdrawal approved for one person must not close somebody
        // else's -- legacy carried an explicit second identity check in that loop for exactly this reason.
        var meId = CreateCurrentUserEmployee("PPW310");
        var approverId = EnableWorkflow("PPW310APP");
        var documentId = CreatePassportFor(meId);
        var otherId = CreateEmployee("PPW310OTHER");

        int theirRequestId = 0;
        UsingDbContext(context =>
        {
            var request = context.Set<WorkflowRequest>().Add(new WorkflowRequest
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                RequestType = "EmployeeDocumentMvmt",
                RequestNumber = "PPW-" + DateTime.Today.Year + "-09999",
                RequestStatus = RequestStatus.Open,
                WorkflowStep = 1
            }).Entity;

            context.SaveChanges();
            theirRequestId = request.Id;
        });

        var mine = await _withdrawals.RequestPassportWithdrawal(Input(documentId));

        await Resolve<IWorkflowApprovalService>().ApproveAsync(mine.WorkflowRequestId.Value, approverId, "ok");

        var theirs = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == theirRequestId));
        theirs.RequestStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task Should_Only_List_My_Own_Withdrawals()
    {
        var meId = CreateCurrentUserEmployee("PPW400");
        var otherId = CreateEmployee("PPW400OTHER");
        var theirDocumentId = CreatePassportFor(otherId);

        UsingDbContext(context =>
        {
            context.Set<EmployeeDocumentMvmt>().Add(new EmployeeDocumentMvmt
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                EmployeeDocumentId = theirDocumentId,
                WithdrawDate = DateTime.Today,
                ReturnDate = DateTime.Today.AddDays(5)
            });
        });

        var mine = await _withdrawals.GetMyPassportWithdrawals(ListInput());

        mine.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Withdrawing_Should_Remove_The_Row_And_Close_Its_Request()
    {
        var meId = CreateCurrentUserEmployee("PPW500");
        EnableWorkflow("PPW500APP");
        var documentId = CreatePassportFor(meId);

        var result = await _withdrawals.RequestPassportWithdrawal(Input(documentId));

        await _withdrawals.CancelMyPassportWithdrawal(new EntityDto(result.TransactionId));

        var rowExists = UsingDbContext(c => c.Set<EmployeeDocumentMvmt>().IgnoreQueryFilters()
            .Any(m => m.Id == result.TransactionId && !m.IsDeleted));
        rowExists.ShouldBeFalse();

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));
        request.RequestStatus.ShouldBe(RequestStatus.Closed);
    }

    [Fact]
    public async Task A_Withdrawal_Should_Be_Numbered_With_Its_Own_Prefix_And_Routed_By_Document_Type()
    {
        var meId = CreateCurrentUserEmployee("PPW600");
        EnableWorkflow("PPW600APP");
        var documentId = CreatePassportFor(meId);

        var documentTypeId = UsingDbContext(c => c.Set<EmployeeDocument>().IgnoreQueryFilters()
            .Single(d => d.Id == documentId).DocumentTypeId);

        var result = await _withdrawals.RequestPassportWithdrawal(Input(documentId));

        var request = UsingDbContext(c => c.Set<WorkflowRequest>().Single(r => r.Id == result.WorkflowRequestId.Value));

        request.RequestNumber.ShouldStartWith("PPW-" + DateTime.Today.Year);

        // Routing by document type, or a chain configured for passports specifically could never match.
        request.RequestSubTypeId.ShouldBe(documentTypeId);
    }

    [Fact]
    public async Task The_List_Should_Show_The_Document_And_Guarantor_Rather_Than_Bare_Ids()
    {
        // Proves the Includes are wired -- without them the screen shows a row of blanks.
        var meId = CreateCurrentUserEmployee("PPW700");
        var guarantorId = CreateEmployee("PPW700GUAR");
        var documentId = CreatePassportFor(meId);

        await _withdrawals.RequestPassportWithdrawal(new RequestPassportWithdrawalInput
        {
            EmployeeDocumentId = documentId,
            WithdrawDate = DateTime.Today,
            ReturnDate = DateTime.Today.AddDays(7),
            GuarantorId = guarantorId,
            WithdrawalReason = "Embassy appointment"
        });

        var listed = await _withdrawals.GetMyPassportWithdrawals(ListInput());
        var row = listed.Items.Single();

        row.DocumentTypeNameEn.ShouldBe("Passport");
        row.DocumentNum.ShouldNotBeNullOrWhiteSpace();
        row.GuarantorId.ShouldBe(guarantorId);
        row.GuarantorNameEn.ShouldBe("PPW700GUAR T");

        // Still out: default(DateTime) on the row is reported as null rather than as year one.
        row.ActualReturnDate.ShouldBeNull();
    }

    // ---------------------------------------------------------------- legacy parity fixes

    [Fact]
    public async Task A_Withdrawal_Without_Dates_Should_Be_Refused()
    {
        // Legacy refused a blank withdraw or return date outright; only the screen was stopping it here.
        var meId = CreateCurrentUserEmployee("PPW810");
        var documentId = CreatePassportFor(meId);

        await Should.ThrowAsync<UserFriendlyException>(() => _withdrawals.RequestPassportWithdrawal(
            new RequestPassportWithdrawalInput { EmployeeDocumentId = documentId, WithdrawalReason = "Visa" }));
    }

    [Fact]
    public async Task A_Document_That_Is_Already_Out_Should_Not_Be_Withdrawn_Again()
    {
        // Legacy: "The passport is already out" -- refused unless an extension is in flight.
        var meId = CreateCurrentUserEmployee("PPW820");
        var approverId = EnableWorkflow("PPW820APP");
        var documentId = CreatePassportFor(meId);

        var first = await _withdrawals.RequestPassportWithdrawal(Input(documentId));
        await Resolve<IWorkflowApprovalService>().ApproveAsync(first.WorkflowRequestId.Value, approverId, "ok");

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _withdrawals.RequestPassportWithdrawal(Input(documentId, 2, 20)));
    }

    [Fact]
    public async Task Approving_A_Withdrawal_Should_Leave_One_Open_Movement()
    {
        // Legacy's approval overwrote the document's open movement instead of adding a second one. Two open
        // movements for one document read as out even after it is handed back, and would then block every
        // future withdrawal. The case that reaches it: HR records a withdrawal by hand while the employee's
        // request is already in flight.
        var meId = CreateCurrentUserEmployee("PPW830");
        var approverId = EnableWorkflow("PPW830APP");
        var documentId = CreatePassportFor(meId);

        var request = await _withdrawals.RequestPassportWithdrawal(Input(documentId, 1, 10));

        UsingDbContext(context => context.Set<EmployeeDocumentMvmt>().Add(new EmployeeDocumentMvmt
        {
            TenantId = TenantId,
            EmployeeId = meId,
            EmployeeDocumentId = documentId,
            WithdrawDate = DateTime.Today,
            ReturnDate = DateTime.Today.AddDays(5),
            ActualReturnDate = default,
            WorkflowStatus = RequestStatus.Approved
        }));

        await Resolve<IWorkflowApprovalService>().ApproveAsync(request.WorkflowRequestId.Value, approverId, "ok");

        // !IsDeleted spelled out: this query runs outside a unit of work, where the soft-delete filter is
        // not applied, so a removed row would otherwise still be counted.
        var openMovements = UsingDbContext(c => c.Set<EmployeeDocumentMvmt>()
            .Count(m => m.EmployeeDocumentId == documentId && m.ActualReturnDate == default && !m.IsDeleted));

        openMovements.ShouldBe(1, "the approved withdrawal replaces the period rather than stacking a second one");
    }

    [Fact]
    public async Task A_Superseded_Request_Should_No_Longer_Offer_Withdraw()
    {
        // Its row stays Open by design, but its request was closed when the extension was approved, so the
        // button would do nothing.
        var meId = CreateCurrentUserEmployee("PPW850");
        var approverId = EnableWorkflow("PPW850APP");
        var documentId = CreatePassportFor(meId);

        var first = await _withdrawals.RequestPassportWithdrawal(Input(documentId, 1, 10));
        var second = await _withdrawals.RequestPassportWithdrawal(Input(documentId, 1, 24));
        await Resolve<IWorkflowApprovalService>().ApproveAsync(second.WorkflowRequestId.Value, approverId, "extended");

        var listed = await _withdrawals.GetMyPassportWithdrawals(
            new GetMyRequestsInput { IncludeHistory = true, MaxResultCount = 50, SkipCount = 0 });

        listed.Items.Single(i => i.Id == first.TransactionId).CanCancel.ShouldBeFalse();
    }

    [Fact]
    public async Task Naming_A_Guarantor_Should_Record_Them_On_The_Request()
    {
        // Legacy let the employee name a guarantor on this form and mailed them. The mail itself is a no-op
        // unless the tenant has configured PassportGuarantor_Notification, which the test database has not.
        var meId = CreateCurrentUserEmployee("PPW860");
        var guarantorId = CreateEmployee("PPW860GUAR");
        var documentId = CreatePassportFor(meId);

        var input = Input(documentId);
        input.GuarantorId = guarantorId;

        var result = await _withdrawals.RequestPassportWithdrawal(input);

        var row = UsingDbContext(c => c.Set<EmployeeDocumentMvmt>().IgnoreQueryFilters()
            .Single(m => m.Id == result.TransactionId));

        row.GuarantorId.ShouldBe(guarantorId);
    }

    [Fact]
    public async Task Approving_A_Withdrawal_Should_Not_Cancel_A_Pending_Return()
    {
        // A return rides the same request type. Closing every open request of that type on approval also
        // cancelled the employee's pending hand-back, which is data loss: the document stays recorded as
        // out and the employee has to ask again.
        var meId = CreateCurrentUserEmployee("PPW840");
        var approverId = EnableWorkflow("PPW840APP");
        var firstDocumentId = CreatePassportFor(meId);
        var secondDocumentId = CreatePassportFor(meId);

        var withdrawal = await _withdrawals.RequestPassportWithdrawal(Input(firstDocumentId));
        await Resolve<IWorkflowApprovalService>().ApproveAsync(withdrawal.WorkflowRequestId.Value, approverId, "ok");

        var returnRequest = await _withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = withdrawal.TransactionId,
            ActualReturnDate = DateTime.Today.AddDays(2)
        });

        var other = await _withdrawals.RequestPassportWithdrawal(Input(secondDocumentId));
        await Resolve<IWorkflowApprovalService>().ApproveAsync(other.WorkflowRequestId.Value, approverId, "ok");

        var pendingReturn = UsingDbContext(c => c.Set<WorkflowRequest>()
            .Single(r => r.Id == returnRequest.WorkflowRequestId));

        pendingReturn.RequestStatus.ShouldBe(RequestStatus.Open, "a pending return is not a superseded withdrawal");
    }
}
