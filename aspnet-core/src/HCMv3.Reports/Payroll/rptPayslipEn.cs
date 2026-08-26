using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Payroll.rptPayslipEn</c>.
    ///
    /// XAF CONSTRUCTS REPLACED (the legacy code-behind was 193 lines; see the class remarks in
    /// HcmReportBase for the base class):
    ///
    /// | Legacy                                                    | Replacement |
    /// |-----------------------------------------------------------|-------------|
    /// | <c>SecuritySystem.CurrentUserName</c> baked into <c>LoggedUserNme.Text</c> in the ctor | <c>PrmLoggedUserName</c> report parameter, expression-bound to the label. Baking it in the ctor is why the legacy exported .repx carries the *exporting* developer's user name as static text. |
    /// | <c>new XPObjectSpaceProvider(ConfigurationManager.ConnectionStrings["HCM10ConnectionString"])</c> + <c>CreateObjectSpace()</c> | removed. The connection is pushed in by the caller through <see cref="HcmReportBase.ApplyConnectionString"/>, which sources it from HCMv3's <c>ConnectionStrings:Default</c>. |
    /// | <c>object_Space.GetObjectsQuery&lt;PermissionPolicyUser&gt;()</c> / <c>GetObjectsQuery&lt;Employee&gt;()</c> lookups | removed from the report. A report constructed by DevExpress has no DI container; the caller (the ReportStorageWebExtension in Web.Host) resolves ABP's <c>IAbpSession</c> / user + employee repositories and passes the results in as parameters. |
    /// | <c>SysService.IsAdminRole(...)</c> + <c>RLSService.GetRLSEmployeeCodeList(...)</c> driving <c>PrmEmpCode.Visible</c> | NOT ported into the report. Whether a user may pick an arbitrary employee code is an authorization decision and belongs behind ABP permissions in the calling layer -- baking it into report layout means a designer edit can revoke row-level security. |
    /// | <c>SysService.GetPayrollSettings(...).PayslipSignature</c> driving <c>PanelSignature.Visible</c> | STUBBED. See <see cref="ApplyPayslipSignature"/>: the panel stays visible until the caller says otherwise, because HCMv3 has no ported <c>ManagePayrollSettings.PayslipSignature</c> setting yet. |
    /// | <c>AfterPrint</c>: export to PDF + <c>SmtpClient.Send</c> + <c>BodyTemplateHtml</c> | removed. Already ported to <c>HCMv3.Application</c> (<c>ReportsAppService.SendPayslipEmails</c> / <c>BuildPayslipEmailBodyHtml</c>). Mailing from AfterPrint would fire on every Document Viewer page request. |
    /// | <c>HCM10.Module.BusinessObjects.*</c> imports | gone; the report touches no entities at all, only SQL and parameters. |
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptPayslipEn : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayslipEn";

        public rptPayslipEn()
        {
            InitializeComponent();

            // Legacy defaulted the period to "last month" in the ctor. Kept: it is a harmless
            // design-time convenience and every real caller overwrites it.
            PrmEmpCode.Value = string.Empty;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;

            // The legacy report always mailed itself when PrmSendMail was set. That path is gone
            // (see the class remarks); pin the parameter off and hide it so no one re-enables a
            // control that now does nothing.
            PrmSendMail.Value = false;
            PrmSendMail.Visible = false;

            EnsureHcmParameters(withLoggedUserName: true);

            // Replaces "LoggedUserNme.Text = SecuritySystem.CurrentUserName" in the constructor.
            LoggedUserNme.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        /// <summary>
        /// Convenience overload mirroring the legacy 7-argument constructor that the payroll run
        /// used. Note it takes no <c>sendMail</c> flag and no XPO session -- mailing is the app
        /// service's job now.
        /// </summary>
        public rptPayslipEn(string employeeCode, string payrollArea, int runType, int month, int year, int employeeStatus)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmPayrollArea.Value = payrollArea ?? string.Empty;
            PrmPayrollRunType.Value = runType;
            PrmMonth.Value = month;
            PrmYear.Value = year;
            PrmEmpStatus.Value = employeeStatus;
        }

        /// <summary>
        /// STUB for the legacy <c>PanelSignature.Visible = GetPayrollSettings(...).PayslipSignature</c>.
        ///
        /// HCMv3 has no ported equivalent of the legacy <c>ManagePayrollSettings.PayslipSignature</c>
        /// flag, so the panel is left visible (the legacy default for tenants that had the setting
        /// on) and the decision is exposed to the caller. Wire this to the real setting when it is
        /// ported; until then the Approved-By / Reviewed-By block always prints.
        /// </summary>
        public void ApplyPayslipSignature(bool visible)
        {
            PanelSignature.Visible = visible;
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs).
        private void rptPayslipEn_1_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // The legacy body was entirely commented out (subreport visibility toggling that was
            // superseded by the designer-time Visible flags). Nothing to port; the handler is kept
            // because the designer file wires it.
        }
    }
}
