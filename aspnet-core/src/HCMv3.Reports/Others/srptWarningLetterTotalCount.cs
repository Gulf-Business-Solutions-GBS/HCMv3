namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF subreport
    /// <c>HCM10.Module.Reports.Others.srptWarningLetterTotalCount</c>.
    ///
    /// Embedded by <see cref="rptWarningLetter"/> as <c>xrSubreport1</c>. Prints
    /// "Count: N | Last Committed on: dd/MM/yy" for the selected employee and disciplinary type,
    /// bound through the parent's <c>PrmEmpCode</c> / <c>PrmDiscType</c>.
    ///
    /// XAF CONSTRUCTS REPLACED:
    /// | Legacy | Replacement |
    /// |--------|-------------|
    /// | base class <c>MyXtraReport</c> | <see cref="HcmReportBase"/>. |
    /// | <c>sqlDataSource1.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | client-side <c>FilterString = "[C_EmpCode] In (?EmpCode) And [DisciplinaryType] = ?DiscType"</c> | WHERE predicates in <see cref="OthersCertificatesSql.WarningLetterTotalCountQuery"/>. <c>EmpCode.MultiValue</c> is dropped with it. |
    /// | <c>sqlDataSource1.ResultSchemaSerializable</c> | dropped -- removed from the v23.2 API. |
    ///
    /// DEFECT CARRIED OVER: the legacy GROUP BY includes <c>DisciplinaryDate</c>, so "Count" is the
    /// number of warnings of that type on ONE DAY, not the running total the label claims. Reproduced
    /// faithfully rather than fixed -- see the note on the query.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. <see cref="HcmReportBase.ApplyTenant"/> on
    /// the PARENT report reaches this one, because it walks nested subreports.
    /// </summary>
    public partial class srptWarningLetterTotalCount : HcmReportBase
    {
        public srptWarningLetterTotalCount()
        {
            InitializeComponent();

            // MANDATORY even in a subreport: EnsureHcmParameters adds this report's own hidden
            // PrmTenantId and wires the TenantId query parameter. Without it ApplyTenant on the
            // parent has no parameter here to set.
            EnsureHcmParameters();
        }
    }
}
