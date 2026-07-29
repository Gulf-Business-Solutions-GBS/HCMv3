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
    /// Connection string to the external access-control database.
    ///
    /// DEVIATION: legacy's <c>TimeAttendanceIntegrationService</c> read this from
    /// <c>web.config</c>. The ported <c>ITimeAttendanceIntegrationService</c> takes it as a caller-supplied
    /// parameter ("not stored anywhere in this app" — see its XML docs), and this port is forbidden from
    /// touching <c>appsettings.json</c>, so the operator supplies it on the screen. Only used by
    /// <c>FetchMissingTimeAttendanceData</c>; the API-based sync does not need it.
    /// </summary>
    public string AccessControlConnectionString { get; set; }
}
