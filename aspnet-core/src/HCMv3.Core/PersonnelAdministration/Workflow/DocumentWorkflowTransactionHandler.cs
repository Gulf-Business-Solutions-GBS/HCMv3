using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
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

    public DocumentWorkflowTransactionHandler(
        IRepository<EmployeeDocument> repository,
        IRepository<DocumentType> documentTypeRepository,
        IUnitOfWorkManager unitOfWorkManager)
        : base(repository, unitOfWorkManager)
    {
        _documentTypeRepository = documentTypeRepository;
    }

    public override string RequestType => DocumentRequestType;

    public override async Task ValidateBeforeSubmitAsync(WorkflowTransactionContext context)
    {
        var document = await GetTargetAsync(context);
        if (document == null)
        {
            return;
        }

        if (document.ExpiryDate.Date < document.IssueDate.Date)
        {
            throw new UserFriendlyException(L("ExpiryDateCannotBeEarlierThanIssueDate"));
        }

        if (document.DocumentNum.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("DocumentNumberIsRequired"));
        }
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
}
