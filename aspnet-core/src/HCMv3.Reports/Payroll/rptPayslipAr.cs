using System;
using DevExpress.DataAccess.Sql;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Payroll.rptPayslipAr</c> -- the
    /// Arabic payslip. Sibling of <see cref="rptPayslipEn"/>; read that class's remarks too, since
    /// the two reports share most of their disposition decisions.
    ///
    /// The legacy code-behind was 411 lines, of which roughly 340 were three near-identical
    /// constructors doing the same XAF work:
    ///
    /// | Legacy                                                    | Replacement |
    /// |-----------------------------------------------------------|-------------|
    /// | <c>SecuritySystem.CurrentUserName</c> assigned to <c>LoggedUserNme.Text</c> (twice, in every constructor) | <c>PrmLoggedUserName</c> report parameter, expression-bound to the label -- same fix as rptPayslipEn. Assigning it in the constructor is why an exported layout carries the exporting developer's user name as static text. |
    /// | <c>new XPObjectSpaceProvider(ConfigurationManager.ConnectionStrings["HCM10ConnectionString"])</c> in all three constructors | removed. The connection is pushed in by the caller through <see cref="HcmReportBase.ApplyConnectionString"/>. |
    /// | <c>SysService.IsAdminRole(...)</c> deciding whether <c>PrmEmployeeCode</c> is visible, and pinning it to the caller's own employee otherwise | NOT ported. Whether a user may pick an arbitrary employee code is an authorization decision and belongs behind ABP permissions in the calling layer -- baking it into report layout means a designer edit can revoke row-level security. Identical call as rptPayslipEn. |
    /// | <c>DataSourceDemanded</c> handler that CONCATENATED the selected employee codes into the SQL text (<c>query.Sql = Sqlquery + " where emp.Code In(" + ... + ")"</c>) and then called <c>RebuildResultSchema()</c> | removed. The employee code is now a real query parameter (<c>@PrmEmpCode</c>). Besides being a SQL-injection vector, the handler mutated <c>PayslipDS.Queries[0]</c> -- which is <c>PayslipQuery</c>, not the <c>PayslipOrgV2Query</c> the report actually binds -- so it never did what it looked like it did. |
    /// | <c>BeforePrint</c>: swapped in a second FilterString and re-added subreport ParameterBindings for the "all employees in a payroll area" mode | removed. Both modes are now one SQL predicate, exactly as in rptPayslipEn: an employee code if one is given, otherwise the payroll area. |
    /// | <c>AfterPrint</c>: <c>ExportToPdf</c> into a MemoryStream + <c>SmtpClient.Send</c>, wrapped in a catch-everything | removed. Mailing is <c>ReportsAppService.SendPayslipEmails</c>'s job; from AfterPrint it would fire on every Document Viewer page request. <c>PrmSendMail</c> is pinned off and hidden below so nobody re-enables a control that no longer does anything. |
    /// | <c>FontCollection1</c> / <c>FontCollection2</c>: static <c>PrivateFontCollection</c>s loading BFantezy.ttf and BNaznnBd.ttf via <c>HttpContext.Current.Server.MapPath</c> | NOT ported, and NOT needed. Every line that consumed them was already commented out in the legacy source, and the layout uses only Arial, Tahoma and Calibri. This is the same dead private-font-collection code that makes <c>rptSubordPayslipAr</c> fail to construct in the legacy exporter; here it is simply unreachable. See the Arabic notes below. |
    ///
    /// ARABIC / RTL -- what this first Arabic payslip port actually needed:
    ///   * NO custom or embedded fonts. The layout uses Arial (46 controls), Tahoma (25) and Calibri
    ///     (1), all of which ship with Windows and cover Arabic. Nothing is missing and nothing has
    ///     been faked.
    ///   * NO <c>RightToLeft</c> / <c>RightToLeftLayout</c> flags anywhere in the layout. The Arabic
    ///     reading order is achieved with right-aligned labels and literal Arabic strings, which
    ///     survive the port untouched.
    ///   * <c>AmountInArabicWords</c> is NOT needed. It is not deliberately omitted here -- this
    ///     report never called it. (<c>HcmReportBase</c> still does not carry it; the first report
    ///     that does call it will have to port it along with the legacy CurrencyInfo helpers.)
    ///   * The four Arabic month-name expressions moved out of the .resx into
    ///     <see cref="PayslipArSql"/>; one of them (<c>CalcArMonth</c>) carries a legacy typo that is
    ///     preserved deliberately -- see the comment there.
    ///   * ORG UNIT: AbpOrganizationUnits has only <c>DisplayName</c>, so the Arabic payslip prints
    ///     the same org-unit string as the English one. *** ARABIC TEXT LOST -- schema gap, not a
    ///     layout gap. ***
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>, on the master and on all five subreports.
    /// Call <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report
    /// returns nothing.
    /// </summary>
    public partial class rptPayslipAr : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayslipAr";

        public rptPayslipAr()
        {
            InitializeComponent();

            PrmEmployeeCode.Value = string.Empty;
            PrmforPayroll.Value = false;
            PrmforPayroll.Visible = false;

            // The legacy report always mailed itself when PrmSendMail was set. That path is gone
            // (see the class remarks); pin the parameter off and hide it.
            PrmSendMail.Value = false;
            PrmSendMail.Visible = false;

            EnsureHcmParameters(withLoggedUserName: true);

            // Replaces "this.LoggedUserNme.Text = SecuritySystem.CurrentUserName" in the constructor.
            LoggedUserNme.ExpressionBindings.Add(
                new DevExpress.XtraReports.UI.ExpressionBinding(
                    "BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        /// <summary>
        /// Convenience overload mirroring the legacy constructors the payroll run used. Note what it
        /// does NOT take: no XPO session, no <c>sendMail</c> flag, and no <c>forPayroll</c> flag --
        /// mailing belongs to the app service, and "for payroll" only ever selected between two
        /// client-side filter strings that no longer exist.
        ///
        /// The legacy overloads took every argument as a <c>string</c> and parsed inside
        /// (<c>Status.Trim() == "Active" ? "1" : "0"</c>); the types are made explicit here.
        /// </summary>
        public rptPayslipAr(string employeeCode, string payrollArea, int runType, int month, int year, int employeeStatus)
            : this()
        {
            PrmEmployeeCode.Value = employeeCode ?? string.Empty;
            PrmPayrollArea.Value = payrollArea ?? string.Empty;
            PrmPayrollRunType.Value = runType;
            PrmPeriodMonth.Value = month;
            PrmPeriodYear.Value = year;
            PrmempStatus.Value = employeeStatus;
        }

        /// <summary>
        /// The six query parameters shared by the two master queries (<c>PayslipOrgV2Query</c>, which
        /// the report binds, and <c>PayslipQuery</c>, which feeds the designation label).
        ///
        /// Built in code rather than spelled out twice in the designer file because a
        /// <see cref="QueryParameter"/> instance cannot be shared between two queries -- each query
        /// needs its own -- and twelve near-identical designer blocks would be twelve places to get
        /// the tenant plumbing subtly wrong.
        ///
        /// <c>@TenantId</c> is deliberately NOT in this list: it is added to every query whose SQL
        /// mentions it by <see cref="HcmReportBase.EnsureHcmParameters"/>, which runs after
        /// InitializeComponent and therefore survives the Visual Studio designer re-serialising this
        /// method wholesale.
        /// </summary>
        private static QueryParameter[] BuildMasterQueryParameters()
        {
            return new[]
            {
                Parameter("PrmEmpCode", "PrmEmployeeCode", typeof(string)),
                Parameter("PrmPayrollArea", "PrmPayrollArea", typeof(string)),
                Parameter("PrmMonth", "PrmPeriodMonth", typeof(int)),
                Parameter("PrmYear", "PrmPeriodYear", typeof(int)),
                Parameter("PrmPayrollRunType", "PrmPayrollRunType", typeof(int)),
                Parameter("PrmEmployeeStatus", "PrmempStatus", typeof(int))
            };
        }

        private static QueryParameter Parameter(string sqlParameterName, string reportParameterName, Type type)
        {
            return new QueryParameter
            {
                Name = sqlParameterName,
                Type = typeof(DevExpress.DataAccess.Expression),
                Value = new DevExpress.DataAccess.Expression("[Parameters." + reportParameterName + "]", type)
            };
        }
    }
}
