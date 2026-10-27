using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Bank
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Bank.rptBankFileAr</c>.
    ///
    /// Despite the name this is the fuller, sponsor-OPTIONAL variant of the salary-transfer file:
    /// account number and IBAN are printed side by side, the sponsor bank's clearing code for the
    /// employee's bank comes from PayeeBankDetails, and a payment-type filter wraps the query.
    /// Same XAF removals as <see cref="rptBankTransfer"/>; see that class for the table.
    ///
    /// DATA GAP: EmployeeBanks / Sponsors / SponsorBanks are EMPTY in HCMv3, so the bank and sponsor
    /// columns render blank. With no sponsor selected the payroll rows themselves do come back.
    /// </summary>
    public partial class rptBankFileAr : HcmReportBase
    {
        public const string ReportName = "BankFileAr";

        public rptBankFileAr()
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
        // The legacy body only assigned lblUserlogin.Text, now an expression binding.
        private void rptBankFileAr_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
