using System;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.EmployeeInfo.rptProbation_Letter</c>.
    ///
    /// XAF -> ABP: the legacy code-behind was 16 lines and did nothing but call
    /// <c>InitializeComponent()</c>. All XAF machinery lived in the shared <c>MyXtraReport</c>
    /// base class and is gone; see the <see cref="HcmReportBase"/> class remarks. Report-level
    /// row-level security is NOT ported: it is an authorization decision for the calling layer.
    ///
    /// This report and rptJobOffer are a copy-paste pair in the legacy source: byte-identical SQL
    /// and a shared Arabic paragraph. The SQL is therefore shared rather than duplicated (see
    /// <c>rptProbation_LetterSql</c>), so a fix lands on both.
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptProbation_Letter : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "ProbationLetter";

        public rptProbation_Letter()
        {
            InitializeComponent();
            PrmDate.Value = DateTime.Now.Date;
            EnsureHcmParameters();
        }

        /// <summary>
        /// Convenience overload. <paramref name="signatoryEmployeeCode"/> is the HR signatory's
        /// EMPLOYEE CODE even though the parameter is typed int -- that is the legacy contract, and
        /// the query casts it back to nvarchar to compare against Employees.Code.
        /// </summary>
        public rptProbation_Letter(string employeeCode, int signatoryEmployeeCode, DateTime asOfDate) : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmUser.Value = signatoryEmployeeCode;
            PrmDate.Value = asOfDate;
        }
    }
}
