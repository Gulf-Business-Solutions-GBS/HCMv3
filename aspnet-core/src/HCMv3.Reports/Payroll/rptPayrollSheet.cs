using System;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Payroll.rptPayrollSheet</c>.
    ///
    /// The base member of the payroll-sheet family: a flat month-end sheet, one row per employee /
    /// salary register, bucketed into basic pay, allowances, annual and sick leave, miscellaneous
    /// pay and deduction, loan recovery and net pay. Its six siblings vary only in the grouping
    /// dimension; the SQL body they share lives in <see cref="PayrollSheetSql"/>.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy                                                | Replacement |
    /// |-------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base (XAF/XPO object-space glue)   | <see cref="HcmReportBase"/> |
    /// | <c>ConnectionName = "HCM10ConnectionString"</c>        | <see cref="HcmReportBase.DesignTimeConnectionName"/> |
    /// | <c>this.LoggedUserNme.Text = CurrentUserName</c> (already commented out in the legacy source) | not ported; the logged-in user is a caller-supplied parameter |
    /// | <c>Scripts.OnBeforePrint</c>                           | dropped -- v23.2 removed report scripts from this API surface |
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report returns
    /// nothing (the parameter defaults to 0, a tenant id matching nothing, so it fails closed).
    /// </summary>
    public partial class rptPayrollSheet : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayrollSheet";

        public rptPayrollSheet()
        {
            InitializeComponent();

            // Legacy defaulted the period to "last month" in the ctor. Kept: a harmless design-time
            // convenience that every real caller overwrites.
            PrmSheetDate.Value = DateTime.Now;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmPayrollRunType.Value = 0;

            EnsureHcmParameters();
        }

        /// <summary>
        /// The header logo is bound to the separate "Query" data member, not to the employee row. Legacy filled it
        /// with <c>SELECT logo FROM OrganizationUnit ... WHERE OrganizationUnitType.Code LIKE '%OrgLevel1%'</c>,
        /// i.e. the first level-1 unit, not the employee's company.
        /// </summary>
        protected override CompanyLogoSource StandaloneLogoSource => CompanyLogoSource.FirstLevel1Unit;

        /// <summary>
        /// Convenience overload mirroring how the payroll run invoked the legacy report.
        /// </summary>
        public rptPayrollSheet(int month, int year, int payrollRunType, string payrollArea, string reportType)
            : this()
        {
            PrmMonth.Value = month;
            PrmYear.Value = year;
            PrmPayrollRunType.Value = payrollRunType;
            PrmPayrollArea.Value = payrollArea ?? string.Empty;
            PrmReportType.Value = reportType ?? string.Empty;
        }

        // v17.2 -> v23.2: band BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs). Body ported verbatim.
        private void ReportHeader_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region << Header Text >>

            #region << Summary >>
            if (Convert.ToInt32(PrmReportType.Value) == 1)
            {
                lblHeader.Text = PrmPaymentType.Value != null && PrmPaymentType.Value.ToString() == "CASH"
                    ? "SUMMARY OF CASH PAYROLL SHEET FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value)
                    : "SUMMARY OF PAYROLL SHEET FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            }
            #endregion

            #region << Detail >>
            else
            {
                lblHeader.Text = PrmPaymentType.Value != null && PrmPaymentType.Value.ToString() == "CASH"
                    ? "CASH PAYROLL SHEET FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value)
                    : "PAYROLL SHEET FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            }
            #endregion

            #endregion
        }

        private void GroupHeader1_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // PrmReportType == 1 means "summary only": suppress the per-employee detail rows and the
            // column headings that describe them. Ported verbatim.
            var isSummary = Convert.ToInt32(PrmReportType.Value) == 1;

            GroupHeader1.Visible = !isSummary;
            Detail.Visible = !isSummary;

            lblHeader_Seq.Visible = !isSummary;
            lblHeader_Emp.Visible = !isSummary;
            lblHeader_EmpName.Visible = !isSummary;
            lblHeader_WorkingDays.Visible = !isSummary;
        }
    }
}
