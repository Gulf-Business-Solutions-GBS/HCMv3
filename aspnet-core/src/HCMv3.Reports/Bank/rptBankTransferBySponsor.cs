using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Bank
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Bank.rptBankTransferBySponsor</c> -- rptBankTransfer
    /// grouped by sponsor instead of filtered by company. Same XAF removals as
    /// <see cref="rptBankTransfer"/>; see that class for the table.
    ///
    /// DATA GAP: Sponsors / EmployeeSponsors are EMPTY in the HCMv3 database, so the sponsor group
    /// header renders blank. Not a porting defect.
    /// </summary>
    public partial class rptBankTransferBySponsor : HcmReportBase
    {
        public const string ReportName = "BankTransferBySponsor";

        public rptBankTransferBySponsor()
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
        private void rptBankTransferBySponsor_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            lblHeader.Text = "BANK TRANSFER AS PER THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
        }
    }
}
