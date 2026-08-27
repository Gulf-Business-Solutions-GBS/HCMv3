using System;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.EmployeeInfo.rptEmploymentNotice</c>.
    ///
    /// XAF -> ABP: the legacy code-behind was 16 lines and did nothing but call
    /// <c>InitializeComponent()</c>. All XAF machinery lived in the shared <c>MyXtraReport</c>
    /// base class and is gone; see the <see cref="HcmReportBase"/> class remarks. Report-level
    /// row-level security is NOT ported: it is an authorization decision for the calling layer.
    ///
    /// The legacy report left <c>PrmDate</c> at <c>DateTime.MinValue</c> until an operator typed
    /// one, which SqlClient rejects outright ("SqlDateTime overflow") rather than returning no
    /// rows. Defaulted to today here, matching the sibling reports in the folder. (HcmReportBase
    /// also clamps stray MinValue dates on DataSourceDemanded as a second line of defence.)
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptEmploymentNotice : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmploymentNotice";

        public rptEmploymentNotice()
        {
            InitializeComponent();
            PrmDate.Value = DateTime.Now.Date;
            EnsureHcmParameters();
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptEmploymentNotice(string employeeCode, DateTime asOfDate, string noticeDays) : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = asOfDate;
            PrmDays.Value = noticeDays ?? string.Empty;
        }
    }
}
