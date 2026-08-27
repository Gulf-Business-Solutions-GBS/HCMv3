using System;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.EmployeeInfo.rptJobOffer</c>.
    ///
    /// XAF -> ABP: the legacy code-behind was 16 lines and did nothing but call
    /// <c>InitializeComponent()</c>. All XAF machinery lived in the shared <c>MyXtraReport</c>
    /// base class and is gone; see the <see cref="HcmReportBase"/> class remarks. Report-level
    /// row-level security is NOT ported: it is an authorization decision for the calling layer.
    ///
    /// TWO CARRIED-OVER LEGACY CONTENT DEFECTS, ported verbatim rather than silently corrected --
    /// both are product decisions, not porting decisions:
    ///   * the English body paragraph is a hard-coded literal naming one specific job title and one
    ///     specific customer company, with no field references at all;
    ///   * the Arabic body paragraph is the PROBATION-COMPLETION text, copy-pasted from
    ///     rptProbation_Letter, so the Arabic and English halves of this letter say different
    ///     things.
    /// See <c>EmployeeInfoResources.JobOfferBodyEnExpression</c> and
    /// <c>EmployeeInfoResources.ProbationBodyArExpression</c>.
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptJobOffer : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "JobOffer";

        public rptJobOffer()
        {
            InitializeComponent();
            PrmDate.Value = DateTime.Now.Date;
            EnsureHcmParameters();
        }

        /// <summary>
        /// Convenience overload. <paramref name="signatoryEmployeeCode"/> is the HR signatory's
        /// EMPLOYEE CODE even though the parameter is typed int -- that is the legacy contract, and
        /// the query casts it back to nvarchar to compare against Employees.Code (an implicit
        /// conversion would instead convert the whole Code COLUMN to int and throw on the first
        /// non-numeric employee code).
        /// </summary>
        public rptJobOffer(string employeeCode, int signatoryEmployeeCode, DateTime asOfDate) : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmUser.Value = signatoryEmployeeCode;
            PrmDate.Value = asOfDate;
        }
    }
}
