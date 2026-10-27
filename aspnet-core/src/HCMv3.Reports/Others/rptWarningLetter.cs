using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptWarningLetter</c> -- the
    /// Arabic disciplinary warning letter, printed over a full-page scanned letterhead.
    ///
    /// It embeds two subreports: <see cref="srptWarningLetterCount"/> (the Arabic name of the selected
    /// disciplinary type) and <see cref="srptWarningLetterTotalCount"/> (how many warnings of that
    /// type the employee already has, and when the last one was).
    ///
    /// XAF CONSTRUCTS REPLACED:
    /// | Legacy | Replacement |
    /// |--------|-------------|
    /// | base class <c>MyXtraReport</c> | <see cref="HcmReportBase"/>. |
    /// | <c>EmpDiscReportDS.ConnectionName = "HCM10ConnectionString"</c> | <see cref="HcmReportBase.DesignTimeConnectionName"/>. |
    /// | SQL + background bitmap in the <c>.resx</c> | <see cref="OthersCertificatesSql"/> and <see cref="OthersCertificatesResources"/>. |
    /// | client-side <c>FilterString = "[Code] In (?PrmEmpCode)"</c> | <c>emp.Code = @PrmEmpCode</c> in SQL. A client-side filter over a raw SqlDataSource fetches every tenant's rows into the report engine before discarding them. <c>PrmEmpCode.MultiValue</c> is dropped with it. |
    /// | <c>EmpDiscReportDS.ResultSchemaSerializable</c> | dropped -- removed from the v23.2 API. |
    ///
    /// The legacy code-behind only set <c>PrmDate</c>; there is no business logic to port. The
    /// subreport class <c>srptWarningLetterCount</c> lived in the legacy <c>Reports.Payroll</c>
    /// namespace despite its file sitting in <c>Reports\Others</c>; the folder wins here and both
    /// subreports land in <see cref="HCMv3.Reports.Others"/>.
    ///
    /// SCHEMA CHANGE: HCMv3 has no <c>EmployeeDisciplinaryDetail</c> table -- one disciplinary event
    /// now carries exactly one type. See the note on
    /// <see cref="OthersCertificatesSql.WarningLetterQuery"/>.
    ///
    /// MULTI-TENANCY: all three queries here, and both subreports' queries, carry <c>@TenantId</c>.
    /// Call <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> -- it walks nested
    /// subreports -- or the report returns nothing.
    /// </summary>
    public partial class rptWarningLetter : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "WarningLetter";

        public rptWarningLetter()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            // Full-page letterhead scan; moved out of the .resx -- see OthersCertificatesResources.
            xrPictureBox1.Image = OthersCertificatesResources.LoadImage(
                OthersCertificatesResources.WarningLetterBackgroundResource);
        }

        /// <summary>Convenience overload for callers that already know the employee and warning type.</summary>
        public rptWarningLetter(string employeeCode, int disciplinaryTypeId, DateTime printDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDiscType.Value = disciplinaryTypeId;
            PrmDate.Value = printDate;
        }
    }
}
