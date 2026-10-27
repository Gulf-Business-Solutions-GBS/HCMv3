using System;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Payroll.rptPayrollSheetByCostCenter</c>.
    ///
    /// The payroll sheet grouped by cost centre, with the full allowance / overtime / deduction
    /// breakdown per employee. Shares
    /// <see cref="PayrollSheetSql.PayrollSheetByCostCenterQuery"/>, which is composed from the same
    /// bucket aggregate and cost-centre-family body as its three siblings
    /// (<see cref="rptPayrollSheetByCostCenterPostingGroup"/>,
    /// <see cref="rptPayrollSheetByCostCenterSummary"/>, <see cref="rptSalaryByBudgetingGroup"/>).
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy                                                | Replacement |
    /// |-------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base + a page of unused XAF/XPO/SMTP <c>using</c>s | <see cref="HcmReportBase"/>; the imports went with it -- the class referenced none of them |
    /// | <c>ConnectionName = "HCM10ConnectionString"</c>        | <see cref="HcmReportBase.DesignTimeConnectionName"/> |
    /// | <c>Scripts.OnBeforePrint</c>                           | dropped -- removed from this API surface in v23.2 |
    ///
    /// LEGACY DEAD CODE NOT PORTED: <c>Detail_BeforePrint</c> and <c>GroupHeader1_BeforePrint</c>
    /// were empty (a manual "page break every 25 rows" block, already commented out), and
    /// <c>rptPayrollSheetByCostCenter_BeforePrint</c> carried a 40-line commented-out month-name
    /// switch superseded by <see cref="HcmReportBase.GetMonthNameYear"/>. The handlers are kept
    /// because the designer file wires them.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPayrollSheetByCostCenter : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayrollSheetByCostCenter";

        public rptPayrollSheetByCostCenter()
        {
            InitializeComponent();

            PrmSheetDate.Value = DateTime.Now;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmPayrollRunType.Value = 0;

            EnsureHcmParameters();
        }

        /// <summary>Convenience overload mirroring how the payroll run invoked the legacy report.</summary>
        public rptPayrollSheetByCostCenter(int month, int year, int payrollRunType, string company, string costCentre)
            : this()
        {
            PrmMonth.Value = month;
            PrmYear.Value = year;
            PrmPayrollRunType.Value = payrollRunType;
            PrmCompany.Value = company ?? string.Empty;
            PrmCostCenter.Value = costCentre ?? string.Empty;
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        private void Detail_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Legacy body was entirely commented out (a manual page break every 25 detail rows,
            // superseded by the band's own KeepTogether / PageBreak settings). Nothing to port.
        }

        private void GroupHeader1_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Legacy body was empty.
        }

        private void rptPayrollSheetByCostCenter_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region << Header Text >>
            lblHeader.Text = "PAYROLL SHEET FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            #endregion
        }
    }
}
