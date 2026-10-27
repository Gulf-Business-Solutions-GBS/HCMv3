using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Refuses a second copy of the same document: same employee, same document type, same document number.
///
/// <para>
/// Legacy <c>DocumentRequest.OnSaving</c> checked for an identical <c>EmployeeDocument</c> when the request
/// was approved and quietly skipped creating it. Here the request row IS the document, so the equivalent is to
/// refuse the duplicate up front, with a reason, instead of approving a request that then does nothing.
/// </para>
///
/// <para>
/// Pending requests count (the ApprovedOnly filter is switched off), so two requests for the same passport
/// cannot both be in flight. Rejected and withdrawn ones do not: the employee may try again.
/// </para>
/// </summary>
public interface IEmployeeDocumentDuplicateCheck : IDomainService
{
    Task EnsureNotDuplicateAsync(int employeeId, int documentTypeId, string documentNum, int? excludeDocumentId);
}

/// <inheritdoc cref="IEmployeeDocumentDuplicateCheck"/>
public class EmployeeDocumentDuplicateCheck : HCMv3DomainServiceBase, IEmployeeDocumentDuplicateCheck
{
    private readonly IRepository<EmployeeDocument> _documentRepository;

    public EmployeeDocumentDuplicateCheck(IRepository<EmployeeDocument> documentRepository)
    {
        _documentRepository = documentRepository;
    }

    public async Task EnsureNotDuplicateAsync(int employeeId, int documentTypeId, string documentNum, int? excludeDocumentId)
    {
        var number = documentNum?.Trim();
        if (string.IsNullOrEmpty(number))
        {
            return;
        }

        bool exists;
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            exists = await _documentRepository.GetAll()
                .Where(d => d.EmployeeId == employeeId
                            && d.DocumentTypeId == documentTypeId
                            && d.DocumentNum.Trim() == number
                            && (!excludeDocumentId.HasValue || d.Id != excludeDocumentId.Value)
                            && d.WorkflowStatus != RequestStatus.Rejected
                            && d.WorkflowStatus != RequestStatus.Closed)
                .AnyAsync();
        }

        if (exists)
        {
            throw new UserFriendlyException(L("DocumentAlreadyRecorded", number));
        }
    }
}
