using System;
using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptMonthlyIndemnityAccrual</c>.
    ///
    /// One row per payroll period showing the indemnity (end-of-service gratuity) quota movement,
    /// read off the salary register wage types 4004 / 4005 / 4025 / 4006. Accrual data only -- it
    /// does not touch the legacy self-service <c>LeaveRequest</c> object, so it is portable.
    ///
    /// XAF CONSTRUCTS REPLACED: identical to <see cref="rptAnnualLeaveSheet"/> -- the constructor's
    /// <c>XPObjectSpaceProvider</c> round-trip that turned <c>SecuritySystem.CurrentUserName</c>
    /// into the employee's <c>FullNameEn</c> is gone; the name now arrives through
    /// <see cref="HcmReportBase.ApplyLoggedUserName"/>.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptMonthlyIndemnityAccrual : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "MonthlyIndemnityAccrual";

        public rptMonthlyIndemnityAccrual()
        {
            InitializeComponent();

            EmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. "true" also adds PrmLoggedUserName, which the page footer prints.
            EnsureHcmParameters(withLoggedUserName: true);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptMonthlyIndemnityAccrual(string employeeCode)
            : this()
        {
            EmpCode.Value = employeeCode ?? string.Empty;
        }

        /// <summary>
        /// Stamps the page-footer "printed by" label. See rptAnnualLeaveSheet's BeforePrint handler
        /// for the full rationale; the legacy body here is byte-identical.
        /// </summary>
        private void rptMonthlyIndemnityAccrual_BeforePrint(object sender, CancelEventArgs e)
        {
            var loggedUser = Parameters[LoggedUserNameParameter] == null
                ? string.Empty
                : Convert.ToString(Parameters[LoggedUserNameParameter].Value);

            lblUserlogin.Text = loggedUser ?? string.Empty;
        }
    }
}
