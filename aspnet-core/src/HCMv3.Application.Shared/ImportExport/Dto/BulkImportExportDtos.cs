using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;

namespace HCMv3.ImportExport.Dto;

/// <summary>One selectable entity in the "Entity :" picker (legacy <c>cmbModule</c>).</summary>
public class BulkImportEntityDto
{
    public string Name { get; set; }

    public string FullName { get; set; }

    /// <summary>Feature area, used only to group the picker. No legacy equivalent.</summary>
    public string Area { get; set; }

    /// <summary>The exact header row the template carries, in order.</summary>
    public List<string> Columns { get; set; } = new List<string>();
}

public class GetBulkImportEntitiesOutput
{
    public List<BulkImportEntityDto> Entities { get; set; } = new List<BulkImportEntityDto>();
}

/// <summary>
/// Input for the "Download Template" / "Download Data" actions (legacy <c>btnDownloadTemplate_Click</c> /
/// <c>btnDownloadData_Click</c>, which both routed into <c>DownloadGrid(bool WithData)</c>).
/// </summary>
public class BulkExportInput
{
    [Required]
    [StringLength(512)]
    public string EntityFullName { get; set; }

    /// <summary>Legacy <c>DownloadGrid</c>'s <c>WithData</c> parameter: false emits the header row only
    /// (legacy appended <c>WHERE 1 = 0</c> to its query).</summary>
    public bool WithData { get; set; }
}

/// <summary>
/// A generated <c>.xlsx</c>. Follows the same base64-in-DTO convention as
/// <c>HCMv3.Reports.Dto.ReportPdfOutput</c> -- ABP dynamic app-service actions serialize return values as
/// JSON, and this repo deliberately never writes generated files to disk server-side (legacy saved uploads
/// under <c>~/TempExcel</c> and streamed exports through <c>gridExport.WriteXlsToResponse()</c>).
/// </summary>
public class BulkExcelOutput
{
    public string FileName { get; set; }

    public string Base64Content { get; set; }
}

/// <summary>
/// Input for the "Validate Data" and "Upload Data" actions. Legacy fed the equivalent values into
/// <c>ExecuteLongOperation</c>'s <c>args[0..2]</c> plus the static <c>OnlyValidate</c> flag.
/// </summary>
public class StartBulkImportInput
{
    [Required]
    [StringLength(512)]
    public string EntityFullName { get; set; }

    [StringLength(260)]
    public string FileName { get; set; }

    /// <summary>Base64-encoded <c>.xlsx</c>. Never written to disk (see <see cref="BulkExcelOutput"/>).</summary>
    [Required]
    [DisableValidation]
    public string FileBase64 { get; set; }

    public BulkImportType ImportType { get; set; }

    /// <summary>Legacy <c>OnlyValidate</c>: true for the "Validate Data" action, false for "Upload Data".</summary>
    public bool ValidateOnly { get; set; }
}

public class StartBulkImportOutput
{
    /// <summary>Always true on success. The run is queued, never executed inline -- see
    /// <c>IBulkImportExportAppService.StartBulkImport</c>.</summary>
    public bool Queued { get; set; }

    /// <summary>Row count found in the workbook is NOT returned here: the workbook is parsed inside the
    /// background job. Poll <c>GetBulkImportStatus</c> instead (legacy did exactly this, on a UI timer).</summary>
    public string Message { get; set; }
}

/// <summary>
/// Everything legacy's <c>CalculateProcessStatus()</c> recomputed on every <c>timer1</c> tick, plus the
/// run-level status legacy inferred from its static <c>LongOperationStatus</c> field.
/// </summary>
public class BulkImportStatusOutput
{
    /// <summary>Legacy <c>txtSummaryAll</c>: total log rows written so far.</summary>
    public int SummaryAll { get; set; }

    /// <summary>Legacy <c>txtSummarySuccess</c>: log rows with <c>Status == 0</c>.</summary>
    public int SummarySuccess { get; set; }

    /// <summary>Legacy <c>txtSummaryFailed</c>.</summary>
    public int SummaryFailed { get; set; }

