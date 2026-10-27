using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Bank
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Bank.rptBankFile</c> -- the Arabic covering letter that
    /// instructs the sponsor's bank to transfer the month's salaries, with one line per employee.
    /// FINANCIAL OUTPUT: the paragraph this class builds states, in figures and in Arabic words, the
    /// amount the bank is being told to move.
    ///
    /// XAF CONSTRUCTS REPLACED -- as <see cref="rptBankTransfer"/>, plus:
    /// | Legacy | Replacement |
    /// |---|---|
    /// | <c>AmountInArabicWords</c> inherited from <c>MyXtraReport</c> | <see cref="ArabicAmountInWords.Convert"/>. The payslip port explicitly deferred these helpers ("port them with the first Arabic report that needs them"); this is that report. Ported bug-for-bug -- see the remarks on that class. |
    ///
    /// DATA GAP: the query is keyed on <c>Sponsors.Id = @PrmSponsor</c> and Sponsors is EMPTY in the
    /// HCMv3 database, so this report returns NO ROWS until sponsor data is migrated. Not a defect.
    /// </summary>
    public partial class rptBankFile : HcmReportBase
    {
        public const string ReportName = "BankFile";

        public rptBankFile()
        {
            InitializeComponent();

            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmPrintDate.Value = DateTime.Now.Date;
            PrmPayrollRunType.Value = 0;

            EnsureHcmParameters(withLoggedUserName: true);

            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        private void rptBankFile_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // PORT DEVIATION (defensive, deliberate). The legacy line was:
            //     double amount = (double)GetCurrentColumnValue("TotalNetPay");
            // Two problems that only bite in HCMv3:
            //   * an unboxing cast. TotalNetPay is SUM(...) OVER () of SalaryRegisterDetails.Amount,
            //     which is decimal in HCMv3, so (double)boxedDecimal throws InvalidCastException.
            //     Convert.ToDecimal handles either shape.
            //   * no null guard. Whenever the query returns no rows -- which is the CURRENT state of
            //     HCMv3, Sponsors being empty -- GetCurrentColumnValue returns null and the legacy
            //     cast throws, turning an empty report into a 500. Treated as zero instead.
            // The printed text for a non-empty result is byte-for-byte what the legacy produced.
            var totalNetPay = GetCurrentColumnValue("TotalNetPay");
            var amount = totalNetPay == null ? 0m : Convert.ToDecimal(totalNetPay);

            var amountInWords = ArabicAmountInWords.Convert(amount);
            var sponsorAccountNo = GetCurrentColumnValue("SponsorAccountNumber");

            var text = "بالاشاره الى الموضوع اعلاه يرجى التكرم باتخاذ اللازم وتحويل مبلغ  وقــــدره";
            text = text + " " + amount.ToString("N3") + " " + amountInWords + "/  قيد مبلغ على حسابنا لديكم رقم ";
            text = text + " " + sponsorAccountNo;

            lblNetPayText.Text = text;
        }
    }
}
