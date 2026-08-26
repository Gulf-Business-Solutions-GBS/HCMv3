namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF subreport <c>HCM10.Module.Reports(.Payroll).srptPaySlipDeductionEn</c>.
    ///
    /// The legacy code-behind was an empty constructor plus a <c>using DevExpress.ExpressApp;</c>
    /// that nothing consumed; the only real change is the new tenant plumbing, which the legacy
    /// single-tenant schema had no equivalent of.
    /// </summary>
    public partial class srptPaySlipDeductionEn : HcmReportBase
    {
        public srptPaySlipDeductionEn()
        {
            InitializeComponent();

            // MANDATORY: the SQL below runs raw through SqlDataSource, outside EF Core's
            // per-tenant global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }
    }
}