using System;
using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptSalaryIncrementLetter</c> -- the promotion / salary-increment
    /// letter, with a before-and-after wage-detail subreport on each side of the page.
    ///
    /// Unlike the rest of this batch it is NOT single-employee: an empty <c>PrmEmpCode</c> means
    /// "every employee whose job title or basic pay changed in (PrmMonth, PrmYear)", which is how the
    /// letter is run in bulk. See <see cref="OthersCertificatesSql.SalaryIncrementLetterQuery"/>.
    ///
    /// XAF CONSTRUCTS REPLACED:
    /// | Legacy | Replacement |
    /// |--------|-------------|
    /// | base class <c>MyXtraReport</c> | <see cref="HcmReportBase"/>. |
    /// | a private <c>static string AmountInWords(double)</c> copy-pasted into this report | <see cref="HcmReportBase.AmountInWords"/>. The two implementations were character-for-character identical. |
    /// | <c>GetSalaryAmountText()</c>: <c>new XPObjectSpaceProvider(...)</c> + <c>CreateObjectSpace()</c> to re-read the employee's basic pay and currency, driven off the label's own <c>.Value</c> | <see cref="UpdateMonthlySalary"/>, reading the current data row. <c>CurrencyCode</c> / <c>CurrencyName</c> were ADDED to the query for this; a DevExpress-constructed report has no DI container and cannot resolve ABP repositories. |
    /// | <c>label.Tag = parentReport.GetCurrentRow()</c> in both label handlers | dropped. Nothing ever read the Tag; it was debugging residue. |
    /// | SQL in the <c>.resx</c> | <see cref="OthersCertificatesSql.SalaryIncrementLetterQuery"/> and <see cref="OthersCertificatesSql.EmployeeWageDetailDatewiseQuery"/>. |
    /// | <c>EmpProfDS.ResultSchemaSerializable</c> | dropped -- removed from the v23.2 API. |
    ///
    /// LEGACY QUIRK KEPT: <c>lblMonthlySalary</c> is refreshed from BOTH <c>lblEmployeeCode</c>'s and
    /// <c>lblBasicPayEffectiveDate</c>'s BeforePrint, exactly as the legacy report did. Whichever
    /// prints first wins; the second call is idempotent. The legacy report needed two entry points
    /// because its XPO query keyed off the two labels' values; here it is simply harmless.
    ///
    /// TYPE FIX: the legacy query parameters for <c>PrmMonth</c> / <c>PrmYear</c> declared their
    /// DevExpress <c>Expression</c> result type as <c>short</c> while the report parameters are
    /// <c>int</c>. Corrected to <c>int</c> on both sides -- a mismatch there fails at run time.
    ///
    /// COMPANY LOGO: <c>xrPictureBox1</c> binds <c>Companylogo</c>, which the SQL returns as NULL.
    ///
    /// MULTI-TENANCY: both queries carry <c>@TenantId</c>, and so do the subreports'. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> -- it walks nested
    /// subreports -- or the report returns nothing.
    /// </summary>
    public partial class rptSalaryIncrementLetter : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SalaryIncrementLetter";

        public rptSalaryIncrementLetter()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now.Date;
            PrmMonth.Value = DateTime.Now.Month;
            PrmYear.Value = DateTime.Now.Year;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>
        /// Convenience overload. Pass <c>null</c> or <c>""</c> for <paramref name="employeeCode"/> to
        /// print every employee with an increment in that month -- the legacy bulk behaviour.
        /// </summary>
        public rptSalaryIncrementLetter(string employeeCode, int month, int year, DateTime printDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmMonth.Value = month;
            PrmYear.Value = year;
            PrmDate.Value = printDate;
        }

        /// <summary>
        /// Composes the monthly-salary sentence from the current data row.
        ///
        /// Legacy text: <c>currencyCode + " " + amount + " (" + currencyName + " " + AmountInWords(amount) + " Only)"</c>,
        /// and it printed nothing at all when the employee had no basic pay in force. Both behaviours
        /// are preserved.
        /// </summary>
        private void UpdateMonthlySalary()
        {
            lblMonthlySalary.Text = string.Empty;

            if (!TryGetColumn("Curr_EmployeeBasicPay", out var amountValue))
            {
                return;
            }

            var amount = Convert.ToDouble(amountValue);
            var currencyCode = TryGetColumn("CurrencyCode", out var code) ? Convert.ToString(code) : "KWD";
            var currencyName = TryGetColumn("CurrencyName", out var name) ? Convert.ToString(name) : "Kuwaiti Dinar";

            lblMonthlySalary.Text =
                currencyCode + " " + amount + " (" + currencyName + " " + AmountInWords(amount) + " Only)";
        }

        /// <summary>
        /// Reads a column of the row currently being printed, tolerating a missing column or a
        /// DBNull. <c>GetCurrentColumnValue</c> returns <see cref="DBNull.Value"/>, not null.
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

        // v17.2 -> v23.2: control/band BeforePrint is BeforePrintEventHandler(CancelEventArgs), not
        // PrintEventHandler(PrintEventArgs). AfterPrint is a plain EventHandler in both versions.

        /// <summary>Legacy entry point 1 into <see cref="UpdateMonthlySalary"/>. See the class remarks.</summary>
        private void lblEmployeeCode_BeforePrint(object sender, CancelEventArgs e)
        {
            UpdateMonthlySalary();
        }

        /// <summary>Legacy entry point 2 into <see cref="UpdateMonthlySalary"/>. See the class remarks.</summary>
        private void lblBasicPayEffectiveDate_BeforePrint(object sender, CancelEventArgs e)
        {
            UpdateMonthlySalary();
        }

        /// <summary>Empty in the legacy report (its body was a commented-out GetSalaryAmountText call).</summary>
        private void Detail_BeforePrint(object sender, CancelEventArgs e) { }

        /// <summary>Empty in the legacy report (its body was a commented-out GetSalaryAmountText call).</summary>
        private void Detail_AfterPrint(object sender, EventArgs e) { }
    }
}
