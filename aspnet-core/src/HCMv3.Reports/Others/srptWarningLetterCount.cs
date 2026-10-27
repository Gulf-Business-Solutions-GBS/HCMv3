namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF subreport <c>HCM10.Module.Reports.Payroll.srptWarningLetterCount</c>
    /// (the class sat in the <c>Payroll</c> namespace but its file lived in <c>Reports\Others</c>
    /// alongside the parent; the folder wins here).
    ///
    /// Embedded by <see cref="rptWarningLetter"/> as <c>xrSubreport2</c>. Despite the name it counts
    /// nothing -- it prints the Arabic name of the single disciplinary type the user selected, bound
    /// through the parent's <c>PrmDiscType</c> to this report's <c>DiscType</c> parameter.
    ///
    /// XAF CONSTRUCTS REPLACED:
    /// | Legacy | Replacement |
    /// |--------|-------------|
    /// | base class <c>MyXtraReport</c> | <see cref="HcmReportBase"/>. |
    /// | <c>sqlDataSource1.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | client-side <c>FilterString = "[OID] = ?DiscType"</c> | <c>discType.Id = @DiscType</c> in SQL -- see <see cref="OthersCertificatesSql.WarningLetterSelectedTypeQuery"/>. |
    /// | <c>sqlDataSource1.ResultSchemaSerializable</c> | dropped -- removed from the v23.2 API. |
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. <see cref="HcmReportBase.ApplyTenant"/> on
    /// the PARENT report reaches this one, because it walks nested subreports.
    /// </summary>
    public partial class srptWarningLetterCount : HcmReportBase
    {
        public srptWarningLetterCount()
        {
            InitializeComponent();

            // MANDATORY even in a subreport: EnsureHcmParameters adds this report's own hidden
            // PrmTenantId and wires the TenantId query parameter. Without it ApplyTenant on the
            // parent has no parameter here to set.
            EnsureHcmParameters();
        }
    }
}
