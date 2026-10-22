using System;

namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Payroll.rptPayrollSheetBySponsor</c>.
    ///
    /// The only aggregate member of the payroll-sheet family: instead of a row per employee it
    /// prints headcount and total net pay per sponsor / department / payment type / pay interval,
    /// with the sponsor's MOSAL file number in the group header. Shares
    /// <see cref="PayrollSheetSql.PayrollSheetBySponsorQuery"/>.
    ///
    /// XAF CONSTRUCTS REPLACED: <c>MyXtraReport</c> base + a page of unused XAF/XPO/SMTP
    /// <c>using</c>s -> <see cref="HcmReportBase"/>; the XPO connection name ->
    /// <see cref="HcmReportBase.DesignTimeConnectionName"/>; <c>Scripts.OnBeforePrint</c> dropped
    /// (removed from this API surface in v23.2).
    ///
    /// NOTE the handler name: the designer wires <c>rptPayrollSheetSponsorWise_BeforePrint</c>, not
    /// <c>rptPayrollSheetBySponsor_BeforePrint</c> -- the class was renamed at some point and the
    /// handler was not. Kept as-is so the layout keeps binding.
    ///
    /// COMPANY LOGO: the header picture box binds <c>[Companylogo]</c>; the SQL returns the employee unit as
    /// <c>CompanylogoOrgUnitId</c> and the company logo is loaded from the file store by HcmReportBase (CompanyLogoLoader / ICompanyLogoResolver).
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPayrollSheetBySponsor : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PayrollSheetBySponsor";

        public rptPayrollSheetBySponsor()
        {
            InitializeComponent();

            PrmMonth.Value = DateTime.Now.AddMonths(-1).Month;
            PrmYear.Value = DateTime.Now.AddMonths(-1).Year;
            PrmSheetDate.Value = DateTime.Now.Date;

            EnsureHcmParameters();
        }

        /// <summary>Convenience overload mirroring how the payroll run invoked the legacy report.</summary>
        public rptPayrollSheetBySponsor(int month, int year, int payrollRunType, string payrollArea)
            : this()
        {
            PrmMonth.Value = month;
            PrmYear.Value = year;
            PrmPayrollRunType.Value = payrollRunType;
            PrmPayrollArea.Value = payrollArea ?? string.Empty;
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        private void rptPayrollSheetSponsorWise_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            #region << Header Text >>
            lblHeader.Text = "SALARY REPORT FOR THE MONTH OF " + GetMonthNameYear(PrmMonth.Value, PrmYear.Value);
            #endregion
        }
    }
}
