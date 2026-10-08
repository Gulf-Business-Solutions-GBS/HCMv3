using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Workflow;
using HCMv3.SystemSecurity;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// The custody trail on the self-service side: a return that is labelled as one, carries remarks, records
/// who took the document back, and stops being offered while it is waiting.
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyPassportWithdrawalCustody_Tests : PassportWithdrawalTestBase
{
    private const long ApproverUserId = 990001;

    [Fact]
    public async Task Should_Refuse_A_Return_Date_In_The_Future()
    {
        var meId = CreateCurrentUserEmployee("PPC100");
        var approverId = EnableWorkflow("PPC100APP");
        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        await Should.ThrowAsync<UserFriendlyException>(() => Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movement.TransactionId,
            ActualReturnDate = DateTime.Today.AddDays(1)
        }));

        MovementRow(movement.TransactionId).ActualReturnDate.ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task A_Return_Should_Be_Raised_Labelled_As_A_Return()
    {
        var meId = CreateCurrentUserEmployee("PPC110");
        var approverId = EnableWorkflow("PPC110APP");
        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        var result = await Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movement.TransactionId,
            ActualReturnDate = DateTime.Today
        });

        var request = RequestRow(result.WorkflowRequestId);

        request.IsAmendment.ShouldBeTrue();
        request.RequestAction.ShouldBe(WorkflowRequestAction.Return);
    }

    [Fact]
    public async Task An_Approved_Return_Should_Store_The_Remarks()
    {
        var meId = CreateCurrentUserEmployee("PPC120");
        var approverId = EnableWorkflow("PPC120APP");
        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        var result = await Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movement.TransactionId,
            ActualReturnDate = DateTime.Today,
            ReturnRemarks = "  Cover slightly torn  "
        });

        // Proposed, not applied: nothing is on the row until somebody agrees.
        MovementRow(movement.TransactionId).ReturnRemarks.ShouldBeNull();

        await Approvals.ApproveAsync(result.WorkflowRequestId, approverId, "received");

        var row = MovementRow(movement.TransactionId);
        row.ReturnRemarks.ShouldBe("Cover slightly torn");
        row.ActualReturnDate.Date.ShouldBe(DateTime.Today);

        var listed = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == movement.TransactionId);
        listed.ReturnRemarks.ShouldBe("Cover slightly torn");
    }

    [Fact]
    public async Task An_Approved_Return_Should_Record_The_Approvers_User_As_The_Receiver()
    {
        var meId = CreateCurrentUserEmployee("PPC130");
        var approverId = EnableWorkflow("PPC130APP", ApproverUserId);
        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        var result = await Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movement.TransactionId,
            ActualReturnDate = DateTime.Today
        });

        await Approvals.ApproveAsync(result.WorkflowRequestId, approverId, "received");

        MovementRow(movement.TransactionId).ReceivedByUserId.ShouldBe(ApproverUserId);
    }

    [Fact]
    public async Task An_Approver_With_No_Login_Should_Leave_The_Receiver_Empty()
    {
        var meId = CreateCurrentUserEmployee("PPC140");
        var approverId = EnableWorkflow("PPC140APP");
        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        var result = await Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movement.TransactionId,
            ActualReturnDate = DateTime.Today
        });

        await Approvals.ApproveAsync(result.WorkflowRequestId, approverId, "received");

        var row = MovementRow(movement.TransactionId);
        row.ActualReturnDate.Date.ShouldBe(DateTime.Today);
        row.ReceivedByUserId.ShouldBeNull();
    }

    [Fact]
    public async Task A_Return_Stored_Under_The_Generic_Amendment_Label_Should_Still_Apply()
    {
        // Returns raised before the label existed carry RequestAction = Amendment and a payload with no
        // remarks. They are waiting in inboxes now and must apply exactly as they would have.
        var meId = CreateCurrentUserEmployee("PPC150");
        var approverId = EnableWorkflow("PPC150APP");
        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        var handedBack = DateTime.Today.AddDays(-1);
        int requestId;

        var unitOfWorkManager = Resolve<IUnitOfWorkManager>();
        using (var unitOfWork = unitOfWorkManager.Begin())
        {
            requestId = await Resolve<IWorkflowAmendmentService>().CreateAndSubmitAsync(
                PassportWithdrawalWorkflowTransactionHandler.PassportWithdrawalRequestType,
                movement.TransactionId,
                meId,
                "{\"ActualReturnDate\":" + JsonSerializer.Serialize(handedBack) + "}",
                meId);

            await unitOfWork.CompleteAsync();
        }

        RequestRow(requestId).RequestAction.ShouldBe(WorkflowRequestAction.Amendment);

        await Approvals.ApproveAsync(requestId, approverId, "received");

        var row = MovementRow(movement.TransactionId);
        row.ActualReturnDate.Date.ShouldBe(handedBack);
        row.ReturnRemarks.ShouldBeNull();
    }

    [Fact]
    public async Task CanReturn_Should_Be_False_While_A_Return_Is_Awaiting_Approval()
    {
        var meId = CreateCurrentUserEmployee("PPC200");
        var approverId = EnableWorkflow("PPC200APP");
        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        var before = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == movement.TransactionId);
        before.CanReturn.ShouldBeTrue();
        before.HasPendingReturn.ShouldBeFalse();
        before.PendingReturnRequestId.ShouldBeNull();

        var returned = await Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movement.TransactionId,
            ActualReturnDate = DateTime.Today
        });

        var pending = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == movement.TransactionId);
        pending.HasPendingReturn.ShouldBeTrue();
        pending.PendingReturnRequestId.ShouldBe(returned.WorkflowRequestId);
        pending.CanReturn.ShouldBeFalse("a second return would only be refused on submit");

        // A refused return puts the button back: the document is still out and still returnable.
        await Approvals.RejectAsync(returned.WorkflowRequestId, approverId, "not received");

        var after = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == movement.TransactionId);
        after.HasPendingReturn.ShouldBeFalse();
        after.CanReturn.ShouldBeTrue();
    }

    [Fact]
    public async Task The_List_Should_Carry_The_Request_Number_And_The_Documents_Dates()
    {
        var meId = CreateCurrentUserEmployee("PPC210");
        var approverId = EnableWorkflow("PPC210APP");
        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        var listed = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == movement.TransactionId);

        listed.RequestNumber.ShouldBe(RequestRow(movement.WorkflowRequestId.Value).RequestNumber);
        listed.RequestNumber.ShouldNotBeNullOrWhiteSpace();
        listed.IssueDate.ShouldBe(DateTime.Today.AddYears(-2));
        listed.ExpiryDate.ShouldBe(DateTime.Today.AddYears(5));
        listed.ActualWithdrawDate.ShouldBeNull();
        listed.IsOverdue.ShouldBeFalse();
        listed.Extensions.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_List_Should_Flag_A_Withdrawal_That_Is_Past_Its_Due_Date()
    {
        var meId = CreateCurrentUserEmployee("PPC220");
        var documentId = CreatePassportFor(meId);
        var otherDocumentId = CreatePassportFor(meId);

        var overdueId = InsertMovement(meId, documentId, RequestStatus.Approved, outInDays: -20, backInDays: -3);

        // Past due but back already, and past due but never approved: neither is overdue.
        var returnedId = InsertMovement(meId, otherDocumentId, RequestStatus.Approved, outInDays: -20, backInDays: -3,
            actualReturnDate: DateTime.Today.AddDays(-4));
        var pendingId = InsertMovement(meId, otherDocumentId, RequestStatus.Open, outInDays: -20, backInDays: -3);

        var items = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items;

        items.Single(i => i.Id == overdueId).IsOverdue.ShouldBeTrue();
        items.Single(i => i.Id == returnedId).IsOverdue.ShouldBeFalse();
        items.Single(i => i.Id == pendingId).IsOverdue.ShouldBeFalse();
    }

    [Fact]
    public async Task HR_Should_Not_Record_A_Return_While_The_Employees_Return_Is_Awaiting_Approval()
    {
        var meId = CreateCurrentUserEmployee("PPC300");
        var approverId = EnableWorkflow("PPC300APP");
        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        var returned = await Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = movement.TransactionId,
            ActualReturnDate = DateTime.Today
        });

        var hr = Resolve<IEmployeeDocumentMvmtsAppService>();

        await Should.ThrowAsync<UserFriendlyException>(() => hr.RecordReturn(new RecordDocumentReturnInput
        {
            Id = movement.TransactionId,
            ActualReturnDate = DateTime.Today
        }));

        MovementRow(movement.TransactionId).ActualReturnDate.ShouldBe(default(DateTime));

        // Once the request is out of the way, the counter route is open again.
        await Approvals.RejectAsync(returned.WorkflowRequestId, approverId, "not received");

        await hr.RecordReturn(new RecordDocumentReturnInput
        {
            Id = movement.TransactionId,
            ActualReturnDate = DateTime.Today
        });

        MovementRow(movement.TransactionId).ActualReturnDate.Date.ShouldBe(DateTime.Today);
    }

    // ---------------------------------------------------------------- the document dropdown

    [Fact]
    public async Task The_Lookup_Should_Offer_Only_My_Documents_Of_The_Withdrawable_Type()
    {
        // The default withdrawable type is the one whose code is "2".
        var meId = CreateCurrentUserEmployee("PPC400");
        var otherId = CreateEmployee("PPC400OTHER");

        var passportTypeId = CreateDocumentType("2", "Passport");
        var certificateTypeId = CreateDocumentType("PPC400C", "Certificate");

        var myPassportId = CreateDocument(meId, passportTypeId, "P-MINE");
        CreateDocument(meId, certificateTypeId, "C-MINE");
        CreateDocument(otherId, passportTypeId, "P-THEIRS");

        var offered = await Resolve<IEssLookupAppService>().GetMyWithdrawableDocuments();

        var only = offered.ShouldHaveSingleItem();
        only.Id.ShouldBe(myPassportId);
        only.DocumentNum.ShouldBe("P-MINE");
        only.DocumentTypeId.ShouldBe(passportTypeId);
        only.DocumentTypeNameEn.ShouldBe("Passport");
        only.IssueDate.ShouldBe(DateTime.Today.AddYears(-2));
        only.ExpiryDate.ShouldBe(DateTime.Today.AddYears(5));
    }

    [Fact]
    public async Task The_Lookup_Should_Not_Restrict_By_Type_When_No_Withdrawable_Type_Exists()
    {
        // The same rule as the request: with no type carrying the configured code, nothing is restricted --
        // but the list is still only ever the caller's own documents.
        var meId = CreateCurrentUserEmployee("PPC410");
        var otherId = CreateEmployee("PPC410OTHER");

        var firstTypeId = CreateDocumentType("PPC410A", "Passport");
        var secondTypeId = CreateDocumentType("PPC410B", "Certificate");

        var firstId = CreateDocument(meId, firstTypeId);
        var secondId = CreateDocument(meId, secondTypeId);
        CreateDocument(otherId, firstTypeId);

        var offered = await Resolve<IEssLookupAppService>().GetMyWithdrawableDocuments();

        offered.Select(d => d.Id).OrderBy(id => id).ShouldBe(new[] { firstId, secondId }.OrderBy(id => id));
    }

    [Fact]
    public async Task The_Lookup_Should_Follow_The_Tenants_Configured_Document_Type_Code()
    {
        var meId = CreateCurrentUserEmployee("PPC420");

        var defaultTypeId = CreateDocumentType("2", "Passport");
        var configuredTypeId = CreateDocumentType("PPC420ID", "Civil ID");

        CreateDocument(meId, defaultTypeId);
        var civilIdDocumentId = CreateDocument(meId, configuredTypeId);

        UsingDbContext(context =>
        {
            context.Set<SysTenantConfig>().Add(new SysTenantConfig
            {
                TenantId = TenantId,
                Key = MyPassportWithdrawalAppService.WithdrawableDocumentTypeCodeKey,
                KeyValue = "PPC420ID",
                StartDate = new DateTime(1800, 1, 1),
                EndDate = new DateTime(9999, 12, 31)
            });
        });

        var offered = await Resolve<IEssLookupAppService>().GetMyWithdrawableDocuments();

        offered.ShouldHaveSingleItem().Id.ShouldBe(civilIdDocumentId);
    }

    [Fact]
    public async Task The_Lookup_Should_Be_Empty_For_A_User_With_No_Employee_Record()
    {
        var offered = await Resolve<IEssLookupAppService>().GetMyWithdrawableDocuments();

        offered.ShouldBeEmpty();
    }
}