    /// <summary>Legacy <c>lblEmpProcessed</c>'s denominator: <c>BulkUploadLog.NoOfEmployee</c> of the last
    /// row -- i.e. the total row count the running import announced.</summary>
    public int TotalRecords { get; set; }

    /// <summary>Legacy <c>lblEmpProcessed</c>'s numerator.</summary>
    public int ProcessedRecords { get; set; }

    /// <summary>Legacy <c>lblEmpSpeed</c>: rows per minute.</summary>
    public double SpeedPerMinute { get; set; }

    /// <summary>Legacy <c>lblEstimateTime</c>, as raw minutes; the client formats it.</summary>
    public double RemainingMinutes { get; set; }

    /// <summary>True once a stop has been requested (legacy: any <c>BulkUploadLog.StopFlag</c> row).</summary>
    public bool StopRequested { get; set; }

    /// <summary>Timestamp of the most recent log row, or null when the log is empty.</summary>
    public DateTime? LastActivity { get; set; }

    /// <summary>
    /// Status of the most recent <c>ExecutionLog</c> row for this module, as its enum name
    /// (<c>INPRECESS</c>/<c>SUCCESS</c>/<c>FAILED</c>/<c>PARTIAL</c>/<c>ABORTED</c>), or null if no run has
    /// been recorded. Replaces legacy's in-memory static <c>LongOperationStatus</c> string, which was
    /// process-wide and did not survive a recycle.
    /// </summary>
    public string ExecutionStatus { get; set; }
}

/// <summary>
/// Input for the per-row log grid (legacy <c>ASPxCallbackPanelGrid_Callback</c>'s <c>btnSuccess</c> /
/// <c>btnFailed</c> / <c>btnAll</c> "Show Details" buttons, which each rebound the same grid with a
/// different <c>Status</c> filter).
/// </summary>
public class GetBulkUploadLogEntriesInput : PagedAndSortedResultRequestDto
{
    /// <summary>0 = successful only (legacy <c>btnSuccess</c>), 1 = failed only (legacy <c>btnFailed</c>),
    /// null = both (legacy <c>btnAll</c>).</summary>
    public int? Status { get; set; }
}

public class BulkUploadLogEntryDto : EntityDto
{
    /// <summary>Legacy grid column "RecordNo": the 1-based spreadsheet row this entry came from.</summary>
    public int RecordNo { get; set; }

    /// <summary>Legacy grid column "Run Date &amp; Time".</summary>
    public DateTime RunTimeStamp { get; set; }

    /// <summary>Legacy grid column "Message".</summary>
    public string Message { get; set; }

    /// <summary>0 = success, 1 = failure (legacy's raw int, kept as-is).</summary>
    public int Status { get; set; }

    /// <summary>Legacy grid columns "Emp Code"/"Employee" -- only ever populated by the profile-photo
    /// importer; the generic engine writes no employee.</summary>
    public string EmployeeCode { get; set; }

    public string EmployeeName { get; set; }
}

/// <summary>One uploaded image for the profile-photo importer (legacy <c>uscBulkImport</c>).</summary>
public class ProfilePhotoFileDto
{
    /// <summary>The employee is matched on this name minus its extension (legacy
    /// <c>Path.GetFileNameWithoutExtension</c> -&gt; <c>Employee.Code</c>).</summary>
    [Required]
    [StringLength(260)]
    public string FileName { get; set; }

    [Required]
    [DisableValidation]
    public string FileBase64 { get; set; }
}

public class StartProfilePhotoImportInput
{
    [Required]
    [DisableValidation]
    public List<ProfilePhotoFileDto> Files { get; set; } = new List<ProfilePhotoFileDto>();
}

/// <summary>Input for the timesheet (<c>TimeEvent</c>) spreadsheet upload -- legacy
/// <c>uscImportTimesheet</c>.</summary>
public class ImportTimesheetInput
{
    [StringLength(260)]
    public string FileName { get; set; }

    [Required]
    [DisableValidation]
    public string FileBase64 { get; set; }
}

public class ImportTimesheetOutput
{
    public int Inserted { get; set; }

    public int Skipped { get; set; }

    public int Failed { get; set; }

    public List<string> Errors { get; set; } = new List<string>();
}
