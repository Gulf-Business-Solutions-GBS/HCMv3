using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Bank
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Bank.rptBankSupporting</c> -- the supporting schedule
    /// that accompanies the salary-transfer file sent to the sponsor's bank.
    /// Same XAF removals as <see cref="rptBankTransfer"/>; see that class for the table.
    ///
    /// DATA GAP: the query is keyed on <c>Sponsors.Id = @PrmSponsor</c> and Sponsors is EMPTY in the
    /// HCMv3 database, so this report returns NO ROWS until sponsor data is migrated.
    /// </summary>
    public partial class rptBankSupporting : HcmReportBase
    {
        public const string ReportName = "BankSupporting";

        public rptBankSupporting()
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
        private void rptBankSupporting_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            lblHeader.Text = "for the Month : " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
        }
    }
}
