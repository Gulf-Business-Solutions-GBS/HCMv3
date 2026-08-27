using System;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.EmployeeInfo.rptEmpStatusChange</c>.
    ///
    /// XAF -> ABP: the legacy code-behind was 20 lines -- <c>InitializeComponent()</c> plus two
    /// empty <c>BeforePrint</c> handlers. All XAF machinery lived in the shared
    /// <c>MyXtraReport</c> base class and is gone; see the <see cref="HcmReportBase"/> class
    /// remarks. Report-level row-level security is NOT ported: it is an authorization decision for
    /// the calling layer.
    ///
    /// SEMANTIC DEVIATION worth flagging to the product owner: the legacy query took
    /// <c>OrgLevel2</c> as "the department" purely by POSITION, without checking the unit type --
    /// unlike every sibling report in the folder, which does check it. HCMv3 has no positional org
    /// levels, so the port resolves the nearest ancestor actually TYPED 'Department'. For a tenant
    /// whose OrgLevel2 was not in fact a Department, this changes which unit prints.
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptEmpStatusChange : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "EmpStatusChange";

        public rptEmpStatusChange()
        {
            InitializeComponent();
            EnsureHcmParameters();

            // The full-page letterhead used to live in the report .resx as a System.Drawing.Bitmap.
            // It ships as an embedded JPEG instead; see EmployeeInfoResources for why.
            xrPictureBox2.Image = EmployeeInfoResources.LoadImage(EmployeeInfoResources.StatusChangeLetterhead);
        }

        /// <summary>
        /// Convenience overload. An empty <paramref name="employeeCode"/> is meaningful: the query
        /// then returns every ACTIVE employee, which is the legacy behaviour.
        /// </summary>
        public rptEmpStatusChange(string employeeCode, string changeReasonEn, string changeReasonAr) : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmChangeReasonEn.Value = changeReasonEn ?? string.Empty;
            PrmChangeReasonAr.Value = changeReasonAr ?? string.Empty;
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs). Both legacy bodies were empty; the handlers are
        // kept because the designer file wires them.
        private void xrLabel18_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }

        private void xrLabel19_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
