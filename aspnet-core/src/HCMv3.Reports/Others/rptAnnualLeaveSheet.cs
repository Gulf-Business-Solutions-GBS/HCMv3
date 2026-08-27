using System;
using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptAnnualLeaveSheet</c>.
    ///
    /// The annual-leave ledger for one employee up to an as-on date. Despite the name this report
    /// reads leave BALANCES and ACCRUALS only (TimeTransfers / TimeRegisters / EmployeeAbsences /
    /// EmployeeAbsAdjustments / EmployeeAbsQuotaForfeits) -- it does NOT touch the legacy
    /// self-service <c>LeaveRequest</c> object, so it is portable. See PORTING-NOTES-OthersLeave.md
    /// for the two reports in this batch that are not.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                       | Replacement |
    /// |--------------------------------------------------------------|-------------|
    /// | <c>SecuritySystem.CurrentUserName</c> read in a field initialiser | the caller pushes the name in through <see cref="HcmReportBase.ApplyLoggedUserName"/>, which sets the <c>PrmLoggedUserName</c> parameter added by <c>EnsureHcmParameters(true)</c>. |
    /// | <c>new XPObjectSpaceProvider(ConfigurationManager.ConnectionStrings["HCM10ConnectionString"])</c> + <c>GetObjectsQuery&lt;PermissionPolicyUser&gt;()</c> / <c>GetObjectsQuery&lt;Employee&gt;()</c> in the constructor, to turn the login name into the employee's <c>FullNameEn</c> | removed. A DevExpress-constructed report has no DI container and cannot resolve ABP repositories or an <c>AbpSession</c>; identity is an application-layer concern. The footer now prints whatever the caller supplied. |
    /// | <c>HCM10.Module.BusinessObjects.*</c> imports | gone; the report touches no entities. |
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptAnnualLeaveSheet : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "AnnualLeaveSheet";

        public rptAnnualLeaveSheet()
        {
            InitializeComponent();

            PrmAsOnDate.Value = DateTime.Now.Date;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. "true" also adds PrmLoggedUserName, which the page footer prints.
            EnsureHcmParameters(withLoggedUserName: true);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptAnnualLeaveSheet(string employeeCode, DateTime asOnDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmAsOnDate.Value = asOnDate;
        }

        /// <summary>
        /// Stamps the page-footer "printed by" label.
        ///
        /// Legacy body: <c>lblUserlogin.Text = UserNameLogin != "" ? UserNameLogin : CurrentUserName;</c>
        /// where both came from the XPO round-trip described in the class summary. The same
        /// precedence survives -- the caller-supplied name if there is one -- but the fallback is now
        /// an empty label rather than the XAF login name, which this layer cannot see.
        ///
        /// v17.2 -> v23.2: report BeforePrint is
        /// <c>BeforePrintEventHandler(CancelEventArgs)</c>, not <c>PrintEventHandler(PrintEventArgs)</c>.
        /// </summary>
        private void rptAnnualLeaveSheet_BeforePrint(object sender, CancelEventArgs e)
        {
            var loggedUser = Parameters[LoggedUserNameParameter] == null
                ? string.Empty
                : Convert.ToString(Parameters[LoggedUserNameParameter].Value);

            lblUserlogin.Text = loggedUser ?? string.Empty;
        }
    }
}
