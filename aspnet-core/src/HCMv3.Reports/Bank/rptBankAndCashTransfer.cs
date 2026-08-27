using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Bank
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Bank.rptBankAndCashTransfer</c> -- the reconciliation
    /// summary that splits a month's net pay into bank / cash / on-hold buckets.
    /// Same XAF removals as <see cref="rptBankTransfer"/>; see that class for the table.
    /// </summary>
    public partial class rptBankAndCashTransfer : HcmReportBase
    {
        public const string ReportName = "BankAndCashTransfer";

        public rptBankAndCashTransfer()
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
        private void rptBankAndCashTransfer_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Legacy read PrmPaymentType.Value.ToString() unguarded; the parameter defaults to null
            // in the Web Document Viewer when the operator never opens the parameters panel, which
            // would NullReference. Guarded here -- null now behaves like the legacy "ALL".
            var selected = PrmPaymentType.Value == null ? "ALL" : PrmPaymentType.Value.ToString();
            var paymentType = selected == "ALL" ? "BANK/CASH" : selected;

            lblHeader.Text = paymentType + " TRANSFER AS PER THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            lblHeader2.Text = paymentType + " TRANSFER";
        }
    }
}
