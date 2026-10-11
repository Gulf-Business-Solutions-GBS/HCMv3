using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.UI;

namespace HCMv3.ReportManagement.Services;

/// <summary>
/// Renders one ported print report to a PDF, from code rather than from the report viewer.
///
/// <para>
/// The report classes live in <c>HCMv3.Reports</c>, which only the web host references, so the host supplies
/// the real implementation and replaces <see cref="NullPrintReportPdfRenderer"/> with it at start-up. Callers
/// here in Core and in the Application layer depend on this contract alone.
/// </para>
/// </summary>
public interface IPrintReportPdfRenderer
{
    /// <summary>
    /// Renders the report named <paramref name="reportName"/> (<see cref="PrintReport.ReportName"/>) for the
    /// current tenant.
    ///
    /// <para>
    /// <paramref name="parameters"/> are matched to the report's own parameters by name. One the report does
    /// not declare is ignored, so a caller can pass what it has without knowing each layout.
    /// </para>
    /// </summary>
    Task<byte[]> RenderAsync(string reportName, IReadOnlyDictionary<string, object> parameters);
}

/// <summary>
/// The stand-in used wherever the report classes are not loaded -- the unit tests, the migrator. It refuses
/// plainly rather than returning an empty file that would be saved and handed to somebody as a letter.
/// </summary>
public class NullPrintReportPdfRenderer : IPrintReportPdfRenderer, ITransientDependency
{
    public Task<byte[]> RenderAsync(string reportName, IReadOnlyDictionary<string, object> parameters)
    {
        throw new UserFriendlyException("Report rendering is not available in this host.");
    }
}
