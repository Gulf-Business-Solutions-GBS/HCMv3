using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// The date rules on an employee document, ported from legacy <c>EmployeeDocument.PerformValidation</c> and
/// kept in its order:
///
/// <list type="number">
/// <item>the issue date must be given;</item>
/// <item>the expiry date must be given when the type says so (<see cref="DocumentType.IsExpiryDateRequired"/>);</item>
/// <item>the issue date cannot be after the expiry date;</item>
/// <item>unless the type allows it (<see cref="DocumentType.AllowDateOverlap"/>), the employee cannot hold two
/// documents of the type whose periods overlap -- or, for a type with no required expiry, two issued on the
/// same day.</item>
/// </list>
///
/// <para>
/// One place for them, called by HR's screen, the employee's own request and the approval handler, so the
/// three cannot disagree -- legacy ran them from the business object's <c>OnSaving</c>, which every path
/// went through.
/// </para>
///
/// <para>
/// A document still awaiting approval counts for the overlap, as it does for the duplicate check: two
/// overlapping requests must not both be in flight. Rejected and withdrawn ones do not.
/// </para>
/// </summary>
public interface IEmployeeDocumentDateRules : IDomainService
{
    /// <param name="expiryDate">Null, or an unset date, means no expiry date was given.</param>
    /// <param name="excludeDocumentId">The document being saved, so it is not compared with itself.</param>
    Task EnsureValidAsync(int employeeId, int documentTypeId, DateTime issueDate, DateTime? expiryDate, int? excludeDocumentId);
}

/// <inheritdoc cref="IEmployeeDocumentDateRules"/>
public class EmployeeDocumentDateRules : HCMv3DomainServiceBase, IEmployeeDocumentDateRules
{
    private readonly IRepository<EmployeeDocument> _documentRepository;
    private readonly IRepository<DocumentType> _documentTypeRepository;

    public EmployeeDocumentDateRules(
        IRepository<EmployeeDocument> documentRepository,
        IRepository<DocumentType> documentTypeRepository)
    {
        _documentRepository = documentRepository;
        _documentTypeRepository = documentTypeRepository;
    }

    [UnitOfWork]
    public virtual async Task EnsureValidAsync(
        int employeeId,
        int documentTypeId,
        DateTime issueDate,
        DateTime? expiryDate,
        int? excludeDocumentId)
    {
        var type = await _documentTypeRepository.GetAll()
            .Where(t => t.Id == documentTypeId)
            .Select(t => new { t.NameEn, t.IsExpiryDateRequired, t.AllowDateOverlap })
            .FirstOrDefaultAsync();

        if (type == null)
        {
            throw new UserFriendlyException(L("DocumentTypeNotFound"));
        }

        if (issueDate == default)
        {
            throw new UserFriendlyException(L("IssueDateIsRequired"));
        }

        var hasExpiry = expiryDate.HasValue && expiryDate.Value != default;

        if (type.IsExpiryDateRequired && !hasExpiry)
        {
            throw new UserFriendlyException(L("ExpiryDateIsRequired"));
        }

        if (hasExpiry && issueDate.Date > expiryDate.Value.Date)
        {
            throw new UserFriendlyException(L("ExpiryDateCannotBeEarlierThanIssueDate"));
        }

        if (type.AllowDateOverlap)
        {
            return;
        }

        var issue = issueDate.Date;

        // A document with no expiry date is on file until the end of time, which is how it is stored.
        var expiry = hasExpiry ? expiryDate.Value.Date : EmployeeDocument.NoExpiryDate;

        EmployeeDocumentPeriod clash;

        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            var others = _documentRepository.GetAll()
                .Where(d => d.EmployeeId == employeeId
                            && d.DocumentTypeId == documentTypeId
                            && (!excludeDocumentId.HasValue || d.Id != excludeDocumentId.Value)
                            && d.WorkflowStatus != RequestStatus.Rejected
                            && d.WorkflowStatus != RequestStatus.Closed);

            // Legacy: with a required expiry the two periods must not overlap; without one, only two
            // documents issued on the same day clash.
            others = type.IsExpiryDateRequired
                ? others.Where(d => !(d.IssueDate > expiry || d.ExpiryDate < issue))
                : others.Where(d => d.IssueDate == issue);

            clash = await others
                .Select(d => new EmployeeDocumentPeriod { IssueDate = d.IssueDate, ExpiryDate = d.ExpiryDate })
                .FirstOrDefaultAsync();
        }

        if (clash == null)
        {
            return;
        }

        throw new UserFriendlyException(type.IsExpiryDateRequired
            ? L("DocumentDatesOverlap", type.NameEn, clash.IssueDate.ToString("dd/MM/yyyy"), clash.ExpiryDate.ToString("dd/MM/yyyy"))
            : L("DocumentIssueDateAlreadyOnFile", type.NameEn, clash.IssueDate.ToString("dd/MM/yyyy")));
    }

    private class EmployeeDocumentPeriod
    {
        public DateTime IssueDate { get; set; }

        public DateTime ExpiryDate { get; set; }
    }
}
