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
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Save as draft, reopen, submit -- and correct-and-resend after an approver sends it back -- for document
/// requests, to the standard <c>MyExitPermitDraft_Tests</c> set.
///
/// <para>
/// The rule particular to this type is the duplicate check. It is a rule about what gets SUBMITTED, so a
/// draft may hold a number that is already on file and is refused only when it is sent.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyDocumentDraft_Tests : EssRequestDraftTestBase
{
    private const string RequestType = "EmployeeDocument";

    private readonly IMyDocumentAppService _documents;

    public MyDocumentDraft_Tests()
    {
        _documents = Resolve<IMyDocumentAppService>();
    }

    private int CreatePassportType() => CreateDocumentType("Passport", isLetterType: false);

    private static SaveDocumentDraftInput CompleteDraft(int documentTypeId, string number, int? id = null) =>
        new SaveDocumentDraftInput
        {
            Id = id,
            DocumentTypeId = documentTypeId,
            DocumentNum = number,
            IssueDate = DateTime.Today.AddYears(-1),
            ExpiryDate = DateTime.Today.AddYears(4),
            Comments = "Renewed last year"
        };

    private static SaveDocumentDraftInput BareDraft(int documentTypeId) => new SaveDocumentDraftInput
    {
        DocumentTypeId = documentTypeId
    };

    private EmployeeDocument DocumentRow(int id) =>
        UsingDbContext(c => c.Set<EmployeeDocument>().IgnoreQueryFilters().Single(d => d.Id == id));

    /// <summary>A document already on file for the employee, as one HR recorded.</summary>
    private void RecordDocument(int employeeId, int documentTypeId, string number)
    {
        UsingDbContext(context =>
        {
            context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = documentTypeId,
                DocumentNum = number,
                IssueDate = DateTime.Today.AddYears(-3),
                ExpiryDate = DateTime.Today.AddYears(2)
            });
        });
    }

    // ---------------------------------------------------------------- what a draft is not

    [Fact]
    public async Task A_Draft_Should_Not_Raise_A_Workflow_Request_Or_Reach_An_Approver()
    {
        CreateCurrentUserEmployee("DCD100");
        var approverId = EnableWorkflowFor(RequestType, "DCD100APP");

        var saved = await _documents.SaveMyDocumentDraft(BareDraft(CreatePassportType()));

        saved.SavedAsDraft.ShouldBeTrue();
        saved.SubmittedForApproval.ShouldBeFalse();
        saved.WorkflowRequestId.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>()
                .Any(r => r.TargetEntityId == saved.TransactionId && r.RequestType == RequestType))
            .ShouldBeFalse("A draft raised a workflow request for something nobody has sent.");

        UsingDbContext(c => c.Set<WorkFlowApprover>().Any(a => a.ApproverId == approverId && !a.IsApproved))
            .ShouldBeFalse("A draft landed in an approver's inbox.");

        var row = DocumentRow(saved.TransactionId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Draft);
        row.WorkflowRequestId.ShouldBeNull();
        row.ExpiryDate.ShouldBe(default(DateTime));
    }

    [Fact]
    public async Task A_Draft_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        CreateCurrentUserEmployee("DCD110");

        var saved = await _documents.SaveMyDocumentDraft(CompleteDraft(CreatePassportType(), "D-1100"));

        using (Resolve<IUnitOfWorkManager>().Begin())
        {
            Resolve<IRepository<EmployeeDocument>>().GetAll().Any(d => d.Id == saved.TransactionId)
                .ShouldBeFalse("A draft document was visible to the rest of the system.");
        }
    }

    // ---------------------------------------------------------------- what a draft is

    [Fact]
    public async Task A_Draft_With_No_Dates_Should_Show_In_The_Default_List_As_An_Editable_Draft()
    {
        // The default list hides expired documents, and a draft with no expiry date reads as long expired.
        CreateCurrentUserEmployee("DCD200");

        var saved = await _documents.SaveMyDocumentDraft(BareDraft(CreatePassportType()));

        var row = (await _documents.GetMyDocuments(ListInput(includeHistory: false))).Items
            .SingleOrDefault(i => i.Id == saved.TransactionId);

        row.ShouldNotBeNull("The requester cannot see their own draft, so they can never finish it.");
        row.IsDraft.ShouldBeTrue();
        row.IsSentBack.ShouldBeFalse();
        row.CanEdit.ShouldBeTrue();
        row.CanCancel.ShouldBeTrue();
        row.RequestNumber.ShouldBeNull();
        row.DocumentTypeNameEn.ShouldBe("Passport");
    }

    [Fact]
    public async Task A_Draft_Should_Be_Refused_For_A_Document_Type_That_Does_Not_Exist()
    {
        CreateCurrentUserEmployee("DCD210");

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _documents.SaveMyDocumentDraft(BareDraft(987654)));
    }

    [Fact]
    public async Task A_Draft_Should_Be_Saveable_With_A_Number_Already_On_File()
    {
        // The duplicate check is about what is submitted. Saving must not run it.
        var employeeId = CreateCurrentUserEmployee("DCD220");
        var typeId = CreatePassportType();
        RecordDocument(employeeId, typeId, "D-2200");

        var saved = await _documents.SaveMyDocumentDraft(CompleteDraft(typeId, "D-2200"));

        saved.SavedAsDraft.ShouldBeTrue();
        DocumentRow(saved.TransactionId).DocumentNum.ShouldBe("D-2200");
    }

    // ---------------------------------------------------------------- whose draft it is

    [Fact]
    public async Task Somebody_Elses_Draft_Should_Not_Be_Readable_Editable_Or_Submittable()
    {
        CreateCurrentUserEmployee("DCD300");
        var otherId = CreateEmployee("DCD300OTHER");
        var typeId = CreatePassportType();

        var theirDraftId = 0;

        UsingDbContext(context =>
        {
            var document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                DocumentTypeId = typeId,
                DocumentNum = "THEIRS",
                IssueDate = DateTime.Today.AddYears(-1),
                ExpiryDate = DateTime.Today.AddYears(1),
                WorkflowStatus = RequestStatus.Draft
            }).Entity;

            context.SaveChanges();
            theirDraftId = document.Id;
        });

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _documents.GetMyDocumentDraftForEdit(new EntityDto(theirDraftId)));

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _documents.SaveMyDocumentDraft(CompleteDraft(typeId, "STOLEN", theirDraftId)));

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _documents.SubmitMyDocumentDraft(new EntityDto(theirDraftId)));

        DocumentRow(theirDraftId).DocumentNum.ShouldBe("THEIRS");
    }

    [Fact]
    public async Task A_Request_Already_With_An_Approver_Should_Not_Be_Editable_As_A_Draft()
    {
        CreateCurrentUserEmployee("DCD310");
        EnableWorkflowFor(RequestType, "DCD310APP");
        var typeId = CreatePassportType();

        var submitted = await _documents.RequestDocument(new RequestDocumentInput
        {
            DocumentTypeId = typeId,
            DocumentNum = "D-3100",
            IssueDate = DateTime.Today.AddYears(-1),
            ExpiryDate = DateTime.Today.AddYears(4)
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _documents.GetMyDocumentDraftForEdit(new EntityDto(submitted.TransactionId)));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _documents.SaveMyDocumentDraft(CompleteDraft(typeId, "D-3101", submitted.TransactionId)));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _documents.SubmitMyDocumentDraft(new EntityDto(submitted.TransactionId)));

        var row = (await _documents.GetMyDocuments(ListInput())).Items.Single(i => i.Id == submitted.TransactionId);
        row.CanEdit.ShouldBeFalse();
        row.IsDraft.ShouldBeFalse();
        row.IsSentBack.ShouldBeFalse();
        row.RequestNumber.ShouldBe(RequestRow(submitted.WorkflowRequestId.Value).RequestNumber);
    }

    // ---------------------------------------------------------------- saved, changed, submitted

    [Fact]
    public async Task A_Draft_Should_Be_Editable_And_Then_Submittable()
    {
        CreateCurrentUserEmployee("DCD400");
        var approverId = EnableWorkflowFor(RequestType, "DCD400APP");
        var typeId = CreatePassportType();

        var saved = await _documents.SaveMyDocumentDraft(BareDraft(typeId));

        var reopened = await _documents.GetMyDocumentDraftForEdit(new EntityDto(saved.TransactionId));

        reopened.Id.ShouldBe(saved.TransactionId);
        reopened.IsDraft.ShouldBeTrue();
        reopened.CanEdit.ShouldBeTrue();
        reopened.DocumentTypeId.ShouldBe(typeId);
        reopened.DocumentTypeNameEn.ShouldBe("Passport");

        await _documents.SaveMyDocumentDraft(CompleteDraft(typeId, "D-4000", saved.TransactionId));

        var edited = DocumentRow(saved.TransactionId);
        edited.DocumentNum.ShouldBe("D-4000");
        edited.ExpiryDate.ShouldBe(DateTime.Today.AddYears(4));
        edited.WorkflowStatus.ShouldBe(RequestStatus.Draft, "Editing a draft must not submit it.");
        edited.WorkflowRequestId.ShouldBeNull();

        var submitted = await _documents.SubmitMyDocumentDraft(new EntityDto(saved.TransactionId));

        submitted.TransactionId.ShouldBe(saved.TransactionId);
        submitted.SubmittedForApproval.ShouldBeTrue();
        submitted.WorkflowRequestId.ShouldNotBeNull();

        var request = RequestRow(submitted.WorkflowRequestId.Value);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.RequestNumber.ShouldBe("1", "The draft must not have taken a number before it was sent.");
        request.RequestSubTypeId.ShouldBe(typeId);

        DocumentRow(saved.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Open);
        IsWaitingOn(approverId, request.Id).ShouldBeTrue("Submitting a draft did not put it in front of anybody.");

        var listed = (await _documents.GetMyDocuments(ListInput())).Items.Single(i => i.Id == saved.TransactionId);
        listed.RequestNumber.ShouldBe("1");
        listed.CanEdit.ShouldBeFalse();

        UsingDbContext(c => c.Set<EmployeeDocument>().IgnoreQueryFilters().Count(d => d.DocumentNum == "D-4000"))
            .ShouldBe(1);
    }

    [Fact]
    public async Task Submitting_A_Draft_Should_Refuse_A_Duplicate_Document()
    {
        var employeeId = CreateCurrentUserEmployee("DCD410");
        EnableWorkflowFor(RequestType, "DCD410APP");
        var typeId = CreatePassportType();
        RecordDocument(employeeId, typeId, "D-4100");

        var saved = await _documents.SaveMyDocumentDraft(CompleteDraft(typeId, "D-4100"));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _documents.SubmitMyDocumentDraft(new EntityDto(saved.TransactionId)));

        var row = DocumentRow(saved.TransactionId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Draft);
        row.WorkflowRequestId.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>().Any(r => r.TargetEntityId == saved.TransactionId && r.RequestType == RequestType))
            .ShouldBeFalse();
    }

    [Fact]
    public async Task Submitting_A_Draft_Should_Refuse_A_Duplicate_Even_With_Workflow_Off()
    {
        // With workflow off no handler runs, so the service's own check is the only one there is.
        var employeeId = CreateCurrentUserEmployee("DCD420");
        var typeId = CreatePassportType();
        RecordDocument(employeeId, typeId, "D-4200");

        var saved = await _documents.SaveMyDocumentDraft(CompleteDraft(typeId, "D-4200"));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _documents.SubmitMyDocumentDraft(new EntityDto(saved.TransactionId)));

        DocumentRow(saved.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Draft);
    }

    [Fact]
    public async Task Submitting_A_Draft_With_No_Document_Number_Should_Be_Refused()
    {
        CreateCurrentUserEmployee("DCD425");

        var saved = await _documents.SaveMyDocumentDraft(BareDraft(CreatePassportType()));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _documents.SubmitMyDocumentDraft(new EntityDto(saved.TransactionId)));
    }

    [Fact]
    public async Task Submitting_A_Draft_With_Workflow_Off_Should_Make_It_Effective()
    {
        CreateCurrentUserEmployee("DCD430");

        var saved = await _documents.SaveMyDocumentDraft(CompleteDraft(CreatePassportType(), "D-4300"));
        var submitted = await _documents.SubmitMyDocumentDraft(new EntityDto(saved.TransactionId));

        submitted.SubmittedForApproval.ShouldBeFalse();
        submitted.WorkflowRequestId.ShouldBeNull();
        DocumentRow(saved.TransactionId).WorkflowStatus.ShouldBeNull();
    }

    // ---------------------------------------------------------------- sent back, corrected, sent again

    [Fact]
    public async Task A_Sent_Back_Request_Should_Be_Correctable_And_Resubmitted_Under_The_Same_Request()
    {
        CreateCurrentUserEmployee("DCD500");
        var approverId = EnableWorkflowFor(RequestType, "DCD500APP");
        var typeId = CreatePassportType();

        var submitted = await _documents.RequestDocument(new RequestDocumentInput
        {
            DocumentTypeId = typeId,
            DocumentNum = "D-500O",
            IssueDate = DateTime.Today.AddYears(-1),
            ExpiryDate = DateTime.Today.AddYears(4)
        });

        var requestId = submitted.WorkflowRequestId.Value;
        var number = RequestRow(requestId).RequestNumber;

        await Approvals.SendBackAsync(requestId, approverId, "The number has a letter O in it");

        var listed = (await _documents.GetMyDocuments(ListInput(includeHistory: false))).Items.Single(i => i.Id == submitted.TransactionId);
        listed.IsSentBack.ShouldBeTrue();
        listed.IsDraft.ShouldBeFalse("A request sent back is not a draft that was never sent.");
        listed.CanEdit.ShouldBeTrue();
        listed.CanCancel.ShouldBeTrue();
        listed.RequestNumber.ShouldBe(number);

        var reopened = await _documents.GetMyDocumentDraftForEdit(new EntityDto(submitted.TransactionId));
        reopened.IsSentBack.ShouldBeTrue();
        reopened.RequestNumber.ShouldBe(number);

        await _documents.SaveMyDocumentDraft(CompleteDraft(typeId, "D-5000", submitted.TransactionId));

        var corrected = DocumentRow(submitted.TransactionId);
        corrected.DocumentNum.ShouldBe("D-5000");
        corrected.WorkflowStatus.ShouldBe(RequestStatus.Draft, "Saving a correction must not send it.");
        corrected.WorkflowRequestId.ShouldBe(requestId, "The correction lost the request it belongs to.");

        var resent = await _documents.SubmitMyDocumentDraft(new EntityDto(submitted.TransactionId));

        resent.SubmittedForApproval.ShouldBeTrue();
        resent.WorkflowRequestId.ShouldBe(requestId, "Resubmitting raised a second request instead of resending the first.");

        var request = RequestRow(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.RequestNumber.ShouldBe(number);

        DocumentRow(submitted.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Open);
        IsWaitingOn(approverId, requestId).ShouldBeTrue("The resubmitted request is not waiting on its approver.");

        UsingDbContext(c => c.Set<WorkflowRequest>().Count(r => r.TargetEntityId == submitted.TransactionId && r.RequestType == RequestType))
            .ShouldBe(1);
    }

    [Fact]
    public async Task A_Sent_Back_Request_Should_Run_The_Duplicate_Check_Again_When_Resubmitted()
    {
        var employeeId = CreateCurrentUserEmployee("DCD510");
        var approverId = EnableWorkflowFor(RequestType, "DCD510APP");
        var typeId = CreatePassportType();
        RecordDocument(employeeId, typeId, "D-5100");

        var submitted = await _documents.RequestDocument(new RequestDocumentInput
        {
            DocumentTypeId = typeId,
            DocumentNum = "D-5101",
            IssueDate = DateTime.Today.AddYears(-1),
            ExpiryDate = DateTime.Today.AddYears(4)
        });

        await Approvals.SendBackAsync(submitted.WorkflowRequestId.Value, approverId, "Check the number");

        // "Corrected" to the number that is already on file.
        await _documents.SaveMyDocumentDraft(CompleteDraft(typeId, "D-5100", submitted.TransactionId));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _documents.SubmitMyDocumentDraft(new EntityDto(submitted.TransactionId)));

        RequestRow(submitted.WorkflowRequestId.Value).RequestStatus.ShouldBe(RequestStatus.Draft);
    }

    // ---------------------------------------------------------------- discard

    [Fact]
    public async Task A_Draft_Should_Be_Discardable()
    {
        CreateCurrentUserEmployee("DCD600");

        var saved = await _documents.SaveMyDocumentDraft(BareDraft(CreatePassportType()));

        await _documents.CancelMyDocument(new EntityDto(saved.TransactionId));

        (await _documents.GetMyDocuments(ListInput())).Items.Select(i => i.Id)
            .ShouldNotContain(saved.TransactionId);
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _documents.SaveMyDocumentDraft(CompleteDraft(CreatePassportType(), "D-9999")));
    }

    // ---------------------------------------------------------------- what the approver is shown

    [Fact]
    public async Task BuildDetail_Should_Publish_Every_Field_Of_The_Document_Form()
    {
        CreateCurrentUserEmployee("DCD800");
        var approverId = EnableWorkflowFor(RequestType, "DCD800APP");

        var saved = await _documents.SaveMyDocumentDraft(CompleteDraft(CreatePassportType(), "D-8000"));
        var submitted = await _documents.SubmitMyDocumentDraft(new EntityDto(saved.TransactionId));

        var detail = await BuildDetailAsync(submitted.WorkflowRequestId.Value, approverId);

        detail.Fields.Select(f => f.Name).ShouldBe(new[]
        {
            "RequestNumber", "Employee", "DocumentType", "DocumentNumber", "IssueDate", "ExpiryDate", "Comments", "Description"
        });

        Value(detail, "RequestNumber").ShouldBe(RequestRow(submitted.WorkflowRequestId.Value).RequestNumber);
        Value(detail, "Employee").ShouldBe("DCD800 - DCD800 T");
        Value(detail, "DocumentType").ShouldBe("Passport");
        Value(detail, "DocumentNumber").ShouldBe("D-8000");
        Value(detail, "IssueDate").ShouldBe(DateTime.Today.AddYears(-1).ToString("yyyy-MM-dd"));
        Value(detail, "ExpiryDate").ShouldBe(DateTime.Today.AddYears(4).ToString("yyyy-MM-dd"));
        Value(detail, "Comments").ShouldBe("Renewed last year");
        Value(detail, "Description").ShouldBeNull();
        detail.SubmissionComment.ShouldBe("Renewed last year");
    }
}
