namespace HCMv3.Reports.Payroll
{
    /// <summary>
    /// HCMv3 port of the legacy XAF subreport <c>HCM10.Module.Reports.srptLeaveSettlementDeductionEn</c>.
    ///
    /// English deductions block of the leave settlement.
    ///
    /// The legacy code-behind was an empty constructor; the only real change is the new tenant
    /// plumbing, which the legacy single-tenant schema had no equivalent of.
    ///
    /// This subreport is instantiated by its parent (<see cref="rptLeaveSettlement"/>) and is never resolved by
    /// name, so <see cref="ReportName"/> exists only to keep the report-name space unique -- it is
    /// deliberately NOT registered in the Web.Host factory registry or the print catalog.
    /// </summary>
    public partial class srptLeaveSettlementDeductionEn : HcmReportBase
    {
        /// <summary>Stable unique key. Not registered in the factory registry -- see the class remarks.</summary>
        public const string ReportName = "LeaveSettlementDeductionEn";

        public srptLeaveSettlementDeductionEn()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }
    }
}
