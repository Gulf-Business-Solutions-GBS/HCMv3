using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// The RETURN leg of a passport withdrawal -- legacy's second <c>MovementType</c> on
/// <c>EmployeeDocMvmtRequest</c>, ported as an AMENDMENT rather than as a request type of its own.
///
/// <para>
/// It stamps one field, <c>ActualReturnDate</c>, on a movement row that is already approved and live. That
/// is what an amendment is, so there is no entity, no table and no catalog row here -- it rides the existing
/// <c>EmployeeDocumentMvmt</c> type and its existing chain.
/// </para>
///
/// <para>
/// The test that matters most is the rejected one. A refused return must leave the movement exactly as it
/// was: the document stays recorded as out, which is the only safe reading when nobody has confirmed it is
/// back. Legacy could not get this wrong because a rejected request simply never reached its copy step; here
/// the request IS the movement, so it has to be the amendment rule that protects it.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyPassportWithdrawalReturn_Tests : AppTestBase
{
    private readonly IMyPassportWithdrawalAppService _withdrawals;
    private readonly IWorkflowApprovalService _approvals;

    public MyPassportWithdrawalReturn_Tests()
    {
        LoginAsDefaultTenantAdmin();
        _withdrawals = Resolve<IMyPassportWithdrawalAppService>();
        _approvals = Resolve<IWorkflowApprovalService>();
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

    /// <summary>A one-step chain with a real approver, and the passport catalog row switched on.</summary>
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
                Code = "PPW-RET",
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

    private static RequestPassportWithdrawalInput WithdrawalInput(int documentId) =>
        new RequestPassportWithdrawalInput
        {
            EmployeeDocumentId = documentId,
            WithdrawDate = DateTime.Today.AddDays(-10),
            ReturnDate = DateTime.Today.AddDays(4),
            WithdrawalReason = "Visa application"
        };

    /// <summary>Raises a withdrawal and takes it to approved, which is the state a return needs.</summary>
    private async Task<int> CreateApprovedWithdrawalAsync(int documentId, int approverId)
    {
        var result = await _withdrawals.RequestPassportWithdrawal(WithdrawalInput(documentId));

        await _approvals.ApproveAsync(result.WorkflowRequestId.Value, approverId, "ok");

        return result.TransactionId;
    }

    private EmployeeDocumentMvmt MovementRow(int movementId)
    {
        return UsingDbContext(context => context.Set<EmployeeDocumentMvmt>()
            .IgnoreQueryFilters()
            .Single(m => m.Id == movementId));
    }

    [Fact]
    public async Task Returning_Should_Stamp_The_Actual_Return_Date_Only_On_Approval()
    {
        // The core promise, in the shape it takes here: until an approver agrees, the movement log still
        // says the passport is out, because it still is.
        var meId = CreateCurrentUserEmployee("PPR100");
        var approverId = EnableWorkflow("PPR100APP");
        var documentId = CreatePassportFor(meId);

        var movementId = await CreateApprovedWithdrawalAsync(documentId, approverId);

        var handedBack = DateTime.Today;

        var result = await _withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movementId,
            ActualReturnDate = handedBack
        });

        result.WorkflowRequestId.ShouldBeGreaterThan(0);

        // Raised but not granted: nothing on the row has moved.
        MovementRow(movementId).ActualReturnDate.ShouldBe(default(DateTime),
            "a return that nobody has approved must not record the document as back");

        await _approvals.ApproveAsync(result.WorkflowRequestId, approverId, "received");

        var after = MovementRow(movementId);
        after.ActualReturnDate.Date.ShouldBe(handedBack.Date);

        // And the withdrawal it amended is still an approved movement -- the amendment changed what the row
        // says, not whether it counts.
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task A_Rejected_Return_Should_Leave_The_Movement_Completely_Untouched()
    {
        // THE important one. Refusing a return must never damage the withdrawal it was raised against: the
        // employee still has the passport, the movement must still say so, and the approved withdrawal must
        // not be turned into a rejected one as a side effect of declining a change to it.
        var meId = CreateCurrentUserEmployee("PPR200");
        var approverId = EnableWorkflow("PPR200APP");
        var documentId = CreatePassportFor(meId);

        var movementId = await CreateApprovedWithdrawalAsync(documentId, approverId);

        var before = MovementRow(movementId);

        var result = await _withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movementId,
            ActualReturnDate = DateTime.Today
        });

        await _approvals.RejectAsync(result.WorkflowRequestId, approverId, "not received");

        var after = MovementRow(movementId);

        after.ActualReturnDate.ShouldBe(default(DateTime),
            "a rejected return recorded the document as handed back anyway");
        after.ActualReturnDate.ShouldBe(before.ActualReturnDate);
        after.WithdrawDate.ShouldBe(before.WithdrawDate);
        after.ReturnDate.ShouldBe(before.ReturnDate);
        after.EmployeeDocumentId.ShouldBe(before.EmployeeDocumentId);
        after.WorkflowStatus.ShouldBe(RequestStatus.Approved,
            "refusing a return must not take the approved withdrawal away from the employee");
    }

    [Fact]
    public async Task Returning_A_Document_That_Is_Already_Back_Should_Be_Refused()
    {
        // Legacy silently did nothing here -- it looked for a movement with no actual return date, found
        // none, and saved an approved request that changed no data at all. Two returns then left nobody able
        // to say which date was the real one.
        var meId = CreateCurrentUserEmployee("PPR300");
        var approverId = EnableWorkflow("PPR300APP");
        var documentId = CreatePassportFor(meId);

        var movementId = await CreateApprovedWithdrawalAsync(documentId, approverId);

        var first = await _withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movementId,
            ActualReturnDate = DateTime.Today.AddDays(-1)
        });

        await _approvals.ApproveAsync(first.WorkflowRequestId, approverId, "received");

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _withdrawals.ReturnMyDocument(new ReturnDocumentInput
            {
                EmployeeDocumentMvmtId = movementId,
                ActualReturnDate = DateTime.Today
            }));

        // And the first return's date is the one that stands.
        MovementRow(movementId).ActualReturnDate.Date.ShouldBe(DateTime.Today.AddDays(-1).Date);
    }

    [Fact]
    public async Task Should_Refuse_A_Return_Date_Before_The_Withdrawal_Date()
    {
        // A document that comes back before it went out is a custody window running backwards -- there is no
        // period an approver could be agreeing to.
        var meId = CreateCurrentUserEmployee("PPR400");
        var approverId = EnableWorkflow("PPR400APP");
        var documentId = CreatePassportFor(meId);

        var movementId = await CreateApprovedWithdrawalAsync(documentId, approverId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _withdrawals.ReturnMyDocument(new ReturnDocumentInput
            {
                EmployeeDocumentMvmtId = movementId,
                ActualReturnDate = DateTime.Today.AddDays(-30)
            }));

        MovementRow(movementId).ActualReturnDate.ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task CanReturn_Should_Be_False_While_Pending_And_True_Once_Approved()
    {
        // The flag is what lets the screen offer the button only when it means something. Before approval
        // the document was never released, so there is nothing to hand back.
        var meId = CreateCurrentUserEmployee("PPR500");
        var approverId = EnableWorkflow("PPR500APP");
        var documentId = CreatePassportFor(meId);

        var submitted = await _withdrawals.RequestPassportWithdrawal(WithdrawalInput(documentId));

        var pending = (await _withdrawals.GetMyPassportWithdrawals(ListInput()))
            .Items.Single(i => i.Id == submitted.TransactionId);

        pending.WorkflowStatus.ShouldBe(RequestStatus.Open);
        pending.CanReturn.ShouldBeFalse("a withdrawal nobody has approved has released no document to return");

        await _approvals.ApproveAsync(submitted.WorkflowRequestId.Value, approverId, "ok");

        var approved = (await _withdrawals.GetMyPassportWithdrawals(ListInput()))
            .Items.Single(i => i.Id == submitted.TransactionId);

        approved.CanReturn.ShouldBeTrue();

        // And it closes again once the document is actually back, so the button cannot invite a second
        // return date over the first.
        var returned = await _withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = submitted.TransactionId,
            ActualReturnDate = DateTime.Today
        });

        await _approvals.ApproveAsync(returned.WorkflowRequestId, approverId, "received");

        var closed = (await _withdrawals.GetMyPassportWithdrawals(ListInput()))
            .Items.Single(i => i.Id == submitted.TransactionId);

        closed.CanReturn.ShouldBeFalse();
        closed.ActualReturnDate.ShouldNotBeNull();
    }

    [Fact]
    public async Task Should_Refuse_Returning_Somebody_Elses_Document()
    {
        // Ownership is re-checked on the write, not merely in the list query. Returning somebody else's
        // passport on paper while it is still in their drawer is exactly the failure that check exists for.
        CreateCurrentUserEmployee("PPR600");
        var otherId = CreateEmployee("PPR600OTHER");
        var theirDocumentId = CreatePassportFor(otherId);

        int theirMovementId = 0;
        UsingDbContext(context =>
        {
            var movement = context.Set<EmployeeDocumentMvmt>().Add(new EmployeeDocumentMvmt
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                EmployeeDocumentId = theirDocumentId,
                WithdrawDate = DateTime.Today.AddDays(-5),
                ReturnDate = DateTime.Today.AddDays(5),
                WorkflowStatus = RequestStatus.Approved
            }).Entity;

            context.SaveChanges();
            theirMovementId = movement.Id;
        });

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _withdrawals.ReturnMyDocument(new ReturnDocumentInput
            {
                EmployeeDocumentMvmtId = theirMovementId,
                ActualReturnDate = DateTime.Today
            }));

        MovementRow(theirMovementId).ActualReturnDate.ShouldBe(default(DateTime));
    }
}
