using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.ImportExport;

/// <summary>
/// The generic, reflection-driven Excel import/export engine.
///
/// <para>Ported from legacy XAF <c>HCM10.Web.UserControls.ImportExport.uscBulkImportExport.ascx.cs</c>
/// (1,309 lines). That one control offered four actions -- <i>Validate Data</i>, <i>Upload Data</i>,
/// <i>Download Template</i>, <i>Download Data</i> -- against <b>any</b> business-object type in the system,
/// which is why this is a single generic engine rather than one importer per entity.</para>
///
/// <para><b>What maps to what:</b>
/// <list type="bullet">
/// <item><c>BindModuleName()</c> -&gt; <see cref="GetImportableEntities"/></item>
/// <item><c>DownloadGrid(false)</c> / <c>btnDownloadTemplate_Click</c> -&gt; <see cref="BuildWorkbook"/> with
/// <c>withData: false</c></item>
/// <item><c>DownloadGrid(true)</c> / <c>btnDownloadData_Click</c> -&gt; <see cref="BuildWorkbook"/> with
/// <c>withData: true</c></item>
/// <item><c>Main(args)</c> + <c>UploadData(...)</c> + <c>SaveLog(...)</c> -&gt; <see cref="ImportAsync"/></item>
/// <item><c>btnStopUploadingFile</c> -&gt; <see cref="RequestStopAsync"/></item>
/// <item><c>btnClearStatus</c> / <c>TruncateLog()</c> -&gt; <see cref="ClearLogAsync"/></item>
/// </list></para>
///
/// <para><b>Deliberately NOT ported:</b> the whole DevExpress presentation half of the control --
/// <c>Page_Load</c>'s Arabic/English label swapping, <c>HideShowControl()</c>, <c>FormatColumnValue()</c>,
/// <c>ASPxGridView1_DataBound</c>, <c>gridExport_RenderBrick</c>, the <c>ASPxCallbackPanel*_Callback</c>
/// timer-polling handlers and <c>ChangeLabelStatus()</c>. HCMv3 is an Angular SPA; localisation is XML-driven
/// and progress is polled through <c>IBulkImportExportAppService.GetBulkImportStatus</c>.</para>
/// </summary>
public interface IBulkImportExportService
{
    /// <summary>
    /// Every entity type the engine can import/export, ordered by name.
    /// Legacy <c>BindModuleName()</c>: "every type in an HCM10* assembly whose BaseType is MyBaseObject".
    /// </summary>
    IReadOnlyList<BulkEntityDescriptor> GetImportableEntities();

    /// <summary>
    /// Column list for one entity -- the header row of the template. Throws if the type is unknown.
    /// </summary>
    BulkEntityDescriptor GetEntityDescriptor(string entityFullName);

    /// <summary>
    /// Builds the <c>.xlsx</c> for the "Download Template" (<paramref name="withData"/> = false) and
    /// "Download Data" (<paramref name="withData"/> = true) actions.
    ///
    /// <para><b>DEVIATION.</b> Legacy <c>GetDataToExport</c> hand-built a raw SQL <c>SELECT</c> with a
    /// <c>LEFT JOIN</c> per reference column and appended <c>WHERE 1 = 0</c> for the template case, then
    /// pushed it through an <c>ASPxGridView</c> exporter. Raw SQL from a domain service would violate this
    /// repo's "no DbContext outside EntityFrameworkCore" rule, so the same result is produced with
    /// reflection + <c>IRepository&lt;T&gt;.GetAll()</c> + ClosedXML. The emitted column set, the ordering
    /// and the value formatting (<c>YES</c>/<c>NO</c> for bool, <c>HH:mm</c> for "*Time*" columns,
    /// enum names, and the referenced row's <c>Code</c>/<c>NameEn</c>/<c>Name</c> for reference columns)
    /// follow legacy exactly.</para>
    /// </summary>
    byte[] BuildWorkbook(string entityFullName, bool withData);

    /// <summary>
    /// Runs one import. Writes one <c>BulkUploadLog</c> row per spreadsheet row (legacy <c>SaveLog</c>) and
    /// one <c>ExecutionLog</c> summary row (HCMv3 convention -- legacy had no run-level record and
    /// recomputed the totals from <c>BulkUploadLog</c> on every UI timer tick).
    ///
    /// <para><b>Per-row isolation is preserved:</b> legacy created a fresh <c>IObjectSpace</c> per row so
    /// that a bad row could not poison its neighbours. Here each row runs in its own unit of work, so a
    /// failing row is rolled back on its own and the run continues -- i.e. <b>a batch is committed
    /// row-by-row, not atomically</b>. This is faithful to legacy and is surfaced to the operator through
    /// the Failed count and the per-row log.</para>
    /// </summary>
    Task<BulkImportRunResult> ImportAsync(BulkImportRunInput input);

    /// <summary>
    /// Legacy <c>btnStopUploadingFile</c>: sets <c>StopFlag</c> on the log so the running loop breaks at the
    /// next row boundary.
    /// </summary>
    Task RequestStopAsync();

    /// <summary>
    /// Legacy <c>btnClearStatus</c> -&gt; <c>TruncateLog()</c> (<c>TRUNCATE TABLE [dbo].[BulkUploadLog]</c>).
    /// Deletes through the repository instead, so it is tenant-scoped and soft-delete aware.
    /// </summary>
    Task ClearLogAsync();
}
