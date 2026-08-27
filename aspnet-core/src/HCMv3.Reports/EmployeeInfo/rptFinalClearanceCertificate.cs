using System;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.EmployeeInfo.rptFinalClearanceCertificate</c>.
    ///
    /// XAF -> ABP: the legacy code-behind was 17 lines -- <c>InitializeComponent()</c> plus
    /// defaulting <c>PrmDate</c> to today, which is kept. All XAF machinery lived in the shared
    /// <c>MyXtraReport</c> base class and is gone; see the <see cref="HcmReportBase"/> class
    /// remarks. Report-level row-level security is NOT ported: whether a user may pick an
    /// arbitrary employee code is an authorization decision, and baking it into report layout
    /// means a designer edit can revoke it.
    ///
    /// KNOWN HCMv3 DATA GAP (not a porting defect): EmployeeDocuments, EmployeeSponsors,
    /// EmployeeBanks and EmployeeSeparations are all empty, so Passport No., Sponsor, Bank,
    /// Account No. and Last Day In Service render blank. The employee's own details still print.
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptFinalClearanceCertificate : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "FinalClearanceCertificate";

        public rptFinalClearanceCertificate()
        {
            InitializeComponent();
            PrmDate.Value = DateTime.Now.Date;
            EnsureHcmParameters();

            // The full-page letterhead used to live in the report .resx as a System.Drawing.Bitmap.
            // It ships as an embedded JPEG instead; see EmployeeInfoResources for why.
            xrPictureBox1.Image = EmployeeInfoResources.LoadImage(EmployeeInfoResources.FinalClearanceLetterhead);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptFinalClearanceCertificate(string employeeCode, DateTime asOfDate) : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = asOfDate;
        }
    }
}
