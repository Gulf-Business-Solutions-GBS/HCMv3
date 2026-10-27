using System;
using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptSalaryCertificateEn</c>.
    ///
    /// XAF CONSTRUCTS REPLACED (the legacy code-behind was 65 lines, of which the whole
    /// <c>BeforePrint</c> body was an XPO round-trip):
    ///
    /// | Legacy                                                       | Replacement |
    /// |--------------------------------------------------------------|-------------|
    /// | <c>new XPObjectSpaceProvider(ConfigurationManager.ConnectionStrings["HCM10ConnectionString"])</c> + <c>CreateObjectSpace()</c> inside <c>BeforePrint</c> | removed. The connection is pushed in by the caller through <see cref="HcmReportBase.ApplyConnectionString"/>. |
    /// | <c>object_Space.GetObjectsQuery&lt;Employee&gt;()</c> / <c>GetObjectsQuery&lt;SalaryRegister&gt;()</c> to find the latest regular payroll period and the employee's basic pay + currency | projected by the SQL instead, as <c>SalaryAmount</c>, <c>CurrencyCode</c>, <c>CurrencyName</c> and <c>HasRegularPayroll</c>. A DevExpress-constructed report has no DI container, so it cannot resolve ABP repositories; anything it needs must arrive through its data source or its parameters. |
    /// | <c>HCM10.Module.BusinessObjects.*</c> imports | gone; the report touches no entities. |
    /// | report-level <c>BeforePrint</c> | the label is now composed in <see cref="Detail_BeforePrint"/>. Report-level BeforePrint fires once, before any data row is current, so the legacy handler could only ever have described whichever employee its own XPO query happened to fetch -- it ignored the printed row entirely. On a single-employee certificate the two agree; the band-level handler is correct for both. |
    /// | client-side <c>FilterString = "[Code] In (?PrmEmpCode, ?)"</c> | replaced by <c>emp.Code = @PrmEmpCode</c> in SQL. See SalaryCertificateSql rewrite note 3 -- a client-side filter over a raw SqlDataSource fetches every tenant's rows before discarding them. |
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptSalaryCertificateEn : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SalaryCertificateEn";

        public rptSalaryCertificateEn()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            // See SalaryCertificateBackground: the letterhead moved out of the .resx.
            xrPictureBox1.Image = SalaryCertificateBackground.Load(SalaryCertificateBackground.EnglishResourceName);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptSalaryCertificateEn(string employeeCode, DateTime printDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = printDate;
        }

        /// <summary>
        /// Composes the salary sentence -- the one thing the legacy report did in code rather than in
        /// the layout, because it needs <see cref="HcmReportBase.AmountInWords"/>.
        ///
        /// Legacy text: <c>amount + " " + currencyCode + " (" + currencyName + " " + AmountInWords(amount) + " only)."</c>
        /// and it printed nothing at all unless the employee had at least one regular payroll run.
        /// Both behaviours are preserved; the inputs now come from the current data row.
        ///
        /// v17.2 -> v23.2: band BeforePrint is
        /// <c>BeforePrintEventHandler(CancelEventArgs)</c>, not <c>PrintEventHandler(PrintEventArgs)</c>.
        /// </summary>
        private void Detail_BeforePrint(object sender, CancelEventArgs e)
        {
            xrLabel9.Text = string.Empty;

            if (!TryGetColumn("HasRegularPayroll", out var hasRegularPayroll) ||
                Convert.ToInt32(hasRegularPayroll) != 1)
            {
                return;
            }

            if (!TryGetColumn("SalaryAmount", out var salaryAmount))
            {
                return;
            }

            var amount = Convert.ToDouble(salaryAmount);
            var currencyCode = TryGetColumn("CurrencyCode", out var code) ? Convert.ToString(code) : "KWD";
            var currencyName = TryGetColumn("CurrencyName", out var name) ? Convert.ToString(name) : "Kuwaiti Dinar";

            xrLabel9.Text = amount + " " + currencyCode + " (" + currencyName + " " + AmountInWords(amount) + " only).";
        }

        /// <summary>
        /// Reads a column of the row currently being printed, tolerating a missing column or a
        /// DBNull -- the certificate must still print for an employee with no payroll history.
        /// </summary>
        private bool TryGetColumn(string columnName, out object value)
        {
            value = null;

            try
            {
                value = GetCurrentColumnValue(columnName);
            }
            catch (Exception)
            {
                return false;
            }

            return value != null && value != DBNull.Value;
        }
    }
}
