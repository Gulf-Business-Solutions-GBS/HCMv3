using System.Linq;
using System.Threading.Tasks;
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
/// Workflow handler for employee documents (<see cref="EmployeeDocument"/>) -- passports, visas, civil IDs
/// and the like.
///
/// <para>
/// Unlike leave or assets, nothing here is a finite resource being consumed, so there is no exclusivity or
/// balance rule. What approval buys is a second pair of eyes on data the payroll and government-reporting
/// side later depends on, which is why the validation is about the dates being coherent rather than about
/// contention with other requests.
/// </para>
/// </summary>
public class DocumentWorkflowTransactionHandler : WorkflowTransactionHandlerBase<EmployeeDocument>
{
    /// <summary>Must match the <c>WorkflowTransactionType.Code</c> seeded for documents.</summary>
    public const string DocumentRequestType = "EmployeeDocument";

    private readonly IRepository<DocumentType> _documentTypeRepository;
    private readonly IEmployeeDocumentDuplicateCheck _duplicateCheck;
    private readonly IEmployeeDocumentDateRules _dateRules;
    private readonly IRepository<Employee> _employeeRepository;

    public DocumentWorkflowTransactionHandler(
        IRepository<EmployeeDocument> repository,
        IRepository<DocumentType> documentTypeRepository,
        IUnitOfWorkManager unitOfWorkManager,
        IEmployeeDocumentDuplicateCheck duplicateCheck,
        IRepository<Employee> employeeRepository,
        IEmployeeDocumentDateRules dateRules)
        : base(repository, unitOfWorkManager)
    {
        _documentTypeRepository = documentTypeRepository;
        _duplicateCheck = duplicateCheck;
        _dateRules = dateRules;
        _employeeRepository = employeeRepository;
    }

    public override string RequestType => DocumentRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var document = await GetTargetAsync(context);
        if (document == null)
        {
            return;
        }

        // Legacy's date rules: required expiry, issue before expiry, no overlapping documents of the type.
        await _dateRules.EnsureValidAsync(
            document.EmployeeId, document.DocumentTypeId, document.IssueDate, document.ExpiryDate, document.Id);

        if (document.DocumentNum.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("DocumentNumberIsRequired"));
        }

        // Legacy skipped a duplicate silently on approval; here it is refused at submit, with a reason.
        await _duplicateCheck.EnsureNotDuplicateAsync(
            document.EmployeeId, document.DocumentTypeId, document.DocumentNum, document.Id);
    }

    public override async Task<WorkflowSummary> BuildSummaryAsync(WorkflowTransactionContext context)
    {
        var document = await GetTargetAsync(context);
        if (document == null)
        {
            return null;
        }

        var documentType = _documentTypeRepository.GetAll().FirstOrDefault(t => t.Id == document.DocumentTypeId);
        var expiry = document.ExpiryDate.ToString("dd MMM yyyy");

        return new WorkflowSummary(
            $"{documentType?.NameEn ?? "Document"} {document.DocumentNum}, expires {expiry}",
            $"{documentType?.NameAr ?? "مستند"} {document.DocumentNum}، ينتهي {expiry}");
    }

    /// <summary>
    /// Every field the document form captures, in the order it captures them, under the names the Angular
    /// document form binds by -- so the approver and the Details view read the form itself, read-only. The
    /// document type and the employee are sent as the words the form shows, in the reader's language.
    ///
    /// <para>
    /// <see cref="EmployeeDocument.Description"/> is not on the self-service form; it travels last because
    /// a document HR recorded may carry one, and the same projection serves that row.
    /// </para>
    /// </summary>
    public override async Task<WorkflowRequestDetail> BuildDetailAsync(WorkflowTransactionContext context)
    {
        var document = await GetTargetAsync(context);
        if (document == null)
        {
            return await base.BuildDetailAsync(context);
        }

        var documentType = _documentTypeRepository.GetAll()
            .Where(t => t.Id == document.DocumentTypeId)
            .Select(t => new { t.NameEn, t.NameAr })
            .FirstOrDefault();

        var employee = _employeeRepository.GetAll()
            .Where(e => e.Id == document.EmployeeId)
            .Select(e => new { e.Code, e.FullNameEn, e.FullNameAr })
            .FirstOrDefault();

        var detail = new WorkflowRequestDetail
        {
            SubmissionComment = document.Comments
        };

        return detail
            .Add(WorkflowDetailField.Text(WorkflowDetailFieldNames.RequestNumber, L("RequestNumber"), context.Request?.RequestNumber))
            .Add(WorkflowDetailField.Text("Employee", L("Employee"), employee == null ? null : WorkflowDetailDisplay.Employee(employee.Code, employee.FullNameEn, employee.FullNameAr)))
            .Add(WorkflowDetailField.Text("DocumentType", L("DocumentType"), WorkflowDetailDisplay.Name(documentType?.NameEn, documentType?.NameAr)))
            .Add(WorkflowDetailField.Text("DocumentNumber", L("DocumentNumber"), document.DocumentNum))
            .Add(WorkflowDetailField.Date(nameof(EmployeeDocument.IssueDate), L("IssueDate"), document.IssueDate))
            .Add(WorkflowDetailField.Date(nameof(EmployeeDocument.ExpiryDate), L("ExpiryDate"), document.ExpiryDate))
            .Add(WorkflowDetailField.LongText(nameof(EmployeeDocument.Comments), L("Comments"), document.Comments))
            .Add(WorkflowDetailField.LongText(nameof(EmployeeDocument.Description), L("Description"), document.Description));
    }
}
