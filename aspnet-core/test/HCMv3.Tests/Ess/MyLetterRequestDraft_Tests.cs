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
using HCMv3.PersonnelAdministration;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.Ess;

/// <summary>
/// Save as draft, reopen, submit -- and correct-and-resend after an approver sends it back -- for letter
/// requests, to the standard <c>MyExitPermitDraft_Tests</c> set.
///
/// <para>
/// The rule particular to this type is that the document type must be one the company issues as a letter,
/// and a draft does not get to defer it: it holds at save AND at submit. Everything else -- the date, the
/// two addressees -- is what a draft is allowed to be missing until it is sent.
/// </para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class MyLetterRequestDraft_Tests : EssRequestDraftTestBase
{
    private const string RequestType = "EmployeeLetterRequest";

    private readonly IMyLetterRequestAppService _letters;

    public MyLetterRequestDraft_Tests()
    {
        _letters = Resolve<IMyLetterRequestAppService>();
    }

    private int CreateLetterType() => CreateDocumentType("Salary Certificate", isLetterType: true);

    private static SaveLetterDraftInput CompleteDraft(int documentTypeId, int? id = null) => new SaveLetterDraftInput
    {
        Id = id,
        DocumentTypeId = documentTypeId,
        EffectiveDate = DateTime.Today,
        AddressToEn = "The Manager, National Bank of Kuwait",
        AddressToAr = "المدير، بنك الكويت الوطني",
        Comments = "For a housing loan application"
    };

    private static SaveLetterDraftInput BareDraft(int documentTypeId) => new SaveLetterDraftInput
    {
        DocumentTypeId = documentTypeId
    };

    private EmployeeLetterRequest LetterRow(int id) =>
        UsingDbContext(c => c.Set<EmployeeLetterRequest>().IgnoreQueryFilters().Single(l => l.Id == id));

    // ---------------------------------------------------------------- what a draft is not

    [Fact]
    public async Task A_Draft_Should_Not_Raise_A_Workflow_Request_Or_Reach_An_Approver()
    {
        CreateCurrentUserEmployee("LTD100");
        var approverId = EnableWorkflowFor(RequestType, "LTD100APP");

        var saved = await _letters.SaveMyLetterRequestDraft(BareDraft(CreateLetterType()));

        saved.SavedAsDraft.ShouldBeTrue();
        saved.SubmittedForApproval.ShouldBeFalse();
        saved.WorkflowRequestId.ShouldBeNull();

        UsingDbContext(c => c.Set<WorkflowRequest>()
                .Any(r => r.TargetEntityId == saved.TransactionId && r.RequestType == RequestType))
            .ShouldBeFalse("A draft raised a workflow request for something nobody has sent.");

        UsingDbContext(c => c.Set<WorkFlowApprover>().Any(a => a.ApproverId == approverId && !a.IsApproved))
            .ShouldBeFalse("A draft landed in an approver's inbox.");

        var row = LetterRow(saved.TransactionId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Draft);
        row.WorkflowRequestId.ShouldBeNull();
        row.EffectiveDate.ShouldBe(default(DateTime));
        row.AddressToEn.ShouldBe(string.Empty);
    }

    [Fact]
    public async Task A_Draft_Should_Be_Hidden_From_The_Rest_Of_The_System()
    {
        CreateCurrentUserEmployee("LTD110");

        var saved = await _letters.SaveMyLetterRequestDraft(CompleteDraft(CreateLetterType()));

        using (Resolve<IUnitOfWorkManager>().Begin())
        {
            Resolve<IRepository<EmployeeLetterRequest>>().GetAll().Any(l => l.Id == saved.TransactionId)
                .ShouldBeFalse("A draft letter request was visible to the rest of the system.");
        }
    }

    // ---------------------------------------------------------------- what a draft is

    [Fact]
    public async Task A_Draft_Should_Show_In_The_Owners_List_As_An_Editable_Draft_With_No_Request_Number()
    {
        CreateCurrentUserEmployee("LTD200");

        var saved = await _letters.SaveMyLetterRequestDraft(BareDraft(CreateLetterType()));

        var row = (await _letters.GetMyLetterRequests(ListInput(includeHistory: false))).Items
            .SingleOrDefault(i => i.Id == saved.TransactionId);

        row.ShouldNotBeNull("The requester cannot see their own draft, so they can never finish it.");
        row.IsDraft.ShouldBeTrue();
        row.IsSentBack.ShouldBeFalse();
        row.CanEdit.ShouldBeTrue();
        row.CanCancel.ShouldBeTrue();
        row.CanPrint.ShouldBeFalse();
        row.RequestNumber.ShouldBeNull();
        row.DocumentTypeNameEn.ShouldBe("Salary Certificate");
    }

    [Fact]
    public async Task A_Draft_Should_Be_Refused_For_A_Document_Type_That_Is_Not_A_Letter()
    {
        // The one rule a draft does not get to defer -- and it holds on an update as well as a create.
        CreateCurrentUserEmployee("LTD210");
        var passportTypeId = CreateDocumentType("Passport", isLetterType: false);

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.SaveMyLetterRequestDraft(CompleteDraft(passportTypeId)));

        var saved = await _letters.SaveMyLetterRequestDraft(CompleteDraft(CreateLetterType()));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.SaveMyLetterRequestDraft(CompleteDraft(passportTypeId, saved.TransactionId)));

        UsingDbContext(c => c.Set<EmployeeLetterRequest>().IgnoreQueryFilters()
                .Any(l => l.DocumentTypeId == passportTypeId))
            .ShouldBeFalse();
    }

    // ---------------------------------------------------------------- whose draft it is

    [Fact]
    public async Task Somebody_Elses_Draft_Should_Not_Be_Readable_Editable_Or_Submittable()
    {
        CreateCurrentUserEmployee("LTD300");
        var otherId = CreateEmployee("LTD300OTHER");
        var letterTypeId = CreateLetterType();

        var theirDraftId = 0;

        UsingDbContext(context =>
        {
            var letter = context.Set<EmployeeLetterRequest>().Add(new EmployeeLetterRequest
            {
                TenantId = TenantId,
                EmployeeId = otherId,
                DocumentTypeId = letterTypeId,
                EffectiveDate = DateTime.Today,
                AddressToEn = "Theirs",
                AddressToAr = "Theirs",
                WorkflowStatus = RequestStatus.Draft
            }).Entity;

            context.SaveChanges();
            theirDraftId = letter.Id;
        });

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _letters.GetMyLetterRequestDraftForEdit(new EntityDto(theirDraftId)));

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _letters.SaveMyLetterRequestDraft(CompleteDraft(letterTypeId, theirDraftId)));

        await Assert.ThrowsAsync<AbpAuthorizationException>(() =>
            _letters.SubmitMyLetterRequestDraft(new EntityDto(theirDraftId)));

        LetterRow(theirDraftId).AddressToEn.ShouldBe("Theirs");
    }

    [Fact]
    public async Task A_Request_Already_With_An_Approver_Should_Not_Be_Editable_As_A_Draft()
    {
        CreateCurrentUserEmployee("LTD310");
        EnableWorkflowFor(RequestType, "LTD310APP");
        var letterTypeId = CreateLetterType();

        var submitted = await _letters.RequestLetter(new RequestLetterInput
        {
            DocumentTypeId = letterTypeId,
            EffectiveDate = DateTime.Today,
            AddressToEn = "The Manager",
            AddressToAr = "المدير"
        });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.GetMyLetterRequestDraftForEdit(new EntityDto(submitted.TransactionId)));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.SaveMyLetterRequestDraft(CompleteDraft(letterTypeId, submitted.TransactionId)));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.SubmitMyLetterRequestDraft(new EntityDto(submitted.TransactionId)));

        var row = (await _letters.GetMyLetterRequests(ListInput())).Items.Single(i => i.Id == submitted.TransactionId);
        row.CanEdit.ShouldBeFalse();
        row.IsDraft.ShouldBeFalse();
        row.IsSentBack.ShouldBeFalse();
        row.RequestNumber.ShouldBe(RequestRow(submitted.WorkflowRequestId.Value).RequestNumber);
    }

    // ---------------------------------------------------------------- saved, changed, submitted

    [Fact]
    public async Task A_Draft_Should_Be_Editable_And_Then_Submittable()
    {
        CreateCurrentUserEmployee("LTD400");
        var approverId = EnableWorkflowFor(RequestType, "LTD400APP");
        var letterTypeId = CreateLetterType();

        var saved = await _letters.SaveMyLetterRequestDraft(BareDraft(letterTypeId));

        var reopened = await _letters.GetMyLetterRequestDraftForEdit(new EntityDto(saved.TransactionId));

        reopened.Id.ShouldBe(saved.TransactionId);
        reopened.IsDraft.ShouldBeTrue();
        reopened.CanEdit.ShouldBeTrue();
        reopened.DocumentTypeId.ShouldBe(letterTypeId);
        reopened.DocumentTypeNameEn.ShouldBe("Salary Certificate");

        await _letters.SaveMyLetterRequestDraft(CompleteDraft(letterTypeId, saved.TransactionId));

        var edited = LetterRow(saved.TransactionId);
        edited.AddressToEn.ShouldBe("The Manager, National Bank of Kuwait");
        edited.EffectiveDate.ShouldBe(DateTime.Today);
        edited.WorkflowStatus.ShouldBe(RequestStatus.Draft, "Editing a draft must not submit it.");
        edited.WorkflowRequestId.ShouldBeNull();

        var submitted = await _letters.SubmitMyLetterRequestDraft(new EntityDto(saved.TransactionId));

        submitted.TransactionId.ShouldBe(saved.TransactionId);
        submitted.SubmittedForApproval.ShouldBeTrue();
        submitted.WorkflowRequestId.ShouldNotBeNull();

        var request = RequestRow(submitted.WorkflowRequestId.Value);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.RequestNumber.ShouldBe("1", "The draft must not have taken a number before it was sent.");
        request.RequestSubTypeId.ShouldBe(letterTypeId);

        LetterRow(saved.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Open);
        IsWaitingOn(approverId, request.Id).ShouldBeTrue("Submitting a draft did not put it in front of anybody.");

        var listed = (await _letters.GetMyLetterRequests(ListInput())).Items.Single(i => i.Id == saved.TransactionId);
        listed.RequestNumber.ShouldBe("1");
        listed.CanEdit.ShouldBeFalse();
    }

    [Fact]
    public async Task Submitting_A_Draft_Should_Refuse_A_Document_Type_That_Is_No_Longer_A_Letter()
    {
        // Passed at save; the type was unflagged afterwards. Submit must ask again rather than trust the row.
        CreateCurrentUserEmployee("LTD410");
        EnableWorkflowFor(RequestType, "LTD410APP");
        var letterTypeId = CreateLetterType();

        var saved = await _letters.SaveMyLetterRequestDraft(CompleteDraft(letterTypeId));

        UsingDbContext(c => { c.Set<DocumentType>().Single(t => t.Id == letterTypeId).IsLetterType = false; });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.SubmitMyLetterRequestDraft(new EntityDto(saved.TransactionId)));

        var row = LetterRow(saved.TransactionId);
        row.WorkflowStatus.ShouldBe(RequestStatus.Draft);
        row.WorkflowRequestId.ShouldBeNull();
    }

    [Fact]
    public async Task Submitting_An_Incomplete_Draft_Should_Be_Refused_Even_With_Workflow_Off()
    {
        // With workflow off no handler runs, and a draft is the one way a row gets here without an addressee.
        CreateCurrentUserEmployee("LTD420");

        var saved = await _letters.SaveMyLetterRequestDraft(BareDraft(CreateLetterType()));

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.SubmitMyLetterRequestDraft(new EntityDto(saved.TransactionId)));

        LetterRow(saved.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Draft);
    }

    [Fact]
    public async Task Submitting_A_Draft_With_Workflow_Off_Should_Make_It_Effective()
    {
        CreateCurrentUserEmployee("LTD430");

        var saved = await _letters.SaveMyLetterRequestDraft(CompleteDraft(CreateLetterType()));
        var submitted = await _letters.SubmitMyLetterRequestDraft(new EntityDto(saved.TransactionId));

        submitted.SubmittedForApproval.ShouldBeFalse();
        submitted.WorkflowRequestId.ShouldBeNull();
        LetterRow(saved.TransactionId).WorkflowStatus.ShouldBeNull();
    }

    // ---------------------------------------------------------------- sent back, corrected, sent again

    [Fact]
    public async Task A_Sent_Back_Request_Should_Be_Correctable_And_Resubmitted_Under_The_Same_Request()
    {
        CreateCurrentUserEmployee("LTD500");
        var approverId = EnableWorkflowFor(RequestType, "LTD500APP");
        var letterTypeId = CreateLetterType();
        var otherLetterTypeId = CreateDocumentType("Experience Certificate", isLetterType: true);

        var submitted = await _letters.RequestLetter(new RequestLetterInput
        {
            DocumentTypeId = letterTypeId,
            EffectiveDate = DateTime.Today,
            AddressToEn = "To whom it may concern",
            AddressToAr = "إلى من يهمه الأمر"
        });

        var requestId = submitted.WorkflowRequestId.Value;
        var number = RequestRow(requestId).RequestNumber;

        await Approvals.SendBackAsync(requestId, approverId, "Name the bank");

        var listed = (await _letters.GetMyLetterRequests(ListInput(includeHistory: false))).Items.Single(i => i.Id == submitted.TransactionId);
        listed.IsSentBack.ShouldBeTrue();
        listed.IsDraft.ShouldBeFalse("A request sent back is not a draft that was never sent.");
        listed.CanEdit.ShouldBeTrue();
        listed.CanCancel.ShouldBeTrue();
        listed.RequestNumber.ShouldBe(number);

        var reopened = await _letters.GetMyLetterRequestDraftForEdit(new EntityDto(submitted.TransactionId));
        reopened.IsSentBack.ShouldBeTrue();
        reopened.RequestNumber.ShouldBe(number);

        var correction = CompleteDraft(otherLetterTypeId, submitted.TransactionId);
        await _letters.SaveMyLetterRequestDraft(correction);

        var corrected = LetterRow(submitted.TransactionId);
        corrected.AddressToEn.ShouldBe("The Manager, National Bank of Kuwait");
        corrected.WorkflowStatus.ShouldBe(RequestStatus.Draft, "Saving a correction must not send it.");
        corrected.WorkflowRequestId.ShouldBe(requestId, "The correction lost the request it belongs to.");

        var resent = await _letters.SubmitMyLetterRequestDraft(new EntityDto(submitted.TransactionId));

        resent.SubmittedForApproval.ShouldBeTrue();
        resent.WorkflowRequestId.ShouldBe(requestId, "Resubmitting raised a second request instead of resending the first.");

        var request = RequestRow(requestId);
        request.RequestStatus.ShouldBe(RequestStatus.Open);
        request.RequestNumber.ShouldBe(number);
        request.RequestSubTypeId.ShouldBe(otherLetterTypeId, "The request still routes by the type it had before the correction.");

        LetterRow(submitted.TransactionId).WorkflowStatus.ShouldBe(RequestStatus.Open);
        IsWaitingOn(approverId, requestId).ShouldBeTrue("The resubmitted request is not waiting on its approver.");

        UsingDbContext(c => c.Set<WorkflowRequest>().Count(r => r.TargetEntityId == submitted.TransactionId && r.RequestType == RequestType))
            .ShouldBe(1);
    }

    [Fact]
    public async Task A_Sent_Back_Request_Should_Run_The_Rules_Again_When_Resubmitted()
    {
        CreateCurrentUserEmployee("LTD510");
        var approverId = EnableWorkflowFor(RequestType, "LTD510APP");
        var letterTypeId = CreateLetterType();

        var submitted = await _letters.RequestLetter(new RequestLetterInput
        {
            DocumentTypeId = letterTypeId,
            EffectiveDate = DateTime.Today,
            AddressToEn = "The Manager",
            AddressToAr = "المدير"
        });

        await Approvals.SendBackAsync(submitted.WorkflowRequestId.Value, approverId, "Fix it");

        UsingDbContext(c => { c.Set<DocumentType>().Single(t => t.Id == letterTypeId).IsLetterType = false; });

        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.SubmitMyLetterRequestDraft(new EntityDto(submitted.TransactionId)));

        RequestRow(submitted.WorkflowRequestId.Value).RequestStatus.ShouldBe(RequestStatus.Draft);
    }

    // ---------------------------------------------------------------- discard

    [Fact]
    public async Task A_Draft_Should_Be_Discardable()
    {
        CreateCurrentUserEmployee("LTD600");

        var saved = await _letters.SaveMyLetterRequestDraft(BareDraft(CreateLetterType()));

        await _letters.CancelMyLetterRequest(new EntityDto(saved.TransactionId));

        (await _letters.GetMyLetterRequests(ListInput())).Items.Select(i => i.Id)
            .ShouldNotContain(saved.TransactionId);
    }

    [Fact]
    public async Task Should_Refuse_A_User_With_No_Employee_Record()
    {
        await Assert.ThrowsAsync<UserFriendlyException>(() =>
            _letters.SaveMyLetterRequestDraft(CompleteDraft(CreateLetterType())));
    }

    // ---------------------------------------------------------------- what the approver is shown

    [Fact]
    public async Task BuildDetail_Should_Publish_Every_Field_Of_The_Letter_Form()
    {
        CreateCurrentUserEmployee("LTD800");
        var approverId = EnableWorkflowFor(RequestType, "LTD800APP");

        var saved = await _letters.SaveMyLetterRequestDraft(CompleteDraft(CreateLetterType()));
        var submitted = await _letters.SubmitMyLetterRequestDraft(new EntityDto(saved.TransactionId));

        var detail = await BuildDetailAsync(submitted.WorkflowRequestId.Value, approverId);

        detail.Fields.Select(f => f.Name).ShouldBe(new[]
        {
            "RequestNumber", "Employee", "DocumentType", "EffectiveDate", "AddressToEn", "AddressToAr", "Comments"
        });

        Value(detail, "RequestNumber").ShouldBe(RequestRow(submitted.WorkflowRequestId.Value).RequestNumber);
        Value(detail, "Employee").ShouldBe("LTD800 - LTD800 T");
        Value(detail, "DocumentType").ShouldBe("Salary Certificate");
        Value(detail, "EffectiveDate").ShouldBe(DateTime.Today.ToString("yyyy-MM-dd"));
        Value(detail, "AddressToEn").ShouldBe("The Manager, National Bank of Kuwait");
        Value(detail, "AddressToAr").ShouldBe("المدير، بنك الكويت الوطني");
        Value(detail, "Comments").ShouldBe("For a housing loan application");
        detail.SubmissionComment.ShouldBe("For a housing loan application");
    }
}
