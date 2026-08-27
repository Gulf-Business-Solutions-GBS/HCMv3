using System;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Payroll.rptPayrollSheetSummary</c>.
    ///
    /// The wide A3 landscape variant of <see cref="rptPayrollSheet"/>: same month-end population,
    /// but the earnings side is broken out into annual/sick leave, unpaid days and three overtime
    /// hour buckets, and the deduction side into residency, warning-letter, PIFSS and rent, each
    /// with an "everything else" residual computed as total-minus-named. Grouped in the layout by
    /// Division / Department / Section.
    ///
    /// Shares <see cref="PayrollSheetSql.PayrollSheetSummaryQuery"/>, which is composed from the
    /// same bucket aggregate as the rest of the family.
    ///
    /// XAF CONSTRUCTS REPLACED: <c>MyXtraReport</c> base -> <see cref="HcmReportBase"/>; the
    /// XPO connection name -> <see cref="HcmReportBase.DesignTimeConnectionName"/>;
    /// <c>Scripts.OnBeforePrint</c> dropped (removed from this API surface in v23.2). The legacy
    /// <c>int RecordCount</c> field was dead -- the manual page-break logic that used it was already
    /// commented out -- and is not ported.
    ///
    /// KNOWN GAP: the Division and Section grouping columns are always NULL in HCMv3 -- ABP's
    /// organisation-unit tree has no unit-type concept, so 'Division' and 'Section' are not
    /// expressible. See <see cref="PayrollSheetSql"/> and PORTING-NOTES-PayrollSheets.md.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPayrollSheetSummary : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayrollSheetSummary";

        public rptPayrollSheetSummary()
        {
            InitializeComponent();

            PrmSheetDate.Value = DateTime.Now;
            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmPayrollRunType.Value = 0;

            EnsureHcmParameters();
        }

        /// <summary>Convenience overload mirroring how the payroll run invoked the legacy report.</summary>
        public rptPayrollSheetSummary(int month, int year, int payrollRunType, string payrollArea)
            : this()
        {
            PrmMonth.Value = month;
            PrmYear.Value = year;
            PrmPayrollRunType.Value = payrollRunType;
            PrmPayrollArea.Value = payrollArea ?? string.Empty;
        }

        // v17.2 -> v23.2: control BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs). Body ported verbatim.
        private void lblHeader_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region << Header Text >>
            lblHeader.Text = "PAYROLL SHEET (SUMMARY) FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            #endregion
        }
    }
}
