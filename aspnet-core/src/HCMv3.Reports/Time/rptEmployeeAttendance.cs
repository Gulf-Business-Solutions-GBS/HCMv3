using System;

namespace HCMv3.Reports.Time
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Time.rptEmployeeAttendance</c> -- the daily punch sheet
    /// for one payroll area over a date range.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy | Replacement |
    /// |---|---|
    /// | <c>string CurrentUserName = SecuritySystem.CurrentUserName.ToString();</c> and <c>string UserNameLogin = ""</c> | REMOVED, not replaced. Both were dead: this report's layout has no user-login label and neither field is read anywhere in the class. The field initialiser alone was enough to make the report un-constructible outside XAF. |
    /// | <c>HCM10.Module.BusinessObjects.*</c> / <c>DevExpress.ExpressApp.*</c> imports | gone. |
    ///
    /// SEE ALSO the sentinel-date note at the top of <see cref="TimeSql"/>: HCMv3 stores an unset
    /// punch as 0001-01-01, not the legacy 1800-01-01, and the SQL had to be rebound for it.
    ///
    /// PARAMETER NOTE: <c>PrmEmpCode</c> is a COMMA-SEPARATED LIST of employee codes here, not one
    /// code, and <c>PrmPayrollArea</c> is MANDATORY -- leaving it blank returns nothing by design.
    /// </summary>
    public partial class rptEmployeeAttendance : HcmReportBase
    {
        public const string ReportName = "EmployeeAttendance";

        public rptEmployeeAttendance()
        {
            InitializeComponent();

            PrmPrintDate.Value = DateTime.Now.Date;
            PrmEmpCode.Value = string.Empty;

            // Legacy defaulted the range to the current calendar month.
            PrmFromDate.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            PrmToDate.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month,
                DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));

            EnsureHcmParameters();
        }
    }
}
