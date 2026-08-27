using System;
using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptSalaryContinuousCertificate</c> -- the English
    /// salary-continuation certificate addressed to the employee's bank.
    ///
    /// XAF CONSTRUCTS REPLACED:
    /// | Legacy | Replacement |
    /// |--------|-------------|
    /// | base class <c>MyXtraReport</c> | <see cref="HcmReportBase"/>. |
    /// | <c>new XPObjectSpaceProvider(ConfigurationManager.ConnectionStrings["HCM10ConnectionString"])</c> + <c>CreateObjectSpace()</c> inside <c>BeforePrint</c>, to read <c>emp.BasicPay</c> and its detail's <c>Currency</c> | projected by the SQL as <c>Amount</c>, <c>CurrencyCode</c> and <c>CurrencyName</c>. A DevExpress-constructed report has no DI container, so it cannot resolve ABP repositories; anything it needs must arrive through its data source or its parameters. |
    /// | report-level <c>BeforePrint</c> | <see cref="Detail_BeforePrint"/>. Report-level BeforePrint fires once, before any data row is current, so <c>GetCurrentColumnValue</c> there returns nothing; the legacy handler worked only because it ran its own XPO query. |
    /// | SQL + letterhead + signature in the <c>.resx</c> | <see cref="OthersCertificatesSql.SalaryContinuousCertificateEnQuery"/> and <see cref="OthersCertificatesResources"/>. |
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptSalaryContinuousCertificate : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SalaryContinuousCertificate";

        public rptSalaryContinuousCertificate()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now.Date;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            // The two bitmaps moved out of the .resx -- see OthersCertificatesResources.
            xrPictureBox1.Image = OthersCertificatesResources.LoadImage(
                OthersCertificatesResources.SalaryContinuousLetterheadResource);
            xrPictureBox2.Image = OthersCertificatesResources.LoadImage(
                OthersCertificatesResources.SalaryContinuousSignatureResource);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptSalaryContinuousCertificate(string employeeCode, DateTime effectiveDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = effectiveDate;
        }

        /// <summary>
        /// Composes the monthly-salary sentence -- the one thing this report did in code rather than
        /// in the layout, because it needs <see cref="HcmReportBase.AmountInWords"/>.
        ///
        /// Legacy text: <c>currencyCode + " " + amount + " (" + currencyName + " " + AmountInWords(amount) + " )"</c>,
        /// and it printed nothing at all when the employee had no basic pay in force on PrmDate.
        /// Both behaviours are preserved; the inputs now come from the current data row.
        ///
        /// v17.2 -&gt; v23.2: band BeforePrint is
        /// <c>BeforePrintEventHandler(CancelEventArgs)</c>, not <c>PrintEventHandler(PrintEventArgs)</c>.
        /// </summary>
        private void Detail_BeforePrint(object sender, CancelEventArgs e)
        {
            lblMonthlySalary.Text = string.Empty;

            if (!TryGetColumn("Amount", out var amountValue))
            {
                return;
            }

            var amount = Convert.ToDouble(amountValue);
            var currencyCode = TryGetColumn("CurrencyCode", out var code) ? Convert.ToString(code) : string.Empty;
            var currencyName = TryGetColumn("CurrencyName", out var name) ? Convert.ToString(name) : string.Empty;

            lblMonthlySalary.Text =
                currencyCode + " " + amount + " (" + currencyName + " " + AmountInWords(amount) + " )";
        }

        /// <summary>
        /// Reads a column of the row currently being printed, tolerating a missing column or a
        /// DBNull -- the certificate must still print for an employee with no pay history.
        /// <c>GetCurrentColumnValue</c> returns <see cref="DBNull.Value"/>, not null.
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
