using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.GovtForms.rptExperienceCertificate</c>
    /// (the class sat in the <c>GovtForms</c> namespace but the file lived in <c>Reports\Others</c>;
    /// the folder wins here, so it lands in <see cref="HCMv3.Reports.Others"/>).
    ///
    /// A bilingual (English + Arabic) experience certificate for one employee.
    ///
    /// XAF CONSTRUCTS REPLACED:
    /// | Legacy | Replacement |
    /// |--------|-------------|
    /// | base class <c>MyXtraReport</c> | <see cref="HcmReportBase"/>. |
    /// | <c>sqlDataSource1.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>, resolved server-side. |
    /// | SQL in the <c>.resx</c> | <see cref="OthersCertificatesSql.ExperienceCertificateQuery"/>. |
    /// | <c>sqlDataSource1.ResultSchemaSerializable</c> | dropped -- removed from the v23.2 API. |
    ///
    /// The legacy code-behind contained THIRTY-NINE empty <c>BeforePrint</c> handlers, one per label,
    /// every one of them a no-op left behind by the designer. They are carried over verbatim (with the
    /// v23.2 <c>CancelEventArgs</c> signature) rather than deleted, so the layout file stays a
    /// line-for-line match with the legacy one. They still do nothing.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptExperienceCertificate : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "ExperienceCertificate";

        public rptExperienceCertificate()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now.Date;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptExperienceCertificate(string employeeCode, DateTime printDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = printDate;
        }

        #region Designer-generated no-op BeforePrint handlers (ported verbatim; all empty in the legacy report)

        private void lblDateAr_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel1_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel2_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel3_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel4_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel5_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel6_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel7_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel8_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel9_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel10_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel11_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel12_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel13_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel14_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel15_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel16_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel17_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel18_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel19_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel20_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel21_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel22_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel23_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel24_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel25_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel26_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel27_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel28_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel29_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel30_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel31_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel32_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel33_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel43_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLabel44_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLine1_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLine3_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }
        private void xrLine15_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e) { }

        #endregion
    }
}
