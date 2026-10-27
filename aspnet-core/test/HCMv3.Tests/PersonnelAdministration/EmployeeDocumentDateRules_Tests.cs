using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using HCMv3.Ess;
using HCMv3.Ess.Dto;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Tests.Ess;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PersonnelAdministration;

/// <summary>
/// The date rules on an employee document, as legacy <c>EmployeeDocument.PerformValidation</c> had them: the
/// two switches on the document type decide whether an expiry date must be given and whether two documents
/// of the type may overlap. The same rules have to hold on HR's screen and on the employee's own request.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EmployeeDocumentDateRules_Tests : PassportWithdrawalTestBase
{
    private readonly IEmployeeDocumentDateRules _rules;
    private readonly IEmployeeDocumentsAppService _hrDocuments;
    private readonly IMyDocumentAppService _myDocuments;

    public EmployeeDocumentDateRules_Tests()
    {
        _rules = Resolve<IEmployeeDocumentDateRules>();
        _hrDocuments = Resolve<IEmployeeDocumentsAppService>();
        _myDocuments = Resolve<IMyDocumentAppService>();
    }

    private static readonly DateTime Issue = new DateTime(2024, 3, 1);
    private static readonly DateTime Expiry = new DateTime(2029, 2, 28);

    private int CreateType(string nameEn, bool expiryRequired, bool allowOverlap)
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
                IsExpiryDateRequired = expiryRequired,
                AllowDateOverlap = allowOverlap
            }).Entity;
        });

        return type.Id;
    }

    /// <summary>A document already on file, with the dates and status the test is about.</summary>
    private int OnFile(int employeeId, int typeId, DateTime issue, DateTime expiry, RequestStatus? status = null)
    {
        EmployeeDocument document = null;
        UsingDbContext(context =>
        {
            document = context.Set<EmployeeDocument>().Add(new EmployeeDocument
            {
                TenantId = TenantId,
                EmployeeId = employeeId,
                DocumentTypeId = typeId,
                DocumentNum = "F" + Guid.NewGuid().ToString("N").Substring(0, 6),
                IssueDate = issue,
                ExpiryDate = expiry,
                WorkflowStatus = status
            }).Entity;
        });

        return document.Id;
    }

    private EmployeeDocument Stored(int id) =>
        UsingDbContext(context => context.Set<EmployeeDocument>().IgnoreQueryFilters().Single(d => d.Id == id));

    private Task CheckAsync(int employeeId, int typeId, DateTime issue, DateTime? expiry, int? excludeId = null) =>
        _rules.EnsureValidAsync(employeeId, typeId, issue, expiry, excludeId);

    // ---------------------------------------------------------------- the dates themselves

    [Fact]
    public async Task Should_Refuse_A_Missing_Issue_Date()
    {
        var me = CreateEmployee("DR100");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: true);

        await Should.ThrowAsync<UserFriendlyException>(() => CheckAsync(me, type, default, Expiry));
    }

    [Fact]
    public async Task Should_Refuse_A_Missing_Expiry_Date_When_The_Type_Requires_One()
    {
        var me = CreateEmployee("DR110");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: true);

        await Should.ThrowAsync<UserFriendlyException>(() => CheckAsync(me, type, Issue, null));
        await Should.ThrowAsync<UserFriendlyException>(() => CheckAsync(me, type, Issue, default(DateTime)));
    }

    [Fact]
    public async Task Should_Accept_A_Missing_Expiry_Date_When_The_Type_Does_Not_Require_One()
    {
        var me = CreateEmployee("DR120");
        var type = CreateType("Degree", expiryRequired: false, allowOverlap: true);

        await Should.NotThrowAsync(() => CheckAsync(me, type, Issue, null));
    }

    [Fact]
    public async Task Should_Refuse_An_Issue_Date_After_The_Expiry_Date()
    {
        var me = CreateEmployee("DR130");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: true);

        await Should.ThrowAsync<UserFriendlyException>(() => CheckAsync(me, type, Expiry, Issue));
    }

    // ---------------------------------------------------------------- overlap

    [Fact]
    public async Task Should_Refuse_An_Overlapping_Document_When_The_Type_Does_Not_Allow_It()
    {
        var me = CreateEmployee("DR200");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: false);
        OnFile(me, type, Issue, Expiry);

        // A renewal entered a year before the old one runs out.
        var exception = await Should.ThrowAsync<UserFriendlyException>(() =>
            CheckAsync(me, type, Expiry.AddYears(-1), Expiry.AddYears(4)));

        exception.Message.ShouldContain("Passport");
    }

    [Fact]
    public async Task Should_Accept_A_Document_That_Starts_After_The_Other_Ends()
    {
        var me = CreateEmployee("DR210");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: false);
        OnFile(me, type, Issue, Expiry);

        await Should.NotThrowAsync(() => CheckAsync(me, type, Expiry.AddDays(1), Expiry.AddYears(5)));
    }

    [Fact]
    public async Task Should_Accept_An_Overlapping_Document_When_The_Type_Allows_It()
    {
        var me = CreateEmployee("DR220");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: true);
        OnFile(me, type, Issue, Expiry);

        await Should.NotThrowAsync(() => CheckAsync(me, type, Expiry.AddYears(-1), Expiry.AddYears(4)));
    }

    [Fact]
    public async Task Without_A_Required_Expiry_Only_The_Same_Issue_Date_Should_Clash()
    {
        var me = CreateEmployee("DR230");
        var type = CreateType("Degree", expiryRequired: false, allowOverlap: false);
        OnFile(me, type, Issue, EmployeeDocument.NoExpiryDate);

        await Should.ThrowAsync<UserFriendlyException>(() => CheckAsync(me, type, Issue, null));
        await Should.NotThrowAsync(() => CheckAsync(me, type, Issue.AddDays(1), null));
    }

    [Fact]
    public async Task Should_Not_Compare_A_Document_With_Itself()
    {
        var me = CreateEmployee("DR240");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: false);
        var existing = OnFile(me, type, Issue, Expiry);

        await Should.NotThrowAsync(() => CheckAsync(me, type, Issue, Expiry.AddMonths(1), existing));
    }

    [Fact]
    public async Task Should_Not_Compare_With_Another_Employee_Or_Another_Type()
    {
        var me = CreateEmployee("DR250");
        var colleague = CreateEmployee("DR251");
        var passport = CreateType("Passport", expiryRequired: true, allowOverlap: false);
        var civilId = CreateType("Civil ID", expiryRequired: true, allowOverlap: false);
        OnFile(colleague, passport, Issue, Expiry);
        OnFile(me, civilId, Issue, Expiry);

        await Should.NotThrowAsync(() => CheckAsync(me, passport, Issue, Expiry));
    }

    [Fact]
    public async Task A_Document_Awaiting_Approval_Should_Count_And_A_Rejected_One_Should_Not()
    {
        var me = CreateEmployee("DR260");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: false);
        var rejected = OnFile(me, type, Issue, Expiry, RequestStatus.Rejected);

        await Should.NotThrowAsync(() => CheckAsync(me, type, Issue, Expiry));

        OnFile(me, type, Issue, Expiry, RequestStatus.Open);

        await Should.ThrowAsync<UserFriendlyException>(() => CheckAsync(me, type, Issue, Expiry));
        rejected.ShouldBeGreaterThan(0);
    }

    // ---------------------------------------------------------------- HR's screen

    [Fact]
    public async Task Hr_Should_Be_Refused_An_Overlapping_Document()
    {
        var employee = CreateEmployee("DR300");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: false);
        OnFile(employee, type, Issue, Expiry);

        await Should.ThrowAsync<UserFriendlyException>(() => _hrDocuments.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee,
            DocumentTypeId = type,
            DocumentNum = "HRNEW1",
            IssueDate = Expiry.AddMonths(-6),
            ExpiryDate = Expiry.AddYears(5),
            RecordDirectly = true
        }));
    }

    [Fact]
    public async Task Hr_Should_Be_Refused_A_Blank_Expiry_Date_When_The_Type_Requires_One()
    {
        var employee = CreateEmployee("DR310");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: true);

        await Should.ThrowAsync<UserFriendlyException>(() => _hrDocuments.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            EmployeeId = employee,
            DocumentTypeId = type,
            DocumentNum = "HRNEW2",
            IssueDate = Issue,
            ExpiryDate = null,
            RecordDirectly = true
        }));
    }

    [Fact]
    public async Task Hr_Should_Be_Refused_An_Edit_That_Moves_A_Document_Onto_Another()
    {
        var employee = CreateEmployee("DR320");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: false);
        OnFile(employee, type, Issue, Expiry);
        var later = OnFile(employee, type, Expiry.AddDays(1), Expiry.AddYears(5));

        await Should.ThrowAsync<UserFriendlyException>(() => _hrDocuments.CreateOrEdit(new CreateOrEditEmployeeDocumentDto
        {
            Id = later,
            EmployeeId = employee,
            DocumentTypeId = type,
            DocumentNum = "HREDIT",
            IssueDate = Expiry.AddMonths(-1),
            ExpiryDate = Expiry.AddYears(5)
        }));
    }

    // ---------------------------------------------------------------- the employee's own request

    [Fact]
    public async Task An_Employee_Should_Be_Refused_An_Overlapping_Document()
    {
        var me = CreateCurrentUserEmployee("DR400");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: false);
        OnFile(me, type, Issue, Expiry);

        await Should.ThrowAsync<UserFriendlyException>(() => _myDocuments.RequestDocument(new RequestDocumentInput
        {
            DocumentTypeId = type,
            DocumentNum = "MYNEW1",
            IssueDate = Expiry.AddMonths(-6),
            ExpiryDate = Expiry.AddYears(5)
        }));
    }

    [Fact]
    public async Task An_Employee_Should_Be_Refused_A_Blank_Expiry_Date_When_The_Type_Requires_One()
    {
        CreateCurrentUserEmployee("DR410");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: true);

        await Should.ThrowAsync<UserFriendlyException>(() => _myDocuments.RequestDocument(new RequestDocumentInput
        {
            DocumentTypeId = type,
            DocumentNum = "MYNEW2",
            IssueDate = Issue
        }));
    }

    [Fact]
    public async Task An_Employee_Document_Without_An_Expiry_Date_Should_Be_Stored_As_Never_Expiring()
    {
        CreateCurrentUserEmployee("DR420");
        var type = CreateType("Degree", expiryRequired: false, allowOverlap: true);

        var result = await _myDocuments.RequestDocument(new RequestDocumentInput
        {
            DocumentTypeId = type,
            DocumentNum = "MYNEW3",
            IssueDate = Issue
        });

        Stored(result.TransactionId).ExpiryDate.ShouldBe(EmployeeDocument.NoExpiryDate);
    }

    [Fact]
    public async Task A_Draft_Without_An_Expiry_Date_Should_Be_Submitted_As_Never_Expiring()
    {
        CreateCurrentUserEmployee("DR430");
        var type = CreateType("Degree", expiryRequired: false, allowOverlap: true);

        var draft = await _myDocuments.SaveMyDocumentDraft(new SaveDocumentDraftInput
        {
            DocumentTypeId = type,
            DocumentNum = "MYNEW4",
            IssueDate = Issue
        });

        await _myDocuments.SubmitMyDocumentDraft(new Abp.Application.Services.Dto.EntityDto(draft.TransactionId));

        Stored(draft.TransactionId).ExpiryDate.ShouldBe(EmployeeDocument.NoExpiryDate);
    }

    [Fact]
    public async Task A_Draft_Should_Be_Refused_At_Submit_When_Its_Type_Requires_An_Expiry_Date()
    {
        CreateCurrentUserEmployee("DR440");
        var type = CreateType("Passport", expiryRequired: true, allowOverlap: true);

        // Saving is allowed: a draft is what somebody files because something is still missing.
        var draft = await _myDocuments.SaveMyDocumentDraft(new SaveDocumentDraftInput
        {
            DocumentTypeId = type,
            DocumentNum = "MYNEW5",
            IssueDate = Issue
        });

        await Should.ThrowAsync<UserFriendlyException>(() =>
            _myDocuments.SubmitMyDocumentDraft(new Abp.Application.Services.Dto.EntityDto(draft.TransactionId)));
    }
}
