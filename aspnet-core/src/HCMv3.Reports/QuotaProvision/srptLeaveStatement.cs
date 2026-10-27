namespace HCMv3.Reports.QuotaProvision
{
    /// <summary>
    /// HCMv3 port of <c>HCM10.Module.Reports.QuotaProvision.srptLeaveStatement</c> -- the
    /// annual-leave entitlement slab table nested inside <see cref="rptLeaveIndemnityStatement"/>.
    ///
    /// The legacy code-behind contained nothing but <c>InitializeComponent()</c>; the only change is
    /// the base class (<c>MyXtraReport</c> -> <see cref="HcmReportBase"/>) and the
    /// <c>EnsureHcmParameters()</c> call that adds the hidden tenant parameter.
    ///
    /// Its <c>PrmEmpCode</c> / <c>PrmYear</c> / <c>PrmMonth</c> arrive from the master report's
    /// parameter bindings; <c>PrmTenantId</c> is pushed in by the master's
    /// <see cref="HcmReportBase.ApplyTenant"/>, which recurses into subreports.
    /// </summary>
    public partial class srptLeaveStatement : HcmReportBase
    {
        public const string ReportName = "LeaveStatementSub";

        public srptLeaveStatement()
        {
            InitializeComponent();

            EnsureHcmParameters();
        }
    }
}
