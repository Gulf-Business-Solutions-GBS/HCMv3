namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF subreport <c>HCM10.Module.Reports.Payroll.srptPayslipWorkingDaysSummaryAr</c>.
    ///
    /// The legacy code-behind was an empty constructor. The two real changes are in the designer
    /// file and the SQL, not here:
    ///   * the tenant plumbing, which the legacy single-tenant schema had no equivalent of;
    ///   * the client-side <c>FilterString</c> ("[Employee Code] In (?...) And [ForPeriod] = ?...")
    ///     is gone, because the query now takes @empcode / @ForPeriod / @InPeriod / @RunType as real
    ///     SqlDataSource parameters. Filtering client-side over a raw SqlDataSource would fetch
    ///     every tenant's payroll rows before discarding them.
    /// </summary>
    public partial class srptPayslipWorkingDaysSummaryAr : HcmReportBase
    {
        public srptPayslipWorkingDaysSummaryAr()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }
    }
}