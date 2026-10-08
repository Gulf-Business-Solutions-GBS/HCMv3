using System;
using System.Linq;
using System.Threading.Tasks;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// The EXTENSION, as legacy actually behaves (<c>PassportWithdrawalRequestController</c>, ~601-659; its
/// "already out" refusal at ~264-277 is commented out). An employee whose document is already out may ask
/// again; on approval the new request replaces the open movement's dates and the employee's other open
/// withdrawal requests are closed with a comment naming the extension.
///
/// <para>
/// What differs from legacy is only what is kept. Legacy overwrote the dates on the one movement row, so the
/// earlier due date was gone. Here each request is its own movement row: the replaced one is soft-deleted,
/// the new one points at it, and the earlier due date stays readable.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyPassportWithdrawalExtension_Tests : PassportWithdrawalTestBase
{
    [Fact]
    public async Task A_Second_Request_Should_Be_Allowed_While_The_Document_Is_Out()
    {
        // No other request is open, which is exactly the case the old refusal caught.
        var meId = CreateCurrentUserEmployee("PPX100");
        var approverId = EnableWorkflow("PPX100APP");
        var documentId = CreatePassportFor(meId);

        await CreateApprovedWithdrawalAsync(documentId, approverId);

        var extension = await Withdrawals.RequestPassportWithdrawal(WithdrawalInput(documentId, -10, 20));

        extension.SubmittedForApproval.ShouldBeTrue();
        RequestRow(extension.WorkflowRequestId.Value).RequestStatus.ShouldBe(RequestStatus.Open);
    }

    [Fact]
    public async Task Approving_An_Extension_Should_Supersede_The_Open_Movement_And_Link_To_It()
    {
        var meId = CreateCurrentUserEmployee("PPX200");
        var approverId = EnableWorkflow("PPX200APP");
        var documentId = CreatePassportFor(meId);

        var first = await CreateApprovedWithdrawalAsync(documentId, approverId, -10, 4);
        var extension = await CreateApprovedWithdrawalAsync(documentId, approverId, -2, 20);

        var replaced = MovementRow(first.TransactionId);
        replaced.IsDeleted.ShouldBeTrue("the replaced movement must not go on reading as a second open period");

        var current = MovementRow(extension.TransactionId);
        current.IsDeleted.ShouldBeFalse();
        current.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        current.ExtendsMovementId.ShouldBe(first.TransactionId);

        // Legacy overwrote both dates with the extension's: the new row's own dates stand.
        current.WithdrawDate.ShouldBe(DateTime.Today.AddDays(-2));
        current.ReturnDate.ShouldBe(DateTime.Today.AddDays(20));

        // One open movement for the document, read the way the rest of the system reads it.
        var unitOfWorkManager = Resolve<Abp.Domain.Uow.IUnitOfWorkManager>();
        var repository = Resolve<Abp.Domain.Repositories.IRepository<EmployeeDocumentMvmt>>();

        using (unitOfWorkManager.Begin())
        {
            repository.GetAll()
                .Where(m => m.EmployeeDocumentId == documentId && m.ActualReturnDate == default)
                .Select(m => m.Id)
                .ToList()
                .ShouldBe(new[] { extension.TransactionId });
        }
    }

    [Fact]
    public async Task Approving_An_Extension_Should_Carry_The_HandOver_Forward()
    {
        // The document never came back, so the hand-over on file is still the original one.
        var meId = CreateCurrentUserEmployee("PPX210");
        var approverId = EnableWorkflow("PPX210APP");
        var documentId = CreatePassportFor(meId);

        var first = await CreateApprovedWithdrawalAsync(documentId, approverId, -10, 4);

        var handedOver = DateTime.Today.AddDays(-9);
        await Resolve<IEmployeeDocumentMvmtsAppService>().RecordHandOver(new RecordDocumentHandOverInput
        {
            Id = first.TransactionId,
            ActualWithdrawDate = handedOver
        });

        var extension = await CreateApprovedWithdrawalAsync(documentId, approverId, -2, 20);

        var current = MovementRow(extension.TransactionId);
        current.ActualWithdrawDate.ShouldBe(handedOver);
        current.HandedOverByUserId.ShouldBe(AbpSession.UserId);
    }

    [Fact]
    public async Task The_Earlier_Due_Date_Should_Be_Readable_In_Extensions()
    {
        var meId = CreateCurrentUserEmployee("PPX220");
        var approverId = EnableWorkflow("PPX220APP");
        var documentId = CreatePassportFor(meId);

        var first = await CreateApprovedWithdrawalAsync(documentId, approverId, -10, 4);
        var second = await CreateApprovedWithdrawalAsync(documentId, approverId, -6, 12);
        var third = await CreateApprovedWithdrawalAsync(documentId, approverId, -2, 20);

        var firstNumber = RequestRow(first.WorkflowRequestId.Value).RequestNumber;
        var secondNumber = RequestRow(second.WorkflowRequestId.Value).RequestNumber;

        // Self-service: only the live movement is listed, with the two it replaced behind it, oldest first.
        var mine = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items;

        mine.Select(i => i.Id).ShouldNotContain(first.TransactionId);
        mine.Select(i => i.Id).ShouldNotContain(second.TransactionId);

        var listed = mine.Single(i => i.Id == third.TransactionId);
        listed.ExtendsMovementId.ShouldBe(second.TransactionId);
        listed.Extensions.Count.ShouldBe(2);

        listed.Extensions[0].Id.ShouldBe(first.TransactionId);
        listed.Extensions[0].WithdrawDate.ShouldBe(DateTime.Today.AddDays(-10));
        listed.Extensions[0].ReturnDate.ShouldBe(DateTime.Today.AddDays(4));
        listed.Extensions[0].RequestNumber.ShouldBe(firstNumber);
        listed.Extensions[0].SupersededOn.ShouldNotBeNull();

        listed.Extensions[1].Id.ShouldBe(second.TransactionId);
        listed.Extensions[1].ReturnDate.ShouldBe(DateTime.Today.AddDays(12));
        listed.Extensions[1].RequestNumber.ShouldBe(secondNumber);

        // HR: the same history on the movement log, and on the single movement the form opens.
        var hr = Resolve<IEmployeeDocumentMvmtsAppService>();

        var logged = (await hr.GetAll(new GetAllEmployeeDocumentMvmtsInput { EmployeeDocumentId = documentId, MaxResultCount = 50 }))
            .Items.Single(i => i.Id == third.TransactionId);

        logged.ExtendsMovementId.ShouldBe(second.TransactionId);
        logged.Extensions.Select(x => x.ReturnDate)
            .ShouldBe(new[] { DateTime.Today.AddDays(4), DateTime.Today.AddDays(12) });

        var opened = await hr.GetEmployeeDocumentMvmtForEdit(new Abp.Application.Services.Dto.EntityDto(third.TransactionId));
        opened.Details.Extensions.Select(x => x.RequestNumber).ShouldBe(new[] { firstNumber, secondNumber });
    }

    [Fact]
    public async Task Extensions_Should_Not_Reach_Another_Employees_Movements()
    {
        // The chain is walked only through rows of the same employee and the same document. A link that
        // somehow pointed elsewhere shows nothing rather than somebody else's dates.
        var meId = CreateCurrentUserEmployee("PPX230");
        var otherId = CreateEmployee("PPX230OTHER");
        var myDocumentId = CreatePassportFor(meId);
        var theirDocumentId = CreatePassportFor(otherId);

        var theirMovementId = InsertMovement(otherId, theirDocumentId, RequestStatus.Approved);
        var myMovementId = InsertMovement(meId, myDocumentId, RequestStatus.Approved);

        UsingDbContext(context =>
        {
            var theirs = context.Set<EmployeeDocumentMvmt>().IgnoreQueryFilters().Single(m => m.Id == theirMovementId);
            theirs.IsDeleted = true;
            theirs.DeletionTime = DateTime.Now;

            var mine = context.Set<EmployeeDocumentMvmt>().IgnoreQueryFilters().Single(m => m.Id == myMovementId);
            mine.ExtendsMovementId = theirMovementId;
        });

        var listed = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == myMovementId);

        listed.Extensions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Other_Open_Requests_Should_Be_Closed_With_The_Extension_Remark()
    {
        var meId = CreateCurrentUserEmployee("PPX300");
        var approverId = EnableWorkflow("PPX300APP");
        var documentId = CreatePassportFor(meId);

        await CreateApprovedWithdrawalAsync(documentId, approverId, -10, 4);

        var overtaken = await Withdrawals.RequestPassportWithdrawal(WithdrawalInput(documentId, -10, 10));
        var extension = await Withdrawals.RequestPassportWithdrawal(WithdrawalInput(documentId, -10, 20));

        await Approvals.ApproveAsync(extension.WorkflowRequestId.Value, approverId, "extended");

        RequestRow(overtaken.WorkflowRequestId.Value).RequestStatus.ShouldBe(RequestStatus.Closed);

        var extensionNumber = RequestRow(extension.WorkflowRequestId.Value).RequestNumber;

        var closing = UsingDbContext(context => context.Set<RequestLog>()
            .Where(l => l.WorkflowRequestId == overtaken.WorkflowRequestId.Value && l.ActionType == RequestActionType.Closed)
            .ToList());

        var entry = closing.ShouldHaveSingleItem();
        entry.RequestActionComment.ShouldBe("Closed due to extension. Extension request number: " + extensionNumber);
        entry.ActionById.ShouldBe(approverId);

        // The extension itself is not closed by its own approval.
        RequestRow(extension.WorkflowRequestId.Value).RequestStatus.ShouldBe(RequestStatus.Approved);
    }

    [Fact]
    public async Task A_Return_On_The_Extended_Movement_Should_Still_Work()
    {
        var meId = CreateCurrentUserEmployee("PPX400");
        var approverId = EnableWorkflow("PPX400APP");
        var documentId = CreatePassportFor(meId);

        await CreateApprovedWithdrawalAsync(documentId, approverId, -10, 4);
        var extension = await CreateApprovedWithdrawalAsync(documentId, approverId, -2, 20);

        var listed = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == extension.TransactionId);
        listed.CanReturn.ShouldBeTrue();

        var returned = await Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = extension.TransactionId,
            ActualReturnDate = DateTime.Today,
            ReturnRemarks = "Back after the extension"
        });

        await Approvals.ApproveAsync(returned.WorkflowRequestId, approverId, "received");

        var row = MovementRow(extension.TransactionId);
        row.ActualReturnDate.Date.ShouldBe(DateTime.Today);
        row.ReturnRemarks.ShouldBe("Back after the extension");
        row.ExtendsMovementId.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_Pending_Return_Should_Follow_The_Document_To_The_Extension()
    {
        // A return raised against the movement that an extension then replaces would otherwise point at a
        // row that no longer exists and could never be approved.
        var meId = CreateCurrentUserEmployee("PPX410");
        var approverId = EnableWorkflow("PPX410APP");
        var documentId = CreatePassportFor(meId);

        var first = await CreateApprovedWithdrawalAsync(documentId, approverId, -10, 4);

        var pendingReturn = await Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = first.TransactionId,
            ActualReturnDate = DateTime.Today
        });

        var extension = await CreateApprovedWithdrawalAsync(documentId, approverId, -2, 20);

        var followed = RequestRow(pendingReturn.WorkflowRequestId);
        followed.RequestStatus.ShouldBe(RequestStatus.Open, "a pending return is not a superseded withdrawal");
        followed.TargetEntityId.ShouldBe(extension.TransactionId);

        await Approvals.ApproveAsync(pendingReturn.WorkflowRequestId, approverId, "received");

        MovementRow(extension.TransactionId).ActualReturnDate.Date.ShouldBe(DateTime.Today);
    }

    [Fact]
    public async Task An_Ordinary_Withdrawal_Should_Extend_Nothing()
    {
        var meId = CreateCurrentUserEmployee("PPX500");
        var approverId = EnableWorkflow("PPX500APP");

        var movement = await CreateApprovedWithdrawalAsync(CreatePassportFor(meId), approverId);

        MovementRow(movement.TransactionId).ExtendsMovementId.ShouldBeNull();
    }
}
