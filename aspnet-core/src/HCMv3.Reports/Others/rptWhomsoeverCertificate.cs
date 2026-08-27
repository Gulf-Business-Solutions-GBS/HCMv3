using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.GovtForms.rptWhomsoeverCertificate</c> (the class sat in the
    /// <c>GovtForms</c> namespace but the file lived in <c>Reports\Others</c>; the folder wins here).
    ///
    /// A "to whom it may concern" certificate printed over a full-page scanned letterhead: identity,
    /// salary, sponsor, bank details and the employee's daily duty window.
    ///
    /// XAF CONSTRUCTS REPLACED:
    /// | Legacy | Replacement |
    /// |--------|-------------|
    /// | base class <c>MyXtraReport</c> | <see cref="HcmReportBase"/>. |
    /// | <c>sqlDataSource1.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL + background bitmap in the <c>.resx</c> | <see cref="OthersCertificatesSql.WhomsoeverCertificateQuery"/> and <see cref="OthersCertificatesResources"/>. |
    /// | <c>sqlDataSource1.ResultSchemaSerializable</c> | dropped -- removed from the v23.2 API. |
    ///
    /// The legacy code-behind was a bare constructor; there is no business logic to port.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptWhomsoeverCertificate : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "WhomsoeverCertificate";

        public rptWhomsoeverCertificate()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now.Date;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            // Full-page letterhead scan; moved out of the .resx -- see OthersCertificatesResources.
            xrPictureBox1.Image = OthersCertificatesResources.LoadImage(
                OthersCertificatesResources.WhomsoeverBackgroundResource);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptWhomsoeverCertificate(string employeeCode, DateTime effectiveDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = effectiveDate;
        }
    }
}
