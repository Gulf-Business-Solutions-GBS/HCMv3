using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.ImportExport.Dto;

namespace HCMv3.ImportExport;

/// <summary>
/// API surface of the bulk import/export screens, ported from
/// <c>C:\...\HCM10.Web\UserControls\ImportExport\</c>.
///
/// <para><b>Investigation summary for that folder (four controls):</b>
/// <list type="number">
/// <item><c>uscBulkImportExport.ascx.cs</c> (1,309 lines) -- the real capability: a generic,
/// reflection-driven Excel import/export for ANY business object. Everything on this interface except the
/// last two methods comes from it.</item>
/// <item><c>uscBulkImport.ascx.cs</c> (595 lines) -- NOT a clone despite the name: a profile-photo importer.
/// It shares only the copy-pasted progress/log UI shell. -&gt; <see cref="StartProfilePhotoImport"/>.</item>
/// <item><c>uscImportExport.ascx.cs</c> (922 lines) -- DEAD CODE. Roughly 860 of its 922 lines are inside
/// comment blocks; the only live members are empty <c>Setup</c>/<c>Refresh</c>/<c>OnLoad</c>/<c>OnInit</c>
/// stubs. The control does nothing at runtime. NOTHING IS PORTED FROM IT and no method here corresponds to
/// it.</item>
/// <item><c>uscImportTimesheet.ascx.cs</c> (285 lines) -- a <c>TimeEvent</c> punch importer whose row loop
/// is the same algorithm over the same fixed columns as legacy
/// <c>TimeAttendanceIntegrationService.ImportTransactions()</c>, which this port already carried over. Only
/// the upload entry point was missing. -&gt; <see cref="ImportTimesheet"/>, which delegates to the existing
/// <c>ITimeAttendanceIntegrationService</c>.</item>
/// </list></para>
///
/// <para><b>File formats:</b> <c>.xlsx</c> only, via ClosedXML. Legacy read uploads with the
/// <c>Microsoft.ACE.OLEDB.12.0</c> provider (<c>uscBulkImportExport</c>) or DevExpress
/// <c>Spreadsheet.Workbook</c> (<c>uscImportTimesheet</c>) and wrote exports through
/// <c>ASPxGridViewExporter.WriteXlsToResponse()</c>. No CSV support existed and none is added.</para>
/// </summary>
public interface IBulkImportExportAppService : IApplicationService
{
    /// <summary>
    /// Every entity the engine can round-trip, with its template columns.
    /// Legacy <c>BindModuleName()</c> (which bound <c>cmbModule</c>).
    /// </summary>
    Task<GetBulkImportEntitiesOutput> GetBulkImportEntities();

    /// <summary>
    /// "Download Template" (<c>WithData = false</c>) and "Download Data" (<c>WithData = true</c>).
    /// Legacy <c>btnDownloadTemplate_Click</c> / <c>btnDownloadData_Click</c> -&gt; <c>DownloadGrid(bool)</c>.
    /// </summary>
    Task<BulkExcelOutput> DownloadBulkExcel(BulkExportInput input);

    /// <summary>
    /// "Validate Data" and "Upload Data". Queues a background job and returns immediately -- legacy did the
    /// same thing with <c>ThreadPool.QueueUserWorkItem(ExecuteLongOperation)</c> and a UI polling timer,
    /// because a run touches every row of an arbitrarily large spreadsheet.
    ///
    /// <para>Clears the previous run's per-row log first, exactly as legacy's <c>TruncateLog()</c> call in
    /// <c>ASPxCallbackPanel1_Callback</c> did before queueing.</para>
    /// </summary>
    Task<StartBulkImportOutput> StartBulkImport(StartBulkImportInput input);

    /// <summary>Legacy <c>btnStopUploadingFile</c>. Takes effect at the next row boundary.</summary>
    Task StopBulkImport();

    /// <summary>Legacy <c>btnClearStatus</c> -&gt; <c>TruncateLog()</c>.</summary>
    Task ClearBulkImportStatus();

    /// <summary>
    /// Progress poll. Legacy <c>ASPxCallbackPanel_Callback</c> -&gt; <c>CalculateProcessStatus()</c>,
    /// which ran on a UI timer. No input DTO, so no <c>[HttpPost]</c> is needed.
    /// </summary>
    Task<BulkImportStatusOutput> GetBulkImportStatus();

    /// <summary>
    /// The per-row log grid. Legacy <c>ASPxCallbackPanelGrid_Callback</c>'s "Show Details" buttons
    /// (<c>btnSuccess</c>/<c>btnFailed</c>/<c>btnAll</c>).
    /// </summary>
    Task<PagedResultDto<BulkUploadLogEntryDto>> GetBulkUploadLogEntries(GetBulkUploadLogEntriesInput input);

    /// <summary>
    /// Profile-photo bulk import. Legacy <c>uscBulkImport.btnStartUpload</c>. Queued, for the same reason
    /// as <see cref="StartBulkImport"/>.
    /// </summary>
    Task<StartBulkImportOutput> StartProfilePhotoImport(StartProfilePhotoImportInput input);

    /// <summary>
    /// Timesheet punch import. Legacy <c>uscImportTimesheet.btnupload_Click</c>. Runs INLINE, not queued:
    /// legacy ran it inline too, and it is bounded by a single hand-produced device export (the legacy
    /// screen even carried a -- never called -- 500-row cap, <c>CheckNoOfRecordsCount</c>).
    /// </summary>
    Task<ImportTimesheetOutput> ImportTimesheet(ImportTimesheetInput input);
}
