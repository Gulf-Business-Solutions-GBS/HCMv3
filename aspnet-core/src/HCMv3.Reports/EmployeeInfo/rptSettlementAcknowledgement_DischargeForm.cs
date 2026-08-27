using System;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.EmployeeInfo.rptSettlementAcknowledgement_DischargeForm</c>.
    ///
    /// XAF -> ABP: the legacy code-behind was 20 lines -- <c>InitializeComponent()</c> plus
    /// defaulting <c>PrmDate</c> to today, which is kept. All XAF machinery lived in the shared
    /// <c>MyXtraReport</c> base class and is gone; see the <see cref="HcmReportBase"/> class
    /// remarks. Report-level row-level security is NOT ported: it is an authorization decision for
    /// the calling layer.
    ///
    /// NOTE THE PARAMETER NAME. The employee-code parameter is spelled <c>PrmEmpCpde</c> -- a typo
    /// in the legacy layout. It is kept verbatim because the layout's data binding resolves by
    /// name; renaming it would silently unbind the report.
    ///
    /// KNOWN HCMv3 DATA GAP (not a porting defect): EmployeeSeparations, EmployeeManagers and
    /// EmployeeDocuments are all empty, so Last Day In Service, the reporting manager and the
    /// passport number render blank inside the acknowledgement paragraphs. The ported SQL wraps
    /// every interpolated value in ISNULL so the sentences still render with the gap visible; the
    /// legacy concatenation turned the WHOLE paragraph NULL if any single input was missing.
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptSettlementAcknowledgement_DischargeForm : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SettlementAcknowledgementDischargeForm";

        public rptSettlementAcknowledgement_DischargeForm()
        {
            InitializeComponent();
            PrmDate.Value = DateTime.Now.Date;
            EnsureHcmParameters();
        }

        /// <summary>
        /// Convenience overload. An empty <paramref name="employeeCode"/> is meaningful: the query
        /// then returns every employee, which is the legacy behaviour.
        /// </summary>
        public rptSettlementAcknowledgement_DischargeForm(string employeeCode, DateTime asOfDate) : this()
        {
            // Legacy spelling; see the class remarks.
            PrmEmpCpde.Value = employeeCode ?? string.Empty;
            PrmDate.Value = asOfDate;
        }
    }
}
