using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Bank
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Bank.rptBankAndCashTransferBySponsor</c> -- the
    /// bank/cash reconciliation summary keyed by sponsor, with an operator-chosen org-unit type
    /// (<c>PrmBreakupBy</c>) driving the breakup detail column.
    /// Same XAF removals as <see cref="rptBankTransfer"/>; see that class for the table.
    ///
    /// DATA GAP: Sponsors / SponsorBanks are EMPTY in HCMv3 -- the sponsor columns render blank.
    /// </summary>
    public partial class rptBankAndCashTransferBySponsor : HcmReportBase
    {
        public const string ReportName = "BankAndCashTransferBySponsor";

        public rptBankAndCashTransferBySponsor()
        {
            InitializeComponent();

            PrmPrintDate.Value = DateTime.Now;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmPayrollRunType.Value = 0;

            EnsureHcmParameters(withLoggedUserName: true);

            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        private void rptBankAndCashTransferBySponsor_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var selected = PrmPaymentType.Value == null ? "ALL" : PrmPaymentType.Value.ToString();
            var paymentType = selected == "ALL" ? "BANK/CASH" : selected;

            lblHeader.Text = paymentType + " TRANSFER AS PER THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            lblHeader2.Text = paymentType + " TRANSFER";
        }
    }
}
