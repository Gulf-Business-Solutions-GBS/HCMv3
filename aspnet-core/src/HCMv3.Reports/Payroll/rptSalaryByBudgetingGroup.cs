using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Payroll.rptSalaryByBudgetingGroup</c>.
    ///
    /// The workmen-compensation category sheet: the same cost-centre-family population, grouped by
    /// budgeting group instead of cost centre, with a headcount column. Shares
    /// <see cref="PayrollSheetSql.SalaryByBudgetingGroupQuery"/>.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy                                                | Replacement |
    /// |-------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                              | <see cref="HcmReportBase"/> |
    /// | <c>string CurrentUserName = SecuritySystem.CurrentUserName</c> field, assigned to <c>lblUserlogin.Text</c> in a BeforePrint handler | <see cref="HcmReportBase.LoggedUserNameParameter"/>, expression-bound to the label. The legacy field was initialised at CLASS-CONSTRUCTION time from the XAF security system, which is why exported layouts carried the exporting developer's name. |
    /// | <c>ConnectionName = "HCM10ConnectionString"</c>        | <see cref="HcmReportBase.DesignTimeConnectionName"/> |
    ///
    /// PORT DEVIATION -- the legacy SQL was wrapped in
    /// <c>IF (ISNULL(@PrmCompany,'') != '') BEGIN ... END</c>; <c>CustomSqlQuery</c> accepts SELECT
    /// only, so the guard is an ordinary WHERE predicate. Semantics unchanged.
    ///
    /// DEFECT CARRIED OVER -- the GrossSalary column prints gross MINUS basic. See
    /// <see cref="PayrollSheetSql"/>.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptSalaryByBudgetingGroup : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SalaryByBudgetingGroup";

        public rptSalaryByBudgetingGroup()
        {
            InitializeComponent();

            PrmPrintDate.Value = DateTime.Now;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmPayrollRunType.Value = 0;

            EnsureHcmParameters(withLoggedUserName: true);

            // Replaces "lblUserlogin.Text = UserNameLogin != "" ? UserNameLogin : CurrentUserName",
            // which read the XAF security system. The caller supplies the name via
            // ApplyLoggedUserName; expression binding is used rather than mail merge because
            // XRLabel.Text = "[...]" silently fails to resolve in v23.2.
            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        /// <summary>Convenience overload mirroring how the payroll run invoked the legacy report.</summary>
        public rptSalaryByBudgetingGroup(int month, int year, int payrollRunType, string company, string budgetingGroup)
            : this()
        {
            PrmMonth.Value = month;
            PrmYear.Value = year;
            PrmPayrollRunType.Value = payrollRunType;
            PrmCompany.Value = company ?? string.Empty;
            PrmBudgetingGroup.Value = budgetingGroup ?? string.Empty;
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        private void lblHeader_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // The legacy handler also assigned lblUserlogin.Text here; that is now the expression
            // binding set up in the constructor.
            #region << Header Text >>
            lblHeader.Text = "WORKMEN COMPENSATION CATEGORY (PY) : " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            #endregion
        }

        private void GroupHeader1_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // PrmReportType == 1 means "totals only": suppress the per-employee rows. Ported verbatim.
            var isSummary = Convert.ToInt32(PrmReportType.Value) == 1;

            GroupHeader1.Visible = !isSummary;
            Detail.Visible = !isSummary;
        }
    }
}
