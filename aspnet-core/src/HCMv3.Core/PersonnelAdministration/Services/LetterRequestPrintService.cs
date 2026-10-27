using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using Abp.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.GeneralMasterInfo;
using HCMv3.ReportManagement;
using HCMv3.ReportManagement.Services;
using HCMv3.Storage;
using HCMv3.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>A letter printed for a request: the PDF, and whether a copy was kept on the request.</summary>
public class PrintedLetter
{
    public string FileName { get; set; }

    public byte[] Content { get; set; }

    /// <summary>
    /// False when the copy could not be kept on the request -- the tenant's document settings refused it
    /// (PDF not an allowed type, storage full). The letter itself is still returned: HR came to print, and a
    /// storage rule must not stand between them and the paper.
    /// </summary>
    public bool SavedOnRequest { get; set; }

    /// <summary>Why the copy was not kept, in the reader's language. Null when it was.</summary>
    public string NotSavedReason { get; set; }
}

/// <summary>
/// Prints the letter an approved <see cref="EmployeeLetterRequest"/> asked for. Ported from legacy
/// <c>HCM10.Module.Controllers.Printing.PrintLetterFormController</c>, and kept to what it did:
///
/// <list type="bullet">
/// <item>only an Approved request prints;</item>
/// <item>the report is found from the document type's English name with the spaces taken out -- "Salary
/// Certificate" prints the report named SalaryCertificate...;</item>
/// <item>the report is given the employee's code and today's date, and nothing else. The addressee the
/// employee typed is on the request for HR to read; no certificate layout prints it, in legacy or here.</item>
/// </list>
///
/// <para>
/// The one thing added: the PDF is kept on the request as an attachment, replacing the copy from an earlier
/// print, so what was issued can be found again.
/// </para>
/// </summary>
public interface ILetterRequestPrintService : IDomainService
{
    Task<PrintedLetter> PrintAsync(int letterRequestId);

    /// <summary>
    /// The catalog report a document type prints as, or null when it has none. Exposed so a screen can say
    /// "no report" before anybody presses Print.
    /// </summary>
    Task<PrintReport> FindReportAsync(string documentTypeNameEn);

    /// <summary>
    /// The report a document type prints as: the one chosen on the type when there is one, otherwise the one
    /// its name finds. Null when the chosen report can no longer be printed -- it is never quietly replaced
    /// by a different one.
    /// </summary>
    Task<PrintReport> FindReportAsync(DocumentType documentType);
}

/// <inheritdoc cref="ILetterRequestPrintService"/>
public class LetterRequestPrintService : HCMv3DomainServiceBase, ILetterRequestPrintService
{
    /// <summary>The attachment owner type the printed copy is kept under.</summary>
    public const string AttachmentEntityType = "EmployeeLetterRequest";

    /// <summary>The two parameters legacy set on every letter (<c>ReportsDataSourceHelper_BeforeShowPreview</c>).</summary>
    public const string EmployeeCodeParameter = "PrmEmpCode";

    public const string DateParameter = "PrmDate";

    private readonly IRepository<EmployeeLetterRequest> _letterRepository;
    private readonly IRepository<PrintReport> _printReportRepository;
    private readonly IPrintReportPdfRenderer _renderer;
    private readonly IAttachmentManager _attachmentManager;

    public LetterRequestPrintService(
        IRepository<EmployeeLetterRequest> letterRepository,
        IRepository<PrintReport> printReportRepository,
        IPrintReportPdfRenderer renderer,
        IAttachmentManager attachmentManager)
    {
        _letterRepository = letterRepository;
        _printReportRepository = printReportRepository;
        _renderer = renderer;
        _attachmentManager = attachmentManager;
    }

    [UnitOfWork]
    public virtual async Task<PrintedLetter> PrintAsync(int letterRequestId)
    {
        EmployeeLetterRequest letter;

        // Looked up with the filter off so a request that is not approved is refused for that reason, rather
        // than reported as not found.
        using (CurrentUnitOfWork.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            letter = await _letterRepository.GetAll()
                .Include(l => l.Employee)
                .Include(l => l.DocumentType)
                .FirstOrDefaultAsync(l => l.Id == letterRequestId);
        }

        if (letter == null)
        {
            throw new UserFriendlyException(L("RequestNotFound"));
        }

        if (letter.WorkflowStatus != RequestStatus.Approved)
        {
            throw new UserFriendlyException(L("RequestIsNotApprovedYouCannotPrint"));
        }

        if (letter.DocumentType == null || letter.DocumentType.NameEn.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("DocumentTypeIsNotDefined"));
        }

