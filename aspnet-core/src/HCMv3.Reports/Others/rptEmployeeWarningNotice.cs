using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.Others.rptEmployeeWarningNotice</c>.
    ///
    /// The HR warning-notice form: a laid-out A4 page with an employee/manager identity table, two
    /// check-box panels (warning type and violation category), free-text lines, and an
    /// acknowledgement paragraph held as rich text.
    ///
    /// Not to be confused with <c>rptWarningLetter</c>, a different report in this folder that
    /// belongs to another batch.
    ///
    /// XAF CONSTRUCTS REPLACED:
    ///
    /// | Legacy                                                        | Replacement |
    /// |---------------------------------------------------------------|-------------|
    /// | <c>MyXtraReport</c> base                                      | <see cref="HcmReportBase"/>. |
    /// | <c>SqlDataSource.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL in the report <c>.resx</c>                                | <see cref="OthersHrFormsSql.EmployeeWarningNoticeQuery"/>. |
    /// | <c>xrRichText1.SerializableRtfString</c> from the <c>.resx</c> | inlined verbatim as a string literal in the designer file (base64-encoded UTF-16 RTF, byte-for-byte the legacy bytes). Same reason the images moved out of .resx -- this project ships none. |
    ///
    /// SEMANTIC DEVIATION (see OthersHrFormsSql rewrite note R1): the legacy query resolved
    /// <c>CompanyEn</c> from whichever of OrgLevel1..7 was typed 'Company' and <c>DepartmentEn</c>
    /// from whichever was typed 'Department'. HCMv3 org units carry no type and the employee has one
    /// unit, so both labels now print the SAME unit name. <c>Companylogo</c> is NULL (rewrite note
    /// R2). Flagged in the porting notes.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptEmployeeWarningNotice : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmployeeWarningNotice";

        public rptEmployeeWarningNotice()
        {
            InitializeComponent();

            // Legacy: this.PrmDate.Value = DateTime.Now.Date; -- date only, no time component.
            PrmDate.Value = DateTime.Now.Date;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptEmployeeWarningNotice(string employeeCode, DateTime noticeDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = noticeDate.Date;
        }
    }
}
