using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.ImportExport;

/// <summary>One image file offered to the profile-photo importer.</summary>
public class ProfilePhotoFile
{
    /// <summary>
    /// File name including extension. The employee is matched on the file name WITHOUT its extension,
    /// exactly as legacy did (<c>Path.GetFileNameWithoutExtension(filename)</c> -&gt; <c>Employee.Code</c>).
    /// </summary>
    public string FileName { get; set; }

    public byte[] Content { get; set; }
}

/// <summary>Arguments for one profile-photo import run.</summary>
public class ProfilePhotoImportInput
{
    public List<ProfilePhotoFile> Files { get; set; } = new List<ProfilePhotoFile>();

    /// <summary>See <c>BulkImportRunInput.TenantId</c> -- background jobs start with no tenant scope.</summary>
    public int TenantId { get; set; }
}

/// <summary>
/// Bulk import of employee profile photos.
///
/// <para>Ported from legacy XAF <c>HCM10.Web.UserControls.ImportExport.uscBulkImport.ascx.cs</c> (595 lines).
/// Despite the name, that control is <b>not</b> a variant of <c>uscBulkImportExport</c>: its
/// <c>UploadData()</c> does exactly one thing -- walk a server folder, match every
/// <c>{EmployeeCode}.jpg|jpeg|png|gif</c> file to an <c>Employee</c> by <c>Code</c>, and store the bytes in
/// <c>Employee.Photo</c>. The two controls share only their copy-pasted progress/log UI shell
/// (<c>CalculateProcessStatus</c>, <c>TruncateLog</c>, the <c>ASPxCallbackPanel*</c> handlers and the
/// success/failure grids), which is presentation and does not port.</para>
///
/// <para><b>DEVIATION -- input source.</b> Legacy read <c>txtFolderPath</c>, a path on the web server, via
/// <c>Directory.GetFiles</c>. An Angular SPA cannot hand the API a server-side folder, and this repo's
/// convention is base64-in-DTO payloads with no server-side file writes, so the caller uploads the images
/// instead. The per-file processing, ordering (by file name), validation and logging are otherwise
/// faithful.</para>
///
/// <para><b>NOT PORTED -- <c>Employee.IsSystemGenerated</c>.</b> Legacy set <c>emp.IsSystemGenerated = true</c>
/// and stamped <c>emp.SysModifier = SysService.userSysDataAdmin</c> / <c>emp.SysModifiedOn = DateTime.Now</c>
/// (<c>uscBulkImport.ascx.cs</c> lines 195-200). HCMv3's <c>Employee</c> has no <c>IsSystemGenerated</c>
/// property and the Sys* audit stamps are superseded by <c>FullAuditedEntity</c>, which ABP fills in
/// automatically. Nothing is invented to stand in for the flag.</para>
///
/// <para><b>NOT PORTED -- <c>BindPayrollArea()</c>.</b> Legacy populated a Payroll Area combo
/// (<c>cmbPayrollArea</c>) and an employee-code textbox on this screen; neither was ever read by
/// <c>ExecuteLongOperation</c> / <c>UploadData</c> -- the only input actually used is the folder path (the
/// validation that referenced them is commented out at lines 89-90). Dead UI, not ported.</para>
/// </summary>
public interface IProfilePhotoImportService
{
    /// <summary>
    /// Legacy <c>uscBulkImport.UploadData(objectSpace, filefolder)</c>. Writes one
    /// <c>BulkUploadLog</c> row per image (<c>LogType = ProfilePhoto</c>) and one <c>ExecutionLog</c>
    /// summary row, and honours the same <c>StopFlag</c> abort handshake as the generic engine.
    /// </summary>
    Task<BulkImportRunResult> ImportAsync(ProfilePhotoImportInput input);

    /// <summary>
    /// Legacy allowed <c>.jpg</c>, <c>.jepg</c> (sic -- the original regex is
    /// <c>@".jpg|.jepg|.png|.gif$"</c>, a typo for "jpeg"), <c>.png</c> and <c>.gif</c>. Both the typo'd and
    /// the correct spelling are accepted here so a correctly-named <c>.jpeg</c> file is not silently
    /// skipped, which is what legacy did.
    /// </summary>
    bool IsSupportedImage(string fileName);
}
