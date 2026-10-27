using System;
using Abp.Application.Services.Dto;
using HCMv3.GridReports.Dto;

namespace HCMv3.GridReports.TimeReport.Dto;

/// <summary>
/// Filter input for the Late/Early Log With Photo analysis grid report (legacy
/// <c>HCM10.Web/UserControls/Reports/Analysis/TimeReport/uscRptLateEarlyLogWithPhoto.ascx</c>).
///
/// Same panel as the plain Late/Early Log: dtFrom/dtTo/cmbPayrollArea/txtEmpCode, with employee status
/// hard-coded to Active (uscRptLateEarlyLogWithPhoto.ascx.cs:635-636). The inherited
/// <c>EmployeeStatus</c> is therefore UNUSED and the Angular screen hides the field.
/// </summary>
public class GetLateEarlyLogWithPhotoReportInput : GridReportInputBase
{
    /// <summary>
    /// Legacy <c>cmbPayrollArea</c> (value = <c>PayrollArea.Code</c>). Null/empty means "all payroll
    /// areas". Legacy matched an employee having an org assignment covering <c>DateTime.Now.Date</c> --
    /// TODAY, not the report window (lines 638-644).
    /// </summary>
    public string PayrollAreaCode { get; set; }

    /// <summary>
    /// Legacy applied no sort: it filled the DataTable day-by-day then employee-by-employee (lines
    /// 762-764). Employee code then date is used here so server-side paging is deterministic.
    /// </summary>
    protected override string DefaultSorting => "EmployeeCode ASC, Date ASC";
}

/// <summary>
/// One grid row. Mirrors the legacy <c>DataTable</c> built in
/// <c>uscRptLateEarlyLogWithPhoto.ascx.cs:623-1340</c> column-for-column, in its declaration order
/// (lines 710-729): SrNo, GeoSelfie, Code, Employee, Date, Shift Start, Shift End, Sign In, Sign Out,
/// Early In, Early Out, Late In, Late Out, PunchInLocation, PunchOutLocation, Status, WeekDay,
/// Time Policy, WorkLocation, Country. This report has NO OrgLevel columns and no hours columns -- it is
/// an older, narrower sibling of <c>uscRptLateEarlyLog</c>, not a superset of it.
///
/// THE PHOTO -- READ THIS. The legacy "GeoSelfie" column is a rendered IMAGE, not a data value: the column
/// is declared as <c>System.Byte[]</c> (line 711) and filled by reading the punch event's
/// <c>GeoSelfiePath</c> off the web server's own file system (<c>File.Exists</c> +
/// <c>GetByteArray(te.GeoSelfiePath)</c>, lines 1071/1113/1247/1300) and handing the bytes to a DevExpress
/// image column. HCMv3 HAS NO EQUIVALENT SOURCE: <c>TimeEvent</c> was ported carrying only the raw
/// <c>GeoSelfiePath</c> string, and the legacy <c>Photo</c>/<c>SampleImage2</c> properties that resolved it
/// through <c>HttpContext.Current.Server.MapPath</c> were explicitly DEFERRED, pending a blob-storage
/// design (see the <c>TimeEvent</c> entity remarks). So this port surfaces
/// <see cref="GeoSelfiePath"/> -- the stored path string -- and NOT the image. Rendering an actual thumbnail
/// needs the deferred file-storage work first; it is called out here rather than dropped silently.
/// </summary>
public class LateEarlyLogWithPhotoReportRowDto : EntityDto<int>
{
    /// <summary>
    /// Legacy "GeoSelfie" column, reduced to the stored path -- see the class remarks. Legacy set this
    /// from the punch-IN event and then OVERWROTE it from the punch-OUT event when both existed, so on a
    /// complete day it is the punch-OUT selfie. Reproduced, including the last-writer-wins order.
    ///
    /// Null unless <c>ManageDocumentSettings.EnableDocSaveInFileSystem</c> is on: legacy's non-file-system
    /// branch is entirely commented out (lines 1067-1069 / 1218-1241), i.e. it wrote nothing at all.
    /// </summary>
    public string GeoSelfiePath { get; set; }

