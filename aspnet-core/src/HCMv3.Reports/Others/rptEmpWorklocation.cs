namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptEmpWorklocation</c> --
    /// active employees listed by work location, grouped per location, with an optional
    /// single-location filter.
    ///
    /// | Legacy                                              | Replacement |
    /// |-----------------------------------------------------|-------------|
    /// | base class <c>MyXtraReport</c> (XAF)                 | <see cref="HcmReportBase"/>. |
    /// | <c>EmpWorklocationDS.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report's <c>.resx</c>                     | <see cref="OthersAdminSql"/>. |
    /// | (nothing)                                           | <see cref="HcmReportBase.EnsureHcmParameters"/> -- the hidden tenant parameter. |
    ///
    /// *** THE ARABIC SWITCH IS NOT PORTED. *** The legacy code-behind hooked
    /// <c>ParametersRequestBeforeShow</c> and, when
    /// <c>WebApplication.Instance.CurrentAspectProvider.CurrentAspect == "ar-KW"</c>, flipped the
    /// report to RightToLeft, overwrote six column captions with Arabic literals, and rewrote four
    /// ExpressionBindings to their Arabic columns ([FullNameAr], [JobTitleAr], [NationalityAr],
    /// [WL_Ar]).
    ///
    /// That cannot be carried over as-is and should not be: it read the CURRENT UI CULTURE from
    /// the XAF web application singleton, which does not exist here, and hard-coded Arabic strings
    /// inside a report. In HCMv3 the caller knows the culture and localisation belongs in ABP
    /// localisation. The SQL still projects every Arabic column the legacy switch needed
    /// (FullNameAr, JobTitleAr, NationalityAr, WL_Ar), so an Arabic variant of this report can be
    /// built without touching the query. Recorded under "Layout deviations" in
    /// PORTING-NOTES-OthersAdmin.md.
    ///
    /// MULTI-TENANCY: both queries -- the employee list and the work-location parameter look-up --
    /// carry <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/> before
    /// <c>CreateDocument()</c> or the report returns nothing. The look-up needing a tenant filter
    /// is not cosmetic: without it the parameter drop-down lists every tenant's work locations.
    /// </summary>
    public partial class rptEmpWorklocation : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmpWorklocation";

        public rptEmpWorklocation()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>
        /// Convenience overload. Pass 0 for "every work location" -- that is the legacy
        /// no-filter signal, preserved in
        /// <see cref="OthersAdminSql.EmpWorklocationQuery"/>.
        /// </summary>
        public rptEmpWorklocation(int workLocationId)
            : this()
        {
            PrmWorkLocation.Value = workLocationId;
        }
    }
}
