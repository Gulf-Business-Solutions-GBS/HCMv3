using System;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Payroll.rptPayrollSheetByCostCenterSummary</c>.
    ///
    /// The cost-centre roll-up: same population and buckets as
    /// <see cref="rptPayrollSheetByCostCenter"/>, but the layout prints only the headline money
    /// columns and totals per cost centre. It is the only member of the family that carries the
    /// air-ticket bucket. Shares
    /// <see cref="PayrollSheetSql.PayrollSheetByCostCenterSummaryQuery"/>.
    ///
    /// XAF CONSTRUCTS REPLACED: <c>MyXtraReport</c> base + a page of unused XAF/XPO/SMTP
    /// <c>using</c>s -> <see cref="HcmReportBase"/>; the XPO connection name ->
    /// <see cref="HcmReportBase.DesignTimeConnectionName"/>; <c>Scripts.OnBeforePrint</c> dropped
    /// (removed from this API surface in v23.2).
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPayrollSheetByCostCenterSummary : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayrollSheetByCostCenterSummary";

        public rptPayrollSheetByCostCenterSummary()
        {
            InitializeComponent();

            PrmSheetDate.Value = DateTime.Now;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmPayrollRunType.Value = 0;

            EnsureHcmParameters();
        }

        /// <summary>Convenience overload mirroring how the payroll run invoked the legacy report.</summary>
        public rptPayrollSheetByCostCenterSummary(int month, int year, int payrollRunType, string company, string costCentre)
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
            // Legacy body was entirely commented out (a manual page break every 25 detail rows).
        }

        private void GroupHeader1_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Legacy body was empty.
        }

        private void rptPayrollSheetByCostCenterSummary_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region << Header Text >>
            lblHeader.Text = "PAYROLL SHEET FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            #endregion
        }
    }
}
