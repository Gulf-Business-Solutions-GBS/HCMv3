using System.Collections.Generic;

namespace HCMv3.TimeManagement.Services;

/// <summary>
/// Outcome of one timesheet (<c>TimeEvent</c>) spreadsheet import.
///
/// <para>Legacy <c>uscImportTimesheet.btnupload_Click</c> reported only a single callback string --
/// "File X Uploading Process Completed." / "... Process Failed." -- and, on failure, threw an exception whose
/// message was the whole accumulated <c>errorLog</c>. This carries the same information in a form the
/// Angular client can display per row without losing the rows that DID import.</para>
/// </summary>
public class TimesheetImportResult
{
    /// <summary>New <c>TimeEvent</c> rows written.</summary>
    public int Inserted { get; set; }

    /// <summary>Rows deliberately ignored: unknown PIN/TransType, already-imported <c>RecordSource</c>, or
    /// an exact (Employee, TransType, TransDate, TransTime) duplicate. All three are legacy <c>continue</c>
    /// branches, not errors.</summary>
    public int Skipped { get; set; }

    /// <summary>Rows that threw while being parsed or written.</summary>
    public int Failed { get; set; }

    /// <summary>Human-readable per-row messages. Only populated for the upload path -- the folder-scanning
    /// path swallows them, as legacy did.</summary>
    public List<string> Errors { get; set; } = new List<string>();
}
