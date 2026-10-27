namespace HCMv3.Reports.Status
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.Status.srptHireDeparture</c> -- the DEPARTURES half of
    /// <see cref="rptHireDeparture"/>, nested as an <c>XRSubreport</c>.
    ///
    /// XAF CONSTRUCTS REPLACED
    /// | Legacy | Replacement |
    /// |---|---|
    /// | <c>this.Scripts.OnBeforePrint = "srptHireDeparture_BeforePrint"</c> in the designer | REMOVED. It named a script that has no body anywhere in the legacy source and no matching CLR handler, so it was already inert. |
    /// | <c>MyXtraReport</c> base (XAF) | <see cref="HcmReportBase"/>. |
    ///
    /// Its <c>PrmMonth</c> / <c>PrmYear</c> / <c>PrmPayrollArea</c> come from the master report's
    /// parameter bindings; <c>PrmTenantId</c> is set on it by the master's
    /// <see cref="HcmReportBase.ApplyTenant"/> call, which recurses into subreports.
    ///
    /// DATA GAP: EmployeeSeparations is EMPTY in HCMv3, so this renders no rows. Not a defect.
    /// </summary>
    public partial class srptHireDeparture : HcmReportBase
    {
        public const string ReportName = "HireDepartureSub";

        public srptHireDeparture()
        {
            InitializeComponent();

            EnsureHcmParameters();
        }
    }
}
