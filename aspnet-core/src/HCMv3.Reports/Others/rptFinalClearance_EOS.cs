using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptFinalClearance_EOS</c>.
    ///
    /// The end-of-service final clearance declaration: a laid-out (not scanned) A4 page carrying an
    /// English and an Arabic waiver paragraph, the employee's identity block, and a signature grid.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                        | Replacement |
    /// |---------------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                                      | <see cref="HcmReportBase"/>. |
    /// | <c>SqlDataSource.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report <c>.resx</c>                                | <see cref="OthersHrFormsSql.FinalClearanceEosQuery"/>. |
    /// | <c>xrLabel5.Text</c> / <c>xrLabel6.Text</c> from the <c>.resx</c> | inlined as string literals in the designer file. This project ships no .resx (see OthersHrFormsImageStore for why), and the two paragraphs are static contract text, not localisation. |
    /// | <c>xrPictureBox2.Image</c> from the <c>.resx</c>              | <see cref="OthersHrFormsImageStore"/>. |
    /// | <c>rptFinalClearance_EOS(object[] args)</c> ctor              | dropped. Its body was a commented-out cast of <c>args[0]</c> to <c>HCM10.Module.BusinessObjects.Workflow.LeaveRequest</c> (a type that does not exist in HCMv3) followed by the same <c>PrmFormDate</c> assignment as the default ctor. The typed overload below replaces it. |
    ///
    /// KNOWN LEGACY DEFECTS CARRIED OVER (see the porting notes):
    ///   * <c>xrPictureBox1</c> binds <c>[Companylogo]</c>; the SQL returns the employee unit as
    ///     <c>CompanylogoOrgUnitId</c> and the company logo is loaded from the file store by HcmReportBase (CompanyLogoLoader / ICompanyLogoResolver).
    ///   * The <c>CalcTotalDays</c> calculated field is dead: its DataMember is "LeaveFormQuery"
    ///     (a query this report does not have) and its expression reads <c>[Parameters].[PrmStartDate]</c>
    ///     and <c>[Parameters].[PrmEndDate]</c> (parameters this report does not declare). Nothing
    ///     binds it. Kept verbatim rather than deleted, because deleting it is a layout change.
    ///   * The data source, query and one field name all say "SickLeaveForm" -- copy-paste leftovers
    ///     from rptSickLeaveForm. Kept because <c>DataMember</c> binds the query by that name.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptFinalClearance_EOS : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "FinalClearanceEos";

        public rptFinalClearance_EOS()
        {
            InitializeComponent();

            PrmFormDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox2.Image = OthersHrFormsImageStore.Load(
                OthersHrFormsImageStore.FinalClearanceEosLogoResourceName);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptFinalClearance_EOS(string employeeCode, DateTime formDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmFormDate.Value = formDate;
        }
    }
}
