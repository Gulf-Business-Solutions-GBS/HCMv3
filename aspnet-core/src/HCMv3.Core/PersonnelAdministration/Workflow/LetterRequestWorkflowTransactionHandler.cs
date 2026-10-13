using System;
using System.Linq;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.Workflow;
using HCMv3.Workflow.Services;

namespace HCMv3.PersonnelAdministration.Workflow;

/// <summary>
/// Workflow handler for employment letter requests (<see cref="EmployeeLetterRequest"/>) -- salary
/// certificates, experience certificates, salary-continuity letters.
///
/// <para>
/// Deliberately validation-only. Legacy's <c>PerformValidation</c> checked exactly five things -- employee,
/// effective date, document type, English addressee, Arabic addressee -- and those are restated here, where
/// they hold for every path into the row rather than only for the screen that happened to raise it.
/// </para>
///
/// <para>
/// Approving a letter request writes nothing to any other record; what it does is UNLOCK PRINTING. Legacy's
/// print action (<c>PrintLetterFormController</c>) refused outright with "Request is not approved. You
/// can't print." unless the request was Approved. (Legacy does carry an <c>if (RequestStatus == Approved)</c>
/// block, but it is commented out and is copy-pasted from the passport-withdrawal request -- it reads
/// <c>EmployeeDocument</c> and writes an <c>EmployeeDocumentMvmt</c>. It is not this type's behaviour and
/// is not ported.)
/// </para>
///
/// <para>
/// <b>One thing beyond legacy, asked for by the product owner (2026-10-13):</b> the letter is produced and
/// kept on the request at the moment of approval, so the employee finds it under Attachments without
/// waiting for HR to press Print Letter. HR's Print Letter still works and replaces that copy. A letter
/// that cannot be produced -- no report for the type, the report engine absent -- never undoes the
/// approval: it is logged, and HR prints it once the cause is fixed.
/// </para>
///
/// <para>
/// No duplicate guard either, for the same reason as HR services: asking for a second salary certificate a
/// month after the first is an ordinary thing to do, and refusing it would be a new rule rather than a port.
/// </para>
/// </summary>
public class LetterRequestWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeLetterRequest>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for letter requests.</summary>
    public const string LetterRequestType = "EmployeeLetterRequest";

    private readonly IRepository<DocumentType> _documentTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly ILetterRequestPrintService _printService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ILogger Logger { get; set; } = NullLogger.Instance;

    public LetterRequestWorkflowTransactionHandler(
        IRepository<EmployeeLetterRequest> repository,
        IRepository<DocumentType> documentTypeRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IRepository<Employee> employeeRepository,
        ILetterRequestPrintService printService)
        : base(repository, unitOfWorkManager)
    {
        _documentTypeRepository = documentTypeRepository;
        _employeeRepository = employeeRepository;
        _printService = printService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <summary>
    /// Approves the request, then produces the letter and keeps it on the request. See the class remarks:
    /// a letter that cannot be produced is logged and left for HR's Print Letter, never a failed approval.
    /// </summary>
    public override async Task OnApprovedAsync(WorkflowTransactionContext context)
    {
        await base.OnApprovedAsync(context);

        if (context.IsAmendment || !context.TargetEntityId.HasValue)
        {
            return;
        }

        try
        {
            // Stored first: the print reads the request back and prints only an approved one.
            await _unitOfWorkManager.Current.SaveChangesAsync();

            var printed = await _printService.PrintAsync(context.TargetEntityId.Value);

            if (!printed.SavedOnRequest)
            {
                Logger.Warn($"Letter request {context.TargetEntityId} was approved but its letter was not kept on it: {printed.NotSavedReason}");
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"Letter request {context.TargetEntityId} was approved but its letter could not be produced: {ex.Message}", ex);
        }
    }

    public override string RequestType => LetterRequestType;

    /// <summary>
    /// The five legacy required-field checks. All five matter for a document that leaves the building: a
    /// letter with no addressee, no date or no stated kind is not a certificate anybody can act on.
    /// </summary>
    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var letter = await GetTargetAsync(context);
        if (letter == null)
        {
            return;
        }

        if (letter.EmployeeId <= 0)
        {
            throw new UserFriendlyException(L("EmployeeIsRequired"));
        }

        if (letter.EffectiveDate == default)
        {
            throw new UserFriendlyException(L("EffectiveDateIsRequired"));
        }

        if (letter.DocumentTypeId <= 0)
        {
            throw new UserFriendlyException(L("DocumentTypeIsRequired"));
        }

        // Whitespace counts as blank. Legacy only tested for null, which let a space through and printed a
        // certificate with an empty "To:" line -- the bank rejects it and the employee comes back.
        if (letter.AddressToEn.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("AddressToEnIsRequired"));
        }

        if (letter.AddressToAr.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("AddressToArIsRequired"));
        }
    }

    /// <summary>
    /// What the letter is, when it speaks from, and who it is addressed to -- "Salary Certificate,
    /// 12 Sep 2026, to National Bank of Kuwait". The addressee belongs in the summary rather than being left
    /// to the detail screen: it is the part an approver is actually weighing up, since the same certificate
    /// going to a landlord and going to an embassy are different decisions.
    /// </summary>
    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var letter = await GetTargetAsync(context);
        if (letter == null)
        {
            return null;
        }

        var documentType = _documentTypeRepository.GetAll()
            .Where(t => t.Id == letter.DocumentTypeId)
            .Select(t => new { t.NameEn, t.NameAr })
            .FirstOrDefault();

        var on = letter.EffectiveDate.ToString("dd MMM yyyy");

        var addressedToEn = letter.AddressToEn.IsNullOrWhiteSpace() ? null : letter.AddressToEn.Trim();
        var addressedToAr = letter.AddressToAr.IsNullOrWhiteSpace() ? addressedToEn : letter.AddressToAr.Trim();

        var en = $"{documentType?.NameEn ?? "Letter"}, {on}";
        var ar = $"{documentType?.NameAr ?? "خطاب"}، {on}";

        if (addressedToEn != null)
        {
            en += $", to {addressedToEn}";
        }

        if (addressedToAr != null)
        {
            ar += $"، إلى {addressedToAr}";
        }

        return new WorkflowSummary(en, ar);
    }

    /// <summary>
    /// Every field the letter form captures, in the order it captures them, under the names the Angular
    /// letter form binds by -- so the approver and the Details view read the form itself, read-only. The
    /// kind of letter and the employee are sent as the words the form shows, in the reader's language.
    /// </summary>
    public override async Task<WorkflowRequestDetail> BuildDetailAsync(WorkflowTransactionContext context)
    {
        var letter = await GetTargetAsync(context);
        if (letter == null)
        {
            return await base.BuildDetailAsync(context);
        }

        var documentType = _documentTypeRepository.GetAll()
            .Where(t => t.Id == letter.DocumentTypeId)
            .Select(t => new { t.NameEn, t.NameAr })
            .FirstOrDefault();

        var employee = _employeeRepository.GetAll()
            .Where(e => e.Id == letter.EmployeeId)
            .Select(e => new { e.Code, e.FullNameEn, e.FullNameAr })
            .FirstOrDefault();

        var detail = new WorkflowRequestDetail
        {
            SubmissionComment = letter.Comments
        };

        return detail
            .Add(WorkflowDetailField.Text(WorkflowDetailFieldNames.RequestNumber, L("RequestNumber"), context.Request?.RequestNumber))
            .Add(WorkflowDetailField.Text("Employee", L("Employee"), employee == null ? null : WorkflowDetailDisplay.Employee(employee.Code, employee.FullNameEn, employee.FullNameAr)))
            .Add(WorkflowDetailField.Text("DocumentType", L("DocumentType"), WorkflowDetailDisplay.Name(documentType?.NameEn, documentType?.NameAr)))
            .Add(WorkflowDetailField.Date(nameof(EmployeeLetterRequest.EffectiveDate), L("EffectiveDate"), letter.EffectiveDate))
            .Add(WorkflowDetailField.Text(nameof(EmployeeLetterRequest.AddressToEn), L("AddressToEn"), letter.AddressToEn))
            .Add(WorkflowDetailField.Text(nameof(EmployeeLetterRequest.AddressToAr), L("AddressToAr"), letter.AddressToAr))
            .Add(WorkflowDetailField.LongText(nameof(EmployeeLetterRequest.Comments), L("Comments"), letter.Comments));
    }
}
