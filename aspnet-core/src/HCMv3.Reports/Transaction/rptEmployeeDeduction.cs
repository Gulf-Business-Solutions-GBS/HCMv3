using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Transaction
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Transaction.rptEmployeeDeduction</c> -- every recurring
    /// deduction in force for one employee in one month (recurring payments, loans, funds, asset
    /// invoices). Same XAF removals as <see cref="rptEmployeeLoan"/>; see that class for the table --
    /// including the two handler bodies, which are commented out in the legacy source here too.
    ///
    /// SEE ALSO the "FEATURE LOST" note on <see cref="TransactionSql.EmployeeDeductionQuery"/>:
    /// HCMv3's AssetTypes has no WageTypeId, so asset-invoice rows carry no wage-type name.
    /// </summary>
    public partial class rptEmployeeDeduction : HcmReportBase
    {
        public const string ReportName = "EmployeeDeduction";

        public rptEmployeeDeduction()
        {
            InitializeComponent();

            PrmMonth.Value = DateTime.Now.Month;
            PrmYear.Value = DateTime.Now.Year;
            PrmPrintDate.Value = DateTime.Now.Date;

            EnsureHcmParameters(withLoggedUserName: true);

            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        // Legacy body was entirely commented out; see the class remarks.
        private void rptEmployeeDeduction_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        // Legacy body was entirely commented out; see the class remarks.
        private void lblTotalUnpaidLeave_AfterPrint(object sender, EventArgs e)
        {
        }
    }
}
