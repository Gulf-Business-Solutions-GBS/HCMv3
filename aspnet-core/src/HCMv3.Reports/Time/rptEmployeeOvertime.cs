using System;

namespace HCMv3.Reports.Time
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Time.rptEmployeeOvertime</c> -- approved overtime hours
    /// and the resulting pay, either for a month or for a single day. FINANCIAL OUTPUT.
    ///
    /// Same XAF removals as <see cref="rptEmployeeAttendance"/>: the two
    /// <c>SecuritySystem.CurrentUserName</c> / <c>UserNameLogin</c> fields were dead here too and are
    /// removed rather than replaced.
    ///
    /// SEE the overtime-formula and NULL-propagation notes on
    /// <see cref="TimeSql.EmployeeOvertimeQuery"/> before touching the arithmetic.
    /// </summary>
    public partial class rptEmployeeOvertime : HcmReportBase
    {
        public const string ReportName = "EmployeeOvertime";

        public rptEmployeeOvertime()
        {
            InitializeComponent();

            PrmAsOnDate.Value = DateTime.Now.Date;
            PrmEmpCode.Value = string.Empty;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;

            EnsureHcmParameters();
        }
    }
}
