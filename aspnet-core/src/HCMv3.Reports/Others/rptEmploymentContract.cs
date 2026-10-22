using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptEmploymentContract</c>.
    ///
    /// The long-form bilingual employment contract -- by far the largest layout in this batch
    /// (a 3,500-line designer file): an English column and a mirrored Arabic column of numbered
    /// clauses, six of them held as rich text rather than labels.
    ///
    /// This is the generic contract. <see cref="rptEmploymentContractAr023"/> and
    /// <see cref="rptEmploymentContractEn023"/> are the other two BASE contract reports in the legacy
    /// tree (their "023" suffix is part of the report name, not a <c>_CustomNNN</c> variant marker).
    /// All three share <see cref="OthersHrFormsSql.EmploymentContractWageQuery"/>, which was
    /// byte-identical in all three legacy .resx files and is held once here.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                        | Replacement |
    /// |---------------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                                      | <see cref="HcmReportBase"/>. |
    /// | <c>SqlDataSource.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report <c>.resx</c>                                | <see cref="OthersHrFormsSql.EmploymentContractQuery"/> and <see cref="OthersHrFormsSql.EmploymentContractWageQuery"/>. |
    /// | eleven <c>xrLabelNN.Text</c> and six <c>SerializableRtfString</c> entries in the <c>.resx</c> | inlined verbatim as string literals in the designer file. The RTF is carried across base64-for-base64, so the rendered blocks are byte-identical to the legacy print. |
    /// | <c>rptEmploymentContract(object[] args)</c> ctor              | dropped; it did nothing but <c>InitializeComponent()</c> plus the PrmFormDate assignment. |
    /// | <c>using DevExpress.XtraRichEdit(.API.Native)</c>             | dropped. The legacy file imported both and used neither -- a leftover from a commented-out <c>OverrideRtfFormatting = true</c>. |
    ///
    /// NO CODE-BEHIND LOGIC: unlike its two siblings, this contract's legacy code-behind held no
    /// BeforePrint handler and no XPO query, so nothing had to move into the data source beyond the
    /// schema rewrites. Everything on the page is either static text or an expression binding.
    ///
    /// COMPANY LOGO: <c>Companylogo</c> is a NULL placeholder; the SQL returns the employee unit as
    /// <c>CompanylogoOrgUnitId</c> and the company logo is loaded from the file store by HcmReportBase (CompanyLogoLoader / ICompanyLogoResolver).
    ///
    /// MULTI-TENANCY: both queries carry <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptEmploymentContract : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmploymentContract";

        public rptEmploymentContract()
        {
            InitializeComponent();

            PrmFormDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptEmploymentContract(string employeeCode, DateTime formDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmFormDate.Value = formDate;
        }
    }
}
