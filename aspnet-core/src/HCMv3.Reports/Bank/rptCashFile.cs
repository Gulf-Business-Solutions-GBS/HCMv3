using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Bank
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Bank.rptCashFile</c> -- the cash-payment counterpart of
    /// the bank file. Same XAF removals as <see cref="rptBankTransfer"/>; see that class for the table.
    ///
    /// DATA GAP: EmployeeBanks / Sponsors / SponsorBanks are EMPTY in HCMv3, so the bank and sponsor
    /// columns render blank. With no sponsor selected the payroll rows themselves do come back.
    /// </summary>
    public partial class rptCashFile : HcmReportBase
    {
        public const string ReportName = "CashFile";

        public rptCashFile()
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
        // The legacy body only assigned lblUserlogin.Text, which is now an expression binding. The
        // handler is kept because the designer file wires it.
        private void rptCashFile_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
