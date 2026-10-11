using System;
using System.Linq;
using System.Threading.Tasks;
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
/// The switches on a document type decide what an employee may do with it in self-service -- record a
/// document of it, withdraw one, ask for it as a letter, or see it at all -- and nothing else does: not the
/// type's name, and its code only for a tenant that has ticked nothing.
/// </summary>
// ReSharper disable once InconsistentNaming
public class DocumentTypeSelfServiceSwitches_Tests : PassportWithdrawalTestBase
{
    private readonly IEssLookupAppService _lookups;
    private readonly IMyDocumentAppService _documents;

    public DocumentTypeSelfServiceSwitches_Tests()
    {
        _lookups = Resolve<IEssLookupAppService>();
        _documents = Resolve<IMyDocumentAppService>();
    }

    private int CreateType(
        string nameEn,
        bool isLetterType = false,
        bool isWithdrawable = false,
        bool includeInWorkflow = true,
        bool isHiddenFromEmployee = false)
    {
        DocumentType type = null;
        UsingDbContext(context =>
        {
            type = context.Set<DocumentType>().Add(new DocumentType
            {
                TenantId = TenantId,
                Code = Guid.NewGuid().ToString("N").Substring(0, 8),
                NameEn = nameEn,
                NameAr = nameEn + " (ar)",
                IsLetterType = isLetterType,
                IsWithdrawable = isWithdrawable,
                IncludeInWorkflow = includeInWorkflow,
                IsHiddenFromEmployee = isHiddenFromEmployee
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>A document written straight to the table, with the dates and status the test is about.</summary>
    private int InsertDocument(
        int employeeId,
        int documentTypeId,
        DateTime issueDate,
        DateTime expiryDate,
        RequestStatus? status = null)
    {
        EmployeeDocument document = null;
        UsingDbContext(context =>
        {
            document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = documentTypeId,
                DocumentNum = "D" + Guid.NewGuid().ToString("N").Substring(0, 6),
                IssueDate = issueDate,
                ExpiryDate = expiryDate,
                WorkflowStatus = status
            }).Entity;
        });

        return document.Id;
    }

    private static RequestDocumentInput DocumentInput(int documentTypeId) => new RequestDocumentInput
    {
        DocumentTypeId = documentTypeId,
        DocumentNum = "N" + Guid.NewGuid().ToString("N").Substring(0, 6),
        IssueDate = DateTime.Today.AddYears(-1),
        ExpiryDate = DateTime.Today.AddYears(4)
    };

    private async Task<MyDocumentRequestDto> MyDocumentAsync(int documentId) =>
        (await _documents.GetMyDocuments(ListInput())).Items.Single(d => d.Id == documentId);

    // ---------------------------------------------------------------- what is offered

    [Fact]
    public async Task Should_Not_Offer_A_Letter_Type_As_A_Document_To_Record()
    {
        var letter = CreateType("Salary certificate", isLetterType: true);
        var passport = CreateType("Passport");

        var offered = (await _lookups.GetDocumentTypes()).Select(t => t.Id).ToList();

        offered.ShouldContain(passport);
        offered.ShouldNotContain(letter);
    }

    [Fact]
    public async Task Should_Not_Offer_A_Type_That_Is_Not_Included_In_Workflow()
    {
        var document = CreateType("Internal file", includeInWorkflow: false);
        var letter = CreateType("Internal letter", isLetterType: true, includeInWorkflow: false);

        (await _lookups.GetDocumentTypes()).Select(t => t.Id).ShouldNotContain(document);
        (await _lookups.GetLetterTypes()).Select(t => t.Id).ShouldNotContain(letter);
    }

    [Fact]
    public async Task Should_Not_Offer_A_Type_Hidden_From_Employees_As_A_Document_To_Record()
    {
        var hidden = CreateType("Security clearance", isHiddenFromEmployee: true);

        (await _lookups.GetDocumentTypes()).Select(t => t.Id).ShouldNotContain(hidden);
    }

    // ---------------------------------------------------------------- what is accepted

    [Fact]
    public async Task Should_Refuse_Recording_A_Document_Of_A_Letter_Type()
    {
        CreateCurrentUserEmployee("SW100");
        var letter = CreateType("Salary certificate", isLetterType: true);

        await Should.ThrowAsync<UserFriendlyException>(() => _documents.RequestDocument(DocumentInput(letter)));
    }

    [Fact]
    public async Task Should_Refuse_Recording_A_Document_Of_A_Type_Not_Included_In_Workflow()
    {
        CreateCurrentUserEmployee("SW110");
        var type = CreateType("Internal file", includeInWorkflow: false);

        await Should.ThrowAsync<UserFriendlyException>(() => _documents.RequestDocument(DocumentInput(type)));
    }

    [Fact]
    public async Task Should_Refuse_A_Draft_Of_A_Type_Hidden_From_Employees()
    {
        CreateCurrentUserEmployee("SW120");
        var hidden = CreateType("Security clearance", isHiddenFromEmployee: true);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _documents.SaveMyDocumentDraft(new SaveDocumentDraftInput { DocumentTypeId = hidden }));
    }

    [Fact]
    public async Task Should_Record_A_Document_Of_An_Ordinary_Type()
    {
        CreateCurrentUserEmployee("SW130");
        var passport = CreateType("Passport");

        var result = await _documents.RequestDocument(DocumentInput(passport));

        result.TransactionId.ShouldBeGreaterThan(0);
    }

    // ---------------------------------------------------------------- what is shown

    [Fact]
    public async Task Should_Hide_Documents_Of_A_Hidden_Type_From_The_Employee()
    {
        var me = CreateCurrentUserEmployee("SW200");
        var visible = CreateDocument(me, CreateType("Passport"));
        var hidden = CreateDocument(me, CreateType("Security clearance", isHiddenFromEmployee: true));

        var ids = (await _documents.GetMyDocuments(ListInput())).Items.Select(d => d.Id).ToList();

        ids.ShouldContain(visible);
        ids.ShouldNotContain(hidden);
    }

    // ---------------------------------------------------------------- withdrawable

    [Fact]
    public async Task Should_Allow_Withdrawing_A_Document_Of_A_Ticked_Type()
    {
        var me = CreateCurrentUserEmployee("SW300");
        var civilId = CreateDocument(me, CreateType("Civil ID", isWithdrawable: true));

        var offered = (await _lookups.GetMyWithdrawableDocuments()).Select(d => d.Id).ToList();
        var result = await Withdrawals.RequestPassportWithdrawal(WithdrawalInput(civilId));

        offered.ShouldContain(civilId);
        result.TransactionId.ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Should_Allow_More_Than_One_Withdrawable_Type()
    {
        var me = CreateCurrentUserEmployee("SW310");
        var passport = CreateDocument(me, CreateType("Passport", isWithdrawable: true));
        var licence = CreateDocument(me, CreateType("Driving licence", isWithdrawable: true));

        var offered = (await _lookups.GetMyWithdrawableDocuments()).Select(d => d.Id).ToList();

        offered.ShouldContain(passport);
        offered.ShouldContain(licence);
    }

    [Fact]
    public async Task Should_Refuse_Withdrawing_An_Unticked_Type_Once_Any_Type_Is_Ticked()
    {
        var me = CreateCurrentUserEmployee("SW320");
        CreateType("Passport", isWithdrawable: true);
        var certificate = CreateDocument(me, CreateType("Degree certificate"));

        (await _lookups.GetMyWithdrawableDocuments()).Select(d => d.Id).ShouldNotContain(certificate);

        await Should.ThrowAsync<UserFriendlyException>(() =>
            Withdrawals.RequestPassportWithdrawal(WithdrawalInput(certificate)));
    }

    [Fact]
    public async Task A_Ticked_Type_Should_Outrank_The_Legacy_Code()
    {
        // A tenant that has ticked a type has said which types are withdrawable; the type that merely
        // carries the legacy code "2" no longer is.
        var me = CreateCurrentUserEmployee("SW330");
        var legacyCoded = CreateDocument(me, CreateDocumentType("2"));
        var ticked = CreateDocument(me, CreateType("Civil ID", isWithdrawable: true));

        var offered = (await _lookups.GetMyWithdrawableDocuments()).Select(d => d.Id).ToList();

        offered.ShouldContain(ticked);
        offered.ShouldNotContain(legacyCoded);
    }

    [Fact]
    public async Task With_Nothing_Ticked_The_Legacy_Code_Should_Still_Decide()
    {
        var me = CreateCurrentUserEmployee("SW340");
        var legacyCoded = CreateDocument(me, CreateDocumentType("2"));
        var other = CreateDocument(me, CreateType("Degree certificate"));

        var offered = (await _lookups.GetMyWithdrawableDocuments()).Select(d => d.Id).ToList();

        offered.ShouldContain(legacyCoded);
        offered.ShouldNotContain(other);
    }

    // ---------------------------------------------------------------- what a document row reports

    [Fact]
    public async Task Should_Report_An_Active_Document_In_Custody_As_Withdrawable_And_Renewable()
    {
        var me = CreateCurrentUserEmployee("SW400");
        var passport = CreateDocument(me, CreateType("Passport", isWithdrawable: true));

        var row = await MyDocumentAsync(passport);

        row.DocumentStatus.ShouldBe(DocumentStatus.Active);
        row.IsOut.ShouldBeFalse();
        row.CanWithdraw.ShouldBeTrue();
        row.CanRenew.ShouldBeTrue();
    }

    [Fact]
    public async Task Should_Report_A_Superseded_Document_As_Inactive_And_Not_Withdrawable()
    {
        var me = CreateCurrentUserEmployee("SW410");
        var type = CreateType("Passport", isWithdrawable: true);
        var old = CreateDocument(me, type);
        var renewed = InsertDocument(me, type, DateTime.Today.AddMonths(-1), DateTime.Today.AddYears(9));

        var oldRow = await MyDocumentAsync(old);
        var renewedRow = await MyDocumentAsync(renewed);

        oldRow.DocumentStatus.ShouldBe(DocumentStatus.Inactive);
        oldRow.CanWithdraw.ShouldBeFalse();
        renewedRow.DocumentStatus.ShouldBe(DocumentStatus.Active);
        renewedRow.CanWithdraw.ShouldBeTrue();
    }

    [Fact]
    public async Task A_Pending_Renewal_Should_Not_Supersede_The_Document_On_File()
    {
        // Until somebody approves it the newer document is not in effect, so the one on file stays active.
        var me = CreateCurrentUserEmployee("SW420");
        var type = CreateType("Passport", isWithdrawable: true);
        var onFile = CreateDocument(me, type);
        var pending = InsertDocument(
            me, type, DateTime.Today.AddMonths(-1), DateTime.Today.AddYears(9), RequestStatus.Open);

        var onFileRow = await MyDocumentAsync(onFile);
        var pendingRow = await MyDocumentAsync(pending);

        onFileRow.DocumentStatus.ShouldBe(DocumentStatus.Active);
        pendingRow.DocumentStatus.ShouldBeNull();
        pendingRow.CanWithdraw.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Report_An_Expired_Document()
    {
        var me = CreateCurrentUserEmployee("SW430");
        var type = CreateType("Passport", isWithdrawable: true);
        var expired = InsertDocument(me, type, DateTime.Today.AddYears(-6), DateTime.Today.AddDays(-1));

        var row = await MyDocumentAsync(expired);

        row.DocumentStatus.ShouldBe(DocumentStatus.Expired);
        row.CanWithdraw.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Report_A_Document_As_Out_While_A_Withdrawal_In_Effect_Is_Not_Returned()
    {
        var me = CreateCurrentUserEmployee("SW440");
        var passport = CreateDocument(me, CreateType("Passport", isWithdrawable: true));

        InsertMovement(me, passport, RequestStatus.Approved);

        var row = await MyDocumentAsync(passport);

        row.IsOut.ShouldBeTrue();

        // Still withdrawable: asking again while it is out is an extension.
        row.CanWithdraw.ShouldBeTrue();
    }

    [Fact]
    public async Task A_Withdrawal_Still_Awaiting_Approval_Should_Not_Report_The_Document_As_Out()
    {
        var me = CreateCurrentUserEmployee("SW450");
        var passport = CreateDocument(me, CreateType("Passport", isWithdrawable: true));

        InsertMovement(me, passport, RequestStatus.Open);

        (await MyDocumentAsync(passport)).IsOut.ShouldBeFalse();
    }

    [Fact]
    public async Task Should_Not_Offer_Renewal_For_A_Type_Employees_May_Not_Record()
    {
        var me = CreateCurrentUserEmployee("SW460");
        var document = CreateDocument(me, CreateType("Internal file", includeInWorkflow: false));

        (await MyDocumentAsync(document)).CanRenew.ShouldBeFalse();
    }
}
