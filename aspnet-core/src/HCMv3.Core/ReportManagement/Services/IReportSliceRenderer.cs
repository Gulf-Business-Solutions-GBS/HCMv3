using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.ReportManagement.Services;

/// <summary>A rendered slice, ready to deliver.</summary>
public class RenderedReportSlice
{
    public byte[] Content { get; set; }

    public string FileName { get; set; }

    public string ContentType { get; set; }

    /// <summary>
    /// Rows in the file AFTER the recipient's scope was applied. Zero is a legitimate outcome and is
    /// logged as SkippedEmpty rather than delivered -- mailing an empty attachment every month trains
    /// recipients to ignore the whole schedule.
    /// </summary>
    public int RowCount { get; set; }
}

/// <summary>
/// Renders one burst slice for one recipient, in the schedule's chosen format.
///
/// SEPARATE FROM THE BURST RESOLVER ON PURPOSE. Resolution decides who gets what and is pure
/// org-chart logic; rendering executes the underlying report and is where the two report families
/// diverge completely -- a grid report goes through its app service and a filter DTO, a print report
/// through DevExpress with named parameters. Keeping them apart means the security-critical half can
/// be tested without executing a single report.
/// </summary>
public interface IReportSliceRenderer
{
    /// <summary>Whether this renderer handles the schedule's report family.</summary>
    bool CanRender(ScheduledReportKind kind);

    /// <summary>
    /// Renders <paramref name="slice"/> restricted to <paramref name="visibleEmployeeIds"/>.
    ///
    /// A null <paramref name="visibleEmployeeIds"/> means UNRESTRICTED -- either row-level security is
    /// switched off tenant-wide, or this recipient was explicitly exempted. An EMPTY set means the
    /// recipient is entitled to nothing and must receive nothing. Implementations must not treat the
    /// two the same; conflating them is how a scoped report turns into a full data dump.
    /// </summary>
    Task<RenderedReportSlice> RenderAsync(
        ReportSchedule schedule,
        ReportBurstSlice slice,
        HashSet<int> visibleEmployeeIds);
}
