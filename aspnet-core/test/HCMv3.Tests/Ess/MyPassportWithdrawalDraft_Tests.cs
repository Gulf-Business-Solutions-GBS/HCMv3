using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using Castle.MicroKernel.Registration;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.SystemSecurity.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Save as draft, reopen, submit -- and correct-and-resend after an approver sends it back -- for passport
/// withdrawals, to the standard <c>MyExitPermitDraft_Tests</c> set.
///
/// <para>
/// Two things here are particular to this type. A draft must still point at the employee's OWN document,
/// because this is the endpoint that hands over original documents and a draft is a row saying whose. And
/// the guarantor is told when the request is submitted, never when a draft is saved: being asked to vouch
/// for something its owner has not decided to send is noise.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyPassportWithdrawalDraft_Tests : PassportWithdrawalTestBase
{
    private const string RequestType = "EmployeeDocumentMvmt";

    private static SavePassportWithdrawalDraftInput DraftInput(int documentId, int? outInDays = 5, int? backInDays = 12) =>
        new SavePassportWithdrawalDraftInput
        {
            EmployeeDocumentId = documentId,
            WithdrawDate = outInDays.HasValue ? DateTime.Today.AddDays(outInDays.Value) : (DateTime?)null,
            ReturnDate = backInDays.HasValue ? DateTime.Today.AddDays(backInDays.Value) : (DateTime?)null,
            WithdrawalReason = "Visa application",
            Comments = "Not sure of the dates yet"
        };

    private async Task<WorkflowRequestDetail> BuildDetailAsync(int requestId, int actingEmployeeId)
    {
        using (var unitOfWork = Resolve<IUnitOfWorkManager>().Begin())
        {
            var request = await Resolve<IRepository<WorkflowRequest>>().GetAsync(requestId);
            var handler = Resolve<IWorkflowTransactionHandlerResolver>().Resolve(request.RequestType);

            var detail = await handler.BuildDetailAsync(new WorkflowTransactionContext(request, actingEmployeeId));

            await unitOfWork.CompleteAsync();

            return detail;
        }
    }

    private static string Value(WorkflowRequestDetail detail, string name) =>
        detail.Fields.Single(f => f.Name == name).Value;

    // ---------------------------------------------------------------- what a draft is not

    [Fact]
    public async Task A_Draft_Should_Not_Raise_A_Workflow_Request_Or_Reach_An_Approver()
    {
        var employeeId = CreateCurrentUserEmployee("PPD100");
        var approverId = EnableWorkflow("PPD100APP");
        var documentId = CreatePassportFor(employeeId);

        var saved = await Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(documentId));

        saved.SavedAsDraft.ShouldBeTrue();
        saved.SubmittedForApproval.ShouldBeFalse();
        saved.WorkflowRequestId.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>()
                .Any(r => r.TargetEntityId == saved.TransactionId && r.RequestType == RequestType))
            .ShouldBeFalse("A draft raised a workflow request for something nobody has sent.");

        UsingDbContext(c => c.Set<WorkFlowApprover>().Any(a => a.ApproverId == approverId && !a.IsApproved))
            .ShouldBeFalse("A draft landed in an approver's inbox.");

        var row = MovementRow(saved.TransactionId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Draft);
        row.WorkflowRequestId.ShouldBeNull();
    }

    [Fact]
    public async Task A_Draft_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        // Through a repository inside a unit of work, which is where the ApprovedOnly filter lives. A draft
        // the movement log could see would be a passport recorded as out that nobody approved releasing.
        var employeeId = CreateCurrentUserEmployee("PPD110");
        var documentId = CreatePassportFor(employeeId);

        var saved = await Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(documentId));

        using (Resolve<IUnitOfWorkManager>().Begin())
        {
            Resolve<IRepository<EmployeeDocumentMvmt>>().GetAll().Any(m => m.Id == saved.TransactionId)
                .ShouldBeFalse("A draft withdrawal was visible to the rest of the system.");
        }
    }

    // ---------------------------------------------------------------- what a draft is

    [Fact]
    public async Task A_Draft_Should_Show_In_The_Owners_List_As_An_Editable_Draft_With_No_Request_Number()
    {
        var employeeId = CreateCurrentUserEmployee("PPD200");
        var documentId = CreatePassportFor(employeeId);

        // No dates at all: the default list must not hide the row for want of one.
        var saved = await Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(documentId, null, null));

        var mine = await Withdrawals.GetMyPassportWithdrawals(
            new GetMyRequestsInput { IncludeHistory = false, MaxResultCount = 50, SkipCount = 0 });

        var row = mine.Items.SingleOrDefault(i => i.Id == saved.TransactionId);

        row.ShouldNotBeNull("The requester cannot see their own draft, so they can never finish it.");
        row.IsDraft.ShouldBeTrue();
        row.IsSentBack.ShouldBeFalse();
        row.CanEdit.ShouldBeTrue();
        row.CanCancel.ShouldBeTrue();
        row.CanReturn.ShouldBeFalse();
        row.RequestNumber.ShouldBeNull();
        row.WorkflowStatus.ShouldBe(RequestStatus.Draft);
    }

    [Fact]
    public async Task A_Draft_Should_Be_Refused_Against_Somebody_Elses_Document()
    {
        // The one rule a draft is held to beyond being storable.
        CreateCurrentUserEmployee("PPD210");
        var otherId = CreateEmployee("PPD210OTHER");
        var theirDocumentId = CreatePassportFor(otherId);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(theirDocumentId)));
    }

    // ---------------------------------------------------------------- whose draft it is

    [Fact]
    public async Task Somebody_Elses_Draft_Should_Not_Be_Readable_Editable_Or_Submittable()
    {
        var meId = CreateCurrentUserEmployee("PPD300");
        var myDocumentId = CreatePassportFor(meId);
        var otherId = CreateEmployee("PPD300OTHER");
        var theirDocumentId = CreatePassportFor(otherId);

        var theirDraftId = InsertMovement(otherId, theirDocumentId, RequestStatus.Draft);

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            Withdrawals.GetMyPassportWithdrawalDraftForEdit(new EntityDto(theirDraftId)));

        var stolen = DraftInput(myDocumentId);
        stolen.Id = theirDraftId;

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            Withdrawals.SaveMyPassportWithdrawalDraft(stolen));

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            Withdrawals.SubmitMyPassportWithdrawalDraft(new EntityDto(theirDraftId)));

        MovementRow(theirDraftId).EmployeeDocumentId.ShouldBe(theirDocumentId);
    }

    [Fact]
    public async Task A_Request_Already_With_An_Approver_Should_Not_Be_Editable_As_A_Draft()
    {
        var employeeId = CreateCurrentUserEmployee("PPD310");
        EnableWorkflow("PPD310APP");
        var documentId = CreatePassportFor(employeeId);

        var submitted = await Withdrawals.RequestPassportWithdrawal(WithdrawalInput(documentId));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            Withdrawals.GetMyPassportWithdrawalDraftForEdit(new EntityDto(submitted.TransactionId)));

        var edit = DraftInput(documentId);
        edit.Id = submitted.TransactionId;

        await Assert.ThrowsAsync<UserFriendlyException>(() => Withdrawals.SaveMyPassportWithdrawalDraft(edit));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            Withdrawals.SubmitMyPassportWithdrawalDraft(new EntityDto(submitted.TransactionId)));

        var row = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == submitted.TransactionId);
        row.CanEdit.ShouldBeFalse();
        row.IsDraft.ShouldBeFalse();
        row.IsSentBack.ShouldBeFalse();
    }

    // ---------------------------------------------------------------- saved, changed, submitted

    [Fact]
    public async Task A_Draft_Should_Be_Editable_And_Then_Submittable()
    {
        var employeeId = CreateCurrentUserEmployee("PPD400");
        var approverId = EnableWorkflow("PPD400APP");
        var documentId = CreatePassportFor(employeeId);
        var guarantorId = CreateEmployee("PPD400GUAR");

        var saved = await Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(documentId, null, null));

        var reopened = await Withdrawals.GetMyPassportWithdrawalDraftForEdit(new EntityDto(saved.TransactionId));

        reopened.Id.ShouldBe(saved.TransactionId);
        reopened.IsDraft.ShouldBeTrue();
        reopened.CanEdit.ShouldBeTrue();
        reopened.EmployeeDocumentId.ShouldBe(documentId);
        reopened.DocumentTypeNameEn.ShouldBe("Passport");
        reopened.RequestNumber.ShouldBeNull();

        var withdrawOn = DateTime.Today.AddDays(3);
        var backOn = DateTime.Today.AddDays(9);

        await Withdrawals.SaveMyPassportWithdrawalDraft(new SavePassportWithdrawalDraftInput
        {
            Id = saved.TransactionId,
            EmployeeDocumentId = documentId,
            WithdrawDate = withdrawOn,
            ReturnDate = backOn,
            GuarantorId = guarantorId,
            WithdrawalReason = "Embassy appointment",
            Comments = "Dates confirmed"
        });

        var edited = MovementRow(saved.TransactionId);
        edited.WithdrawDate.ShouldBe(withdrawOn);
        edited.ReturnDate.ShouldBe(backOn);
        edited.GuarantorId.ShouldBe(guarantorId);
        edited.WithdrawalReason.ShouldBe("Embassy appointment");
        edited.WorkflowStatus.ShouldBe(RequestStatus.Draft, "Editing a draft must not submit it.");
        edited.WorkflowRequestId.ShouldBeNull();

        var submitted = await Withdrawals.SubmitMyPassportWithdrawalDraft(new EntityDto(saved.TransactionId));

        submitted.TransactionId.ShouldBe(saved.TransactionId);
        submitted.SubmittedForApproval.ShouldBeTrue();
        submitted.WorkflowRequestId.ShouldNotBeNull();

        var request = RequestRow(submitted.WorkflowRequestId.Value);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.RequestNumber.ShouldBe("1", "The draft must not have taken a number before it was sent.");

        MovementRow(saved.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Open);

        UsingDbContext(c => c.Set<WorkFlowApprover>().Any(a => a.ApproverId == approverId && !a.IsApproved))
            .ShouldBeTrue("Submitting a draft did not put it in front of anybody.");

        var listed = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == saved.TransactionId);
        listed.RequestNumber.ShouldBe("1");
        listed.IsDraft.ShouldBeFalse();
        listed.CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task Submitting_A_Draft_Should_Refuse_A_Return_Date_Before_The_Withdraw_Date()
    {
        // Saved without complaint, because a draft is allowed to be wrong; refused the moment it is sent.
        var employeeId = CreateCurrentUserEmployee("PPD410");
        EnableWorkflow("PPD410APP");
        var documentId = CreatePassportFor(employeeId);

        var saved = await Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(documentId, outInDays: 10, backInDays: 4));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            Withdrawals.SubmitMyPassportWithdrawalDraft(new EntityDto(saved.TransactionId)));

        var row = MovementRow(saved.TransactionId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Draft);
        row.WorkflowRequestId.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>().Any(r => r.TargetEntityId == saved.TransactionId && r.RequestType == RequestType))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Submitting_A_Draft_With_No_Dates_Should_Be_Refused_Even_With_Workflow_Off()
    {
        // With workflow off no handler runs, so the service's own checks are the only gate there is.
        var employeeId = CreateCurrentUserEmployee("PPD420");
        var documentId = CreatePassportFor(employeeId);

        var saved = await Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(documentId, null, null));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            Withdrawals.SubmitMyPassportWithdrawalDraft(new EntityDto(saved.TransactionId)));

        MovementRow(saved.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Draft);
    }

    [Fact]
    public async Task Submitting_A_Draft_With_Workflow_Off_Should_Make_It_Effective()
    {
        var employeeId = CreateCurrentUserEmployee("PPD430");
        var documentId = CreatePassportFor(employeeId);

        var saved = await Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(documentId));
        var submitted = await Withdrawals.SubmitMyPassportWithdrawalDraft(new EntityDto(saved.TransactionId));

        submitted.SubmittedForApproval.ShouldBeFalse();
        submitted.WorkflowRequestId.ShouldBeNull();
        MovementRow(saved.TransactionId).WorkflowStatus.ShouldBeNull();
    }

    // ---------------------------------------------------------------- sent back, corrected, sent again

    [Fact]
    public async Task A_Sent_Back_Request_Should_Be_Correctable_And_Resubmitted_Under_The_Same_Request()
    {
        var employeeId = CreateCurrentUserEmployee("PPD500");
        var approverId = EnableWorkflow("PPD500APP");
        var documentId = CreatePassportFor(employeeId);

        var submitted = await Withdrawals.RequestPassportWithdrawal(WithdrawalInput(documentId, outInDays: 2, backInDays: 30));
        var requestId = submitted.WorkflowRequestId.Value;
        var number = RequestRow(requestId).RequestNumber;

        await Approvals.SendBackAsync(requestId, approverId, "Thirty days is too long");

        var listed = (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Single(i => i.Id == submitted.TransactionId);
        listed.IsSentBack.ShouldBeTrue();
        listed.IsDraft.ShouldBeFalse("A request sent back is not a draft that was never sent.");
        listed.CanEdit.ShouldBeTrue();
        listed.CanCancel.ShouldBeTrue();
        listed.RequestNumber.ShouldBe(number);

        var reopened = await Withdrawals.GetMyPassportWithdrawalDraftForEdit(new EntityDto(submitted.TransactionId));
        reopened.IsSentBack.ShouldBeTrue();
        reopened.CanEdit.ShouldBeTrue();
        reopened.RequestNumber.ShouldBe(number);

        var correctedBackOn = DateTime.Today.AddDays(9);

        await Withdrawals.SaveMyPassportWithdrawalDraft(new SavePassportWithdrawalDraftInput
        {
            Id = submitted.TransactionId,
            EmployeeDocumentId = documentId,
            WithdrawDate = DateTime.Today.AddDays(2),
            ReturnDate = correctedBackOn,
            WithdrawalReason = "Visa application"
        });

        var corrected = MovementRow(submitted.TransactionId);
        corrected.ReturnDate.ShouldBe(correctedBackOn);
        corrected.WorkflowStatus.ShouldBe(RequestStatus.Draft, "Saving a correction must not send it.");
        corrected.WorkflowRequestId.ShouldBe(requestId, "The correction lost the request it belongs to.");

        var resent = await Withdrawals.SubmitMyPassportWithdrawalDraft(new EntityDto(submitted.TransactionId));

        resent.SubmittedForApproval.ShouldBeTrue();
        resent.WorkflowRequestId.ShouldBe(requestId, "Resubmitting raised a second request instead of resending the first.");

        var request = RequestRow(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.RequestNumber.ShouldBe(number);

        MovementRow(submitted.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Open);

        UsingDbContext(c => c.Set<WorkflowRequest>().Count(r => r.TargetEntityId == submitted.TransactionId && r.RequestType == RequestType))
            .ShouldBe(1);

        UsingDbContext(c => c.Set<WorkFlowApprover>()
                .Any(a => a.WorkflowRequestId == requestId && a.ApproverId == approverId && !a.IsApproved))
            .ShouldBeTrue("The resubmitted request is not waiting on its approver.");

        // And it can go all the way: the corrected dates are what gets approved.
        await Approvals.ApproveAsync(requestId, approverId, "ok");

        var approved = MovementRow(submitted.TransactionId);
        approved.WorkflowStatus.ShouldBe(RequestStatus.Approved);
        approved.ReturnDate.ShouldBe(correctedBackOn);
    }

    [Fact]
    public async Task A_Sent_Back_Request_Should_Run_The_Rules_Again_When_Resubmitted()
    {
        var employeeId = CreateCurrentUserEmployee("PPD510");
        var approverId = EnableWorkflow("PPD510APP");
        var documentId = CreatePassportFor(employeeId);

        var submitted = await Withdrawals.RequestPassportWithdrawal(WithdrawalInput(documentId, outInDays: 2, backInDays: 30));
        await Approvals.SendBackAsync(submitted.WorkflowRequestId.Value, approverId, "Fix the dates");

        await Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(documentId, outInDays: 10, backInDays: 4).WithId(submitted.TransactionId));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            Withdrawals.SubmitMyPassportWithdrawalDraft(new EntityDto(submitted.TransactionId)));

        RequestRow(submitted.WorkflowRequestId.Value).RequestStatus.ShouldBe(RequestStatus.Draft);
    }

    // ---------------------------------------------------------------- discard

    [Fact]
    public async Task A_Draft_Should_Be_Discardable()
    {
        var employeeId = CreateCurrentUserEmployee("PPD600");
        var documentId = CreatePassportFor(employeeId);

        var saved = await Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(documentId));

        await Withdrawals.CancelMyPassportWithdrawal(new EntityDto(saved.TransactionId));

        (await Withdrawals.GetMyPassportWithdrawals(ListInput())).Items.Select(i => i.Id)
            .ShouldNotContain(saved.TransactionId);
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            Withdrawals.SaveMyPassportWithdrawalDraft(DraftInput(1)));
    }

    // ---------------------------------------------------------------- the guarantor is told at submit

    [Fact]
    public async Task The_Guarantor_Should_Be_Notified_When_A_Draft_Is_Submitted_And_Not_When_It_Is_Saved()
    {
        var notifications = Substitute.For<INotificationDispatchService>();
        LocalIocManager.IocContainer.Register(
            Component.For<INotificationDispatchService>().Instance(notifications).IsDefault());

        // Resolved after the substitute is registered, so this instance is the one holding it.
        var withdrawals = Resolve<IMyPassportWithdrawalAppService>();

        var employeeId = CreateCurrentUserEmployee("PPD700");
        EnableWorkflow("PPD700APP");
        var documentId = CreatePassportFor(employeeId);
        var guarantorId = CreateEmployee("PPD700GUAR");

        var input = DraftInput(documentId);
        input.GuarantorId = guarantorId;

        var saved = await withdrawals.SaveMyPassportWithdrawalDraft(input);
        await withdrawals.SaveMyPassportWithdrawalDraft(input.WithId(saved.TransactionId));

        await notifications.DidNotReceiveWithAnyArgs()
            .SendPassportGuarantorNotificationAsync(default, default, default);

        var submitted = await withdrawals.SubmitMyPassportWithdrawalDraft(new EntityDto(saved.TransactionId));
        var number = RequestRow(submitted.WorkflowRequestId.Value).RequestNumber;

        await notifications.Received(1)
            .SendPassportGuarantorNotificationAsync(guarantorId, employeeId, number);
    }

    [Fact]
    public async Task The_Guarantor_Should_Still_Be_Notified_By_A_One_Step_Request()
    {
        var notifications = Substitute.For<INotificationDispatchService>();
        LocalIocManager.IocContainer.Register(
            Component.For<INotificationDispatchService>().Instance(notifications).IsDefault());

        var withdrawals = Resolve<IMyPassportWithdrawalAppService>();

        var employeeId = CreateCurrentUserEmployee("PPD710");
        EnableWorkflow("PPD710APP");
        var documentId = CreatePassportFor(employeeId);
        var guarantorId = CreateEmployee("PPD710GUAR");

        var input = WithdrawalInput(documentId);
        input.GuarantorId = guarantorId;

        var submitted = await withdrawals.RequestPassportWithdrawal(input);
        var number = RequestRow(submitted.WorkflowRequestId.Value).RequestNumber;

        await notifications.Received(1)
            .SendPassportGuarantorNotificationAsync(guarantorId, employeeId, number);
    }

    // ---------------------------------------------------------------- what the approver is shown

    [Fact]
    public async Task BuildDetail_Should_Publish_Every_Field_Of_The_Withdrawal_Form()
    {
        var employeeId = CreateCurrentUserEmployee("PPD800");
        var approverId = EnableWorkflow("PPD800APP");
        var documentTypeId = CreateDocumentType(Guid.NewGuid().ToString("N").Substring(0, 8));
        var documentId = CreateDocument(employeeId, documentTypeId, "P800800");
        var guarantorId = CreateEmployee("PPD800GUAR");

        var input = WithdrawalInput(documentId, outInDays: 2, backInDays: 9);
        input.GuarantorId = guarantorId;
        input.Comments = "Back before the holiday";

        var submitted = await Withdrawals.RequestPassportWithdrawal(input);

        var detail = await BuildDetailAsync(submitted.WorkflowRequestId.Value, approverId);

        detail.Fields.Select(f => f.Name).ShouldBe(new[]
        {
            "RequestNumber", "RequestAction", "Employee", "DocumentType", "DocumentNumber", "IssueDate",
            "ExpiryDate", "WithdrawDate", "ReturnDate", "Guarantor", "WithdrawalReason", "Comments"
        });

        Value(detail, "RequestNumber").ShouldBe(RequestRow(submitted.WorkflowRequestId.Value).RequestNumber);
        Value(detail, "RequestAction").ShouldBe("New");
        Value(detail, "Employee").ShouldBe("PPD800 - PPD800 T");
        Value(detail, "DocumentType").ShouldBe("Passport");
        Value(detail, "DocumentNumber").ShouldBe("P800800");
        Value(detail, "IssueDate").ShouldBe(DateTime.Today.AddYears(-2).ToString("yyyy-MM-dd"));
        Value(detail, "ExpiryDate").ShouldBe(DateTime.Today.AddYears(5).ToString("yyyy-MM-dd"));
        Value(detail, "WithdrawDate").ShouldBe(DateTime.Today.AddDays(2).ToString("yyyy-MM-dd"));
        Value(detail, "ReturnDate").ShouldBe(DateTime.Today.AddDays(9).ToString("yyyy-MM-dd"));
        Value(detail, "Guarantor").ShouldBe("PPD800GUAR - PPD800GUAR T");
        Value(detail, "WithdrawalReason").ShouldBe("Visa application");
        Value(detail, "Comments").ShouldBe("Back before the holiday");
        detail.SubmissionComment.ShouldBe("Back before the holiday");
    }

    [Fact]
    public async Task BuildDetail_Should_Show_The_Custody_Facts_Once_There_Are_Some()
    {
        var employeeId = CreateCurrentUserEmployee("PPD810");
        var approverId = EnableWorkflow("PPD810APP");
        var documentId = CreatePassportFor(employeeId);

        var approved = await CreateApprovedWithdrawalAsync(documentId, approverId);
        var handedOverOn = DateTime.Today.AddDays(-9);
        var returnedOn = DateTime.Today.AddDays(-1);

        UsingDbContext(context =>
        {
            var movement = context.Set<EmployeeDocumentMvmt>().IgnoreQueryFilters().Single(m => m.Id == approved.TransactionId);
            movement.ActualWithdrawDate = handedOverOn;
            movement.ActualReturnDate = returnedOn;
            movement.ReturnRemarks = "Cover slightly worn";
        });

        var detail = await BuildDetailAsync(approved.WorkflowRequestId.Value, approverId);

        Value(detail, "RequestAction").ShouldBe("New");
        Value(detail, "HandedOverOn").ShouldBe(handedOverOn.ToString("yyyy-MM-dd"));
        Value(detail, "ActualReturnDate").ShouldBe(returnedOn.ToString("yyyy-MM-dd"));
        Value(detail, "ReturnRemarks").ShouldBe("Cover slightly worn");
    }

    [Theory]
    [InlineData(WorkflowRequestAction.Return)]
    [InlineData(WorkflowRequestAction.Amendment)]
    public async Task BuildDetail_Should_Show_What_A_Return_Proposes_Beside_The_Movement_It_Is_Against(WorkflowRequestAction label)
    {
        // Under the Return label, and under the generic amendment label older returns were stored with: what
        // makes it a return is that it is an amendment of this type.
        var employeeId = CreateCurrentUserEmployee("PPD820");
        var approverId = EnableWorkflow("PPD820APP");
        var documentId = CreateDocument(employeeId, CreateDocumentType(Guid.NewGuid().ToString("N").Substring(0, 8)), "P820820");

        var approved = await CreateApprovedWithdrawalAsync(documentId, approverId, outInDays: -10, backInDays: 4);

        var returned = await Withdrawals.ReturnMyDocument(new ReturnDocumentInput
        {
            EmployeeDocumentMvmtId = approved.TransactionId,
            ActualReturnDate = DateTime.Today,
            ReturnRemarks = "Handed to reception"
        });

        UsingDbContext(context =>
        {
            context.Set<WorkflowRequest>().Single(r => r.Id == returned.WorkflowRequestId).RequestAction = label;
        });

        var detail = await BuildDetailAsync(returned.WorkflowRequestId, approverId);

        Value(detail, "RequestAction").ShouldBe("Return");

        // What the return proposes...
        Value(detail, "ActualReturnDate").ShouldBe(DateTime.Today.ToString("yyyy-MM-dd"));
        Value(detail, "ReturnRemarks").ShouldBe("Handed to reception");
        detail.SubmissionComment.ShouldBe("Handed to reception");

        // ...beside the movement it is against.
        Value(detail, "Employee").ShouldBe("PPD820 - PPD820 T");
        Value(detail, "DocumentNumber").ShouldBe("P820820");
        Value(detail, "WithdrawDate").ShouldBe(DateTime.Today.AddDays(-10).ToString("yyyy-MM-dd"));
        Value(detail, "ReturnDate").ShouldBe(DateTime.Today.AddDays(4).ToString("yyyy-MM-dd"));
        Value(detail, "RequestNumber").ShouldBe(RequestRow(returned.WorkflowRequestId).RequestNumber);

        // Proposed only: nothing is stamped on the movement until somebody approves the return.
        MovementRow(approved.TransactionId).ActualReturnDate.ShouldBe(default(DateTime));
    }
}

internal static class SavePassportWithdrawalDraftInputTestExtensions
{
    public static SavePassportWithdrawalDraftInput WithId(this SavePassportWithdrawalDraftInput input, int id)
    {
        input.Id = id;
        return input;
    }
}
