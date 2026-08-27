using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Transaction
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Transaction.rptEmployeeLeave</c> -- absences overlapping
    /// a date range, optionally narrowed by employee, leave type, payroll area or cost centre.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy | Replacement |
    /// |---|---|
    /// | The constructor opened <c>new XPObjectSpaceProvider(ConfigurationManager.ConnectionStrings["HCM10ConnectionString"])</c>, resolved <c>PermissionPolicyUser</c> by <c>SecuritySystem.CurrentUserName</c>, then <c>Employee</c> by <c>LoginID</c>, purely to print the operator's FULL NAME instead of their login | <c>PrmLoggedUserName</c> report parameter, expression-bound to <c>lblUserlogin</c>. A DevExpress-constructed report has no ABP DI container and must not open its own database connection; the caller resolves the display name and passes it in. NOTE the caller should pass the employee's full name where one exists, to preserve the legacy output. |
    /// | <c>UserNameLogin != "" ? UserNameLogin : CurrentUserName</c> fallback in BeforePrint | folded into the single parameter -- the caller decides which name it supplies. |
    /// | <c>HCM10.Module.BusinessObjects.*</c> / <c>DevExpress.ExpressApp.*</c> imports | gone. |
    ///
    /// SEE ALSO the "LEGACY BUG, PORTED AS-IS" note on <see cref="TransactionSql.CostCenterList"/>:
    /// the cost-centre parameter cannot match anything, in HCMv3 or in the legacy app.
    /// </summary>
    public partial class rptEmployeeLeave : HcmReportBase
    {
        public const string ReportName = "EmployeeLeave";

        public rptEmployeeLeave()
        {
            InitializeComponent();

            PrmStartDate.Value = DateTime.Now.Date;
            PrmEndDate.Value = DateTime.Now.Date;

            EnsureHcmParameters(withLoggedUserName: true);

            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        // The legacy body only assigned lblUserlogin.Text, now an expression binding.
        private void rptEmployeeLeave_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
