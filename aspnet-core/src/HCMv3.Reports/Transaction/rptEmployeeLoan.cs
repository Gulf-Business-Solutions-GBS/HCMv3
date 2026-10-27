using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Transaction
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Transaction.rptEmployeeLoan</c> --
    /// one employee's outstanding loans.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy | Replacement |
    /// |---|---|
    /// | <c>string CurrentUserName = SecuritySystem.CurrentUserName.ToString();</c> field initialiser, assigned to <c>lblUserlogin.Text</c> IN THE CONSTRUCTOR | <c>PrmLoggedUserName</c> report parameter, expression-bound to the label. Setting it in the ctor is why an exported .repx carries the exporting user's name as static text. |
    /// | <c>string UserNameLogin = ""</c> | dropped -- nothing ever assigned it. |
    /// | <c>rptEmployeeLoan_BeforePrint</c> body and <c>lblTotalUnpaidLeave_AfterPrint</c> body | ENTIRELY COMMENTED OUT in the legacy source (an abandoned <c>object_Space.GetObjectsQuery&lt;Employee&gt;()</c> / <c>EmployeeSeparation</c> service-span calculation). Nothing to port; the handlers are kept because the designer file wires them. |
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptEmployeeLoan : HcmReportBase
    {
        public const string ReportName = "EmployeeLoan";

        public rptEmployeeLoan()
        {
            InitializeComponent();

            PrmPrintDate.Value = DateTime.Now.Date;

            EnsureHcmParameters(withLoggedUserName: true);

            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        // Legacy body was entirely commented out; see the class remarks.
        private void rptEmployeeLoan_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        // Legacy body was entirely commented out; see the class remarks.
        private void lblTotalUnpaidLeave_AfterPrint(object sender, EventArgs e)
        {
        }
    }
}
