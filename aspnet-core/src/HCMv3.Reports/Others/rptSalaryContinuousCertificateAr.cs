using System;
using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptSalaryContinuousCertificateAr</c> -- the Arabic
    /// salary-continuation certificate.
    ///
    /// Same de-XAF-ing as <see cref="rptSalaryContinuousCertificate"/>: the XPObjectSpaceProvider
    /// round-trip in <c>BeforePrint</c> is replaced by columns projected in
    /// <see cref="OthersCertificatesSql.SalaryContinuousCertificateArQuery"/>, and the handler moves
    /// from report level to the Detail band so a data row is actually current when it runs.
    ///
    /// This report has no bitmaps -- its legacy .resx held only SQL.
    ///
    /// *** FEATURE GAP: Arabic amount-in-words. ***
    /// The legacy handler called <c>MyXtraReport.AmountInArabicWords(decimal)</c>. That helper is NOT
    /// part of <see cref="HcmReportBase"/> -- the base-class port deliberately left it out because it
    /// depends on the legacy <c>CurrencyInfo</c> type and on arabicOnes/arabicTens lookup tables that
    /// live outside <c>MyXtraReport.cs</c>. Until it is ported, the sentence prints the numeric amount
    /// inside the legacy Arabic wrapper with the spelled-out words segment left blank.
    /// Recorded under BLOCKED / Layout deviations in PORTING-NOTES-OthersCertificates.md.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptSalaryContinuousCertificateAr : HcmReportBase
    {
        /// <summary>
        /// Legacy Arabic wrapper around the amount-in-words segment: the text opened with
        /// "(only" and closed with "nothing more )". Held as constants so the RTL literals do not
        /// have to be read inline.
        /// </summary>
        private const string ArabicAmountPrefix = "(فقط";

        private const string ArabicAmountSuffix = "لا غير )";

        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SalaryContinuousCertificateAr";

        public rptSalaryContinuousCertificateAr()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now.Date;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptSalaryContinuousCertificateAr(string employeeCode, DateTime effectiveDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = effectiveDate;
        }

        /// <summary>
        /// Composes the Arabic monthly-salary sentence. See the FEATURE GAP note on the class: the
        /// amount-in-Arabic-words segment is blank until <c>AmountInArabicWords</c> is ported.
        ///
        /// Legacy shape, preserved exactly apart from that gap:
        /// <c>amount + ArabicAmountPrefix + arabicWords + ArabicAmountSuffix</c>. The legacy code also
        /// picked the LATEST basic pay (OrderByDescending StartDate) whereas its own SQL narrowed to
        /// the row in force on PrmDate; the SQL wins here, so the printed amount and the sentence
        /// cannot disagree.
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

            var amount = Convert.ToDecimal(amountValue);

            // FEATURE GAP: AmountInArabicWords is not ported; the words segment is blank.
            const string arabicWordsNotPorted = " ";

            lblMonthlySalary.Text = amount + ArabicAmountPrefix + arabicWordsNotPorted + ArabicAmountSuffix;
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
    }
}
