using System;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Payroll.rptPayrollSheetByCostCenterPostingGroup</c>.
    ///
    /// The cost-centre payroll sheet with an outer grouping by FI posting group. Shares
    /// <see cref="PayrollSheetSql.PayrollSheetByCostCenterPostingGroupQuery"/>.
    ///
    /// XAF CONSTRUCTS REPLACED: <c>MyXtraReport</c> base -> <see cref="HcmReportBase"/>; the XPO
    /// connection name -> <see cref="HcmReportBase.DesignTimeConnectionName"/>;
    /// <c>Scripts.OnBeforePrint</c> dropped (removed from this API surface in v23.2).
    ///
    /// PORT DEVIATION -- the legacy SQL was wrapped in
    /// <c>IF (ISNULL(@PrmCompany,'') != '') BEGIN ... END</c>. DevExpress
    /// <c>CustomSqlQuery</c> rejects anything that is not purely SELECT, so the guard is expressed
    /// as an ordinary WHERE predicate. Semantics are identical: with no company selected the report
    /// returns no rows. Note the legacy default of <c>PrmPayrollRunType = -1</c> ("all") is kept.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPayrollSheetByCostCenterPostingGroup : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayrollSheetByCostCenterPostingGroup";

        public rptPayrollSheetByCostCenterPostingGroup()
        {
            InitializeComponent();

            PrmSheetDate.Value = DateTime.Now;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;

            // Legacy default: -1 == "all run types" (off-cycle first, then regular).
            PrmPayrollRunType.Value = -1;

            EnsureHcmParameters();
        }

        /// <summary>Convenience overload mirroring how the payroll run invoked the legacy report.</summary>
        public rptPayrollSheetByCostCenterPostingGroup(int month, int year, int payrollRunType, string company, string costCentre)
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
            // Legacy body was empty.
        }

        private void GroupHeader1_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Legacy body was empty.
        }

        private void rptPayrollSheetByCostCenterPostingGroup_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region << Header Text >>
            // The "015" prefix is a customer form number baked into the legacy header. Kept verbatim.
            lblHeader.Text = "015 PAYROLL SHEET FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            #endregion
        }
    }
}