    /// <summary>Legacy "Code" column -- <c>emp.Code</c>.</summary>
    public string EmployeeCode { get; set; }

    /// <summary>Legacy "Employee" column -- FullNameAr when Arabic, else FullNameEn.</summary>
    public string EmployeeName { get; set; }

    /// <summary>
    /// Legacy "Date" column, which legacy wrote as a pre-formatted "dd MMMM yyyy" STRING into a string
    /// column (line 860). A real date here, formatted in the Angular grid, so it sorts correctly.
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Legacy "Shift Start". On the break-split rows this is the SUB-shift's start (line 1035). See the
    /// app service remarks on the STICKY SHIFT DEFECT for days with no resolvable Duty.
    /// </summary>
    public TimeSpan ShiftStart { get; set; }

    /// <summary>Legacy "Shift End". See <see cref="ShiftStart"/>.</summary>
    public TimeSpan ShiftEnd { get; set; }

    /// <summary>Legacy "Sign In" column -- the selected punch-in event's TransTime, null when none.</summary>
    public TimeSpan? SignIn { get; set; }

    /// <summary>Legacy "Sign Out" column -- the selected punch-out event's TransTime, null when none.</summary>
    public TimeSpan? SignOut { get; set; }

    /// <summary>Legacy "Early In" -- shift start minus punch-in, compared as TIME OF DAY only.</summary>
    public TimeSpan? EarlyIn { get; set; }

    /// <summary>Legacy "Early Out" -- shift end minus punch-out, compared as TIME OF DAY only.</summary>
    public TimeSpan? EarlyOut { get; set; }

    /// <summary>Legacy "Late In" -- punch-in minus shift start, compared as TIME OF DAY only.</summary>
    public TimeSpan? LateIn { get; set; }

    /// <summary>
    /// Legacy "Late Out". On the BREAK-SPLIT rows legacy tested the punch against the sub-shift's START
    /// time while subtracting its END time (line 1095) -- a defect that is reproduced; see the app service.
    /// </summary>
    public TimeSpan? LateOut { get; set; }

    /// <summary>Legacy "PunchInLocation" -- the punch-in event's WorkLocation.</summary>
    public string PunchInLocation { get; set; }

    /// <summary>
    /// Legacy "PunchOutLocation". Legacy picked the ENGLISH name in the Arabic aspect and the ARABIC name
    /// otherwise (lines 1103/1258) -- the language ternary is inverted on this one cell. Reproduced.
    /// </summary>
    public string PunchOutLocation { get; set; }

    /// <summary>
    /// Legacy "Status" column -- "Leave" / "No Return Date" / "Week Off" / "Public Holiday" (each with an
    /// Arabic twin), <c>"*"</c> when exactly one of the two punches is missing, else "". Note this report
    /// uses the bare asterisk, not the "Missed In"/"Missed Out" wording of <c>uscRptLateEarlyLog</c>.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Legacy "WeekDay" column -- the day name, localized to Arabic in the Arabic aspect through a
    /// hard-coded literal table (lines 809-815), not a resource file. Reproduced.
    /// </summary>
    public string WeekDay { get; set; }

    /// <summary>Legacy "Time Policy" column.</summary>
    public string TimePolicyName { get; set; }

    /// <summary>
    /// Legacy "WorkLocation" column -- <c>emp.CurrentWorkLocation</c>, i.e. the employee's work location
    /// as of TODAY, not as of the reported day (lines 878/1150/1259).
    /// </summary>
    public string WorkLocationName { get; set; }

    /// <summary>
    /// Legacy "Country" column -- the PayrollArea Country of the org assignment covering the report's FROM
    /// DATE (lines 884-895), not the reported day. Null, not empty string, when there is none.
    /// </summary>
    public string CountryName { get; set; }
}
