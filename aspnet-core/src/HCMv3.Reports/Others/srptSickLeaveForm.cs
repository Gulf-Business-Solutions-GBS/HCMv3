namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF subreport
    /// <c>HCM10.Module.Reports.Others.srptSickLeaveForm</c>.
    ///
    /// The sick-leave history table printed inside <see cref="rptSickLeaveForm"/>. Not a standalone
    /// report -- it has no <c>ReportName</c> and is not registered in the report catalogue; it is
    /// instantiated by rptSickLeaveForm's <c>xrSubreport1.ReportSource</c>.
    ///
    /// The legacy code-behind was an empty constructor; the only change is the base class and the
    /// mandatory tenant plumbing.
    ///
    /// MULTI-TENANCY: <see cref="HcmReportBase.ApplyTenant"/> on the PARENT report reaches this
    /// subreport too -- <c>SetParameterEverywhere</c> walks nested XRSubreports. It still calls
    /// <c>EnsureHcmParameters</c> itself so the parameter and the query binding exist to be set.
    /// </summary>
    public partial class srptSickLeaveForm : HcmReportBase
    {
        public srptSickLeaveForm()
        {
            InitializeComponent();

            // MANDATORY even in a subreport: the query runs raw through SqlDataSource, outside EF
            // Core's per-tenant global query filter.
            EnsureHcmParameters();
        }
    }
}
