using System;
using DevExpress.XtraReports.UI;

namespace HCMv3.Reports.Bank
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Bank.rptBankTransfer</c> --
    /// the monthly bank transfer letter, one line per employee grouped by company / bank.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy | Replacement |
    /// |---|---|
    /// | <c>string CurrentUserName = SecuritySystem.CurrentUserName.ToString();</c> -- a FIELD INITIALISER, so it ran during construction | <c>PrmLoggedUserName</c> report parameter, expression-bound to <c>lblUserlogin</c>. A DevExpress-constructed report has no ABP session; the caller supplies the name via <see cref="HcmReportBase.ApplyLoggedUserName"/>. |
    /// | <c>string UserNameLogin = ""</c> and the <c>UserNameLogin != "" ? ... : CurrentUserName</c> ternary | dropped. Nothing in the legacy codebase ever assigned <c>UserNameLogin</c>, so the ternary always chose <c>CurrentUserName</c>. |
    /// | <c>HCM10.Module.BusinessObjects.*</c> / <c>DevExpress.ExpressApp.*</c> imports | gone; the report touches no entities, only SQL and parameters. |
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptBankTransfer : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "BankTransfer";

        public rptBankTransfer()
        {
            InitializeComponent();

            // Legacy defaulted the period to "last month" in the ctor. Kept: harmless design-time
            // convenience, and every real caller overwrites it.
            PrmPrintDate.Value = DateTime.Now;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmPayrollRunType.Value = 0;

            EnsureHcmParameters(withLoggedUserName: true);

            lblUserlogin.ExpressionBindings.Add(
                new ExpressionBinding("BeforePrint", "Text", "[Parameters." + LoggedUserNameParameter + "]"));
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs).
        private void rptBankTransfer_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // The legacy body also assigned lblUserlogin.Text here; that is now the expression
            // binding wired in the constructor. The header text survives unchanged.
            lblHeader.Text = "BANK TRANSFER AS PER THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
        }
    }
}
