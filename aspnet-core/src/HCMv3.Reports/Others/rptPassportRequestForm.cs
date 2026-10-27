namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.GovtForms.rptPassportRequestForm</c> -- the printed request an
    /// employee signs to get their passport released.
    ///
    /// NAMESPACE NOTE: the legacy class sat in <c>Reports.GovtForms</c> even though the FILE lives
    /// in the <c>Reports\Others</c> folder -- a legacy mismatch. The port follows the folder, so
    /// this is <c>HCMv3.Reports.Others</c>, matching every other report in this batch. It is not a
    /// GovtForms report: it prints over a company form, not an official government one.
    ///
    /// | Legacy                                              | Replacement |
    /// |-----------------------------------------------------|-------------|
    /// | base class <c>MyXtraReport</c> (XAF)                 | <see cref="HcmReportBase"/>. |
    /// | <c>sqlDataSource1.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report's <c>.resx</c>                     | <see cref="OthersAdminSql"/>. |
    /// | <c>xrPictureBox1.Image</c> from the <c>.resx</c>     | <see cref="OthersAdminImageStore"/>. |
    /// | (nothing)                                           | <see cref="HcmReportBase.EnsureHcmParameters"/> -- the hidden tenant parameter. |
    ///
    /// THE EMPLOYEE FILTER STAYS CLIENT-SIDE, unlike the salary certificates. <c>PrmEmpCode</c> is
    /// a MULTI-VALUE parameter and the layout narrows with
    /// <c>FilterString "[Code] In (?PrmEmpCode)"</c>; a multi-value list cannot be bound to a
    /// single SQL parameter without dynamic SQL, which <c>CustomSqlQuery</c> will not accept. The
    /// query's tenant predicate bounds the fetch to one tenant, so this is a performance
    /// characteristic rather than a leak. See
    /// <see cref="OthersAdminSql.PassportRequestFormQuery"/>.
    ///
    /// DEPARTMENT: legacy took the org unit at level 2 by POSITION, with no unit-type filter. That
    /// translates exactly rather than approximately -- ABP's <c>Code</c> is a materialised path of
    /// five-digit segments, so level 2 is the ancestor whose <c>LEN(Code) = 11</c>. See
    /// <see cref="OthersAdminSql.PassportRequestFormQuery"/>. No own-unit fallback: an employee at
    /// level 1 has no level-2 ancestor and the label prints blank, as it did in the legacy report.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptPassportRequestForm : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "PassportRequestForm";

        public rptPassportRequestForm()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = OthersAdminImageStore.Load(OthersAdminImageStore.PassportRequestForm);
        }
    }
}
