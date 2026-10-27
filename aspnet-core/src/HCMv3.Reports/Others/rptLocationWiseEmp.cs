using System.ComponentModel;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptLocationWiseEmp</c> --
    /// an Arabic-headed roster of the active employees at ONE work location, signed off by a
    /// named HR officer.
    ///
    /// | Legacy                                              | Replacement |
    /// |-----------------------------------------------------|-------------|
    /// | base class <c>MyXtraReport</c> (XAF)                 | <see cref="HcmReportBase"/>. |
    /// | <c>EmpWorklocationDS.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report's <c>.resx</c>                     | <see cref="OthersAdminSql"/>. |
    /// | <c>ParametersRequestBeforeShow</c> presetting PrmSignature / PrmCompany | the same two defaults, set in this constructor. The legacy hook needed XAF's parameter-request pipeline; assigning the parameter values directly is equivalent and works from any host. |
    /// | (nothing)                                           | <see cref="HcmReportBase.EnsureHcmParameters"/> -- the hidden tenant parameter. |
    ///
    /// DEFECT CARRIED OVER DELIBERATELY: <c>PrmSignature</c> and <c>PrmCompany</c> are
    /// StaticListLookUp parameters whose values are hard-coded Arabic company and officer names
    /// belonging to ONE customer, baked into the layout. That is wrong for a multi-tenant product
    /// -- every tenant now sees that customer's names in the parameter drop-down -- but it is a
    /// LAYOUT fact, not a data leak, and "fixing" it means deciding where those names should come
    /// from instead. Reproduced faithfully and recorded in PORTING-NOTES-OthersAdmin.md.
    ///
    /// *** SEMANTIC DEVIATION -- SPONSOR. *** The page header prints a trade-licence number that
    /// legacy read from the WORK LOCATION's sponsor. HCMv3 has no work-location-to-sponsor link,
    /// so it is sourced from the EMPLOYEE's sponsor instead. See
    /// <see cref="OthersAdminSql.LocationWiseEmpQuery"/> and the porting notes -- this needs a
    /// product decision.
    ///
    /// MULTI-TENANCY: both queries carry <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptLocationWiseEmp : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "LocationWiseEmp";

        /// <summary>
        /// The signatory the legacy report defaulted to, and the one
        /// <see cref="PageFooter_BeforePrint"/> tests against to choose a job title. Kept as a
        /// named constant so the string literal appears once rather than twice.
        /// </summary>
        private const string DefaultSignatory = "محمد بشير";

        public rptLocationWiseEmp()
        {
            InitializeComponent();

            // Legacy MyXtraReport_ParametersRequestBeforeShow body, verbatim.
            PrmSignature.Value = DefaultSignatory;
            PrmCompany.Value = "شركة الطرف الاغر للتجارة العامة";

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for callers that already know the work location.</summary>
        public rptLocationWiseEmp(int workLocationId)
            : this()
        {
            PrmWorkLocation.Value = workLocationId;
        }

        /// <summary>
        /// Picks the signatory's Arabic job title from which signatory was chosen. Ported verbatim
        /// from the legacy handler, hard-coded names included -- see the class remarks.
        ///
        /// v17.2 -&gt; v23.2: band BeforePrint is
        /// <c>BeforePrintEventHandler(CancelEventArgs)</c>, not
        /// <c>PrintEventHandler(PrintEventArgs)</c>.
        /// </summary>
        private void PageFooter_BeforePrint(object sender, CancelEventArgs e)
        {
            if (PrmSignature.Value != null)
            {
                lblMudirTitle.Text = (string)PrmSignature.Value == DefaultSignatory
                    ? "مدير الموارد البشرية"
                    : "مدير العمليات";
            }
        }
    }
}
