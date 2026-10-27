using System;

namespace HCMv3.Reports.Status
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Status.rptHireDeparture</c> -- the monthly headcount
    /// movement sheet. The HIRES half is this report's own query; the DEPARTURES half is the nested
    /// <see cref="srptHireDeparture"/> subreport.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy | Replacement |
    /// |---|---|
    /// | <c>this.Scripts.OnBeforePrint = "rptHireDeparture_BeforePrint"</c> in the designer | REMOVED. Report scripts need compilable script source shipped with the layout; the CLR <c>BeforePrint</c> event is still wired and does the same job. The legacy handler body was empty anyway. |
    /// | <c>MyXtraReport</c> base (XAF) | <see cref="HcmReportBase"/>. |
    ///
    /// MULTI-TENANCY: <see cref="HcmReportBase.ApplyTenant"/> walks into the subreport too, so one
    /// call covers both halves -- but it must still be made before <c>CreateDocument()</c>.
    ///
    /// DATA GAP: EmployeeSeparations is EMPTY in HCMv3, so the departures subreport renders no rows.
    /// </summary>
    public partial class rptHireDeparture : HcmReportBase
    {
        public const string ReportName = "HireDeparture";

        public rptHireDeparture()
        {
            InitializeComponent();

            PrmMonth.Value = DateTime.Now.Month;
            PrmYear.Value = DateTime.Now.Year;

            EnsureHcmParameters();

            // The subreport is constructed by the designer, so its own ctor has already run
            // EnsureHcmParameters. Nothing further to wire here -- ApplyTenant / ApplyConnectionString
            // on this report reach it through HcmReportBase.SelfAndDescendants().
        }

        // v17.2 -> v23.2: PrintEventHandler(PrintEventArgs) -> BeforePrintEventHandler(CancelEventArgs).
        // Legacy body was empty; the handler is kept because the designer file wires it.
        private void rptHireDeparture_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
