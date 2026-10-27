using System;

namespace HCMv3.Administration.Tools.Dto;

/// <summary>
/// Input for the legacy uscSystemConfigPanel "Fetch Time Attendance Missing Data"
/// (<c>ASPxCallbackPanelMissingData_Callback</c>, line 844) and "Sync Time Attendance Missing API Data"
/// (<c>ASPxCallbackPanelMissingAPIData_Callback</c>, line 899) panels.
/// </summary>
public class FetchMissingTimeAttendanceDataInput
{
    /// <summary>
    /// Legacy <c>dtTransFromDate</c> / <c>dtAPITransFromDate</c>. Clamped up to
    /// <c>ManageTimeSettings.EarliestEvaluationDate</c> when unset or earlier (legacy lines 871-873 /
    /// 926-928). The "to" date is always "now", as in legacy.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Legacy <c>txtEmpListForMissingData</c> / <c>txtEmpListForMissingDataAPI</c>. Empty means "all active
    /// employees that have an AccessID".
    /// </summary>
    public string[] EmployeeCodes { get; set; }

    /// <summary>
    /// Connection string to the external access-control database. Optional: when blank, the one saved in the
    /// Integration Center (Access-control database card) is used. Legacy read it from <c>web.config</c>.
    /// </summary>
    public string AccessControlConnectionString { get; set; }
}
