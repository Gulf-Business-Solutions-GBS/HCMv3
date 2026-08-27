namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptEmployeeConfirmationLetter</c> -- the letter confirming an
    /// employee's services after a satisfactory probation period.
    ///
    /// XAF CONSTRUCTS REPLACED:
    /// | Legacy | Replacement |
    /// |--------|-------------|
    /// | base class <c>MyXtraReport</c> | <see cref="HcmReportBase"/>. |
    /// | <c>sqlDataSource1.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL + two RTF blocks in the <c>.resx</c> | <see cref="OthersCertificatesSql.EmployeeConfirmationLetterQuery"/> and <see cref="OthersCertificatesResources"/>. |
    /// | <c>sqlDataSource1.ResultSchemaSerializable</c> | dropped -- removed from the v23.2 API. |
    ///
    /// The legacy code-behind was a bare constructor; there is no business logic to port.
    ///
    /// TYPO KEPT: the employee-code parameter is spelled <c>PrmEmpCpde</c> in the legacy layout, and
    /// the query parameter binds <c>[Parameters.PrmEmpCpde]</c>. Renaming it would break any saved
    /// layout or catalog entry that already refers to it, so the misspelling is carried over verbatim.
    ///
    /// COMPANY LOGO: <c>xrPictureBox1</c> binds the <c>Companylogo</c> column, which the SQL returns
    /// as NULL -- HCMv3 has no org-unit logo. The box prints empty. See OthersCertificatesSql's
    /// FEATURE LOST note.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptEmployeeConfirmationLetter : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmployeeConfirmationLetter";

        public rptEmployeeConfirmationLetter()
        {
            InitializeComponent();

            PrmEmpCpde.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptEmployeeConfirmationLetter(string employeeCode)
            : this()
        {
            PrmEmpCpde.Value = employeeCode ?? string.Empty;
        }
    }
}