        var report = await FindReportAsync(letter.DocumentType);
        if (report == null)
        {
            throw new UserFriendlyException(L("NoReportFoundForDocumentType", letter.DocumentType.NameEn));
        }

        var content = await _renderer.RenderAsync(report.ReportName, new Dictionary<string, object>
        {
            [EmployeeCodeParameter] = letter.Employee?.Code ?? string.Empty,
            [DateParameter] = Clock.Now
        });

        var printed = new PrintedLetter
        {
            FileName = $"{report.ReportName}_{letter.Employee?.Code}_{letter.Id}.pdf",
            Content = content
        };

        await KeepCopyAsync(letter.Id, printed);

        return printed;
    }

    [UnitOfWork]
    public virtual async Task<PrintReport> FindReportAsync(DocumentType documentType)
    {
        if (documentType == null)
        {
            return null;
        }

        if (!documentType.PrintReportId.HasValue)
        {
            return await FindReportAsync(documentType.NameEn);
        }

        // The report HR chose for the type. One that has since been switched off, deleted or is not ported
        // prints nothing rather than falling back to the name: HR picked a report, and a different one
        // coming out of the printer would be worse than being told.
        var chosen = await _printReportRepository.GetAll()
            .FirstOrDefaultAsync(r => r.Id == documentType.PrintReportId.Value && r.IsActive);

        return chosen != null && PortedReportCatalog.Names.Contains(chosen.ReportName, StringComparer.OrdinalIgnoreCase)
            ? chosen
            : null;
    }

    [UnitOfWork]
    public virtual async Task<PrintReport> FindReportAsync(string documentTypeNameEn)
    {
        // "Salary Certificate" => "SalaryCertificate", as legacy did.
        var name = (documentTypeNameEn ?? string.Empty).Replace(" ", string.Empty);
        if (name.Length == 0)
        {
            return null;
        }

        // Legacy looked for a report class whose name contains "rpt" + that name; the catalog here holds the
        // class name without its "rpt" prefix, so the same test is "starts with". Only reports the tenant has
        // been given and that are actually ported.
        var ported = PortedReportCatalog.Names;

        var candidates = (await _printReportRepository.GetAll()
                .Where(r => r.IsActive && r.ReportName.StartsWith(name))
                .OrderBy(r => r.Code)
                .ToListAsync())
            .Where(r => ported.Contains(r.ReportName, StringComparer.OrdinalIgnoreCase))
            .ToList();

        // The report named exactly for the type when there is one; otherwise the first by catalog code, so
        // the choice among language variants (SalaryCertificateEn, SalaryCertificateAr) is at least stable --
        // legacy took whichever its query happened to return first.
        return candidates.FirstOrDefault(r => string.Equals(r.ReportName, name, StringComparison.OrdinalIgnoreCase))
               ?? candidates.FirstOrDefault();
    }

    /// <summary>
    /// Keeps the PDF on the request, in place of the copy an earlier print left. A refusal by the tenant's
    /// document settings is reported, not thrown.
    /// </summary>
    private async Task KeepCopyAsync(int letterRequestId, PrintedLetter printed)
    {
        var entityId = letterRequestId.ToString();

        try
        {
            var earlier = (await _attachmentManager.GetListAsync(AttachmentEntityType, entityId))
                .Where(a => string.Equals(a.FileName, printed.FileName, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var attachment in earlier)
            {
                await _attachmentManager.DeleteAsync(attachment);
            }

            await _attachmentManager.AddAsync(AttachmentEntityType, entityId, printed.FileName, printed.Content);

            printed.SavedOnRequest = true;
        }
        catch (UserFriendlyException ex)
        {
            printed.SavedOnRequest = false;
            printed.NotSavedReason = ex.Message;
        }
    }
}
