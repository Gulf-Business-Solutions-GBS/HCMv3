using System;
using System.Drawing;

namespace HCMv3.Reports.EmployeeInfo
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.EmployeeInfo.rptTerminationLetter</c>.
    ///
    /// XAF -> ABP: the legacy code-behind was 16 lines and did nothing but call
    /// <c>InitializeComponent()</c>. Every piece of XAF machinery lived in the shared base class
    /// <c>MyXtraReport</c> (962 lines of SecuritySystem / XPObjectSpaceProvider /
    /// GetObjectsQuery&lt;&gt; / WebApplication.Instance glue, including report-level row-level
    /// security). This report derives from <see cref="HcmReportBase"/> instead, which carries none
    /// of it -- see the HcmReportBase class remarks for what was dropped and why. In particular the
    /// legacy base class hid the employee-code parameter for non-admin users; that is an
    /// AUTHORIZATION decision and now belongs behind ABP permissions in the calling layer, never in
    /// a report layout a designer edit could revoke.
    ///
    /// MULTI-TENANCY: the query filters <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> and <see cref="HcmReportBase.ApplyConnectionString"/>
    /// before <c>CreateDocument()</c>, or the report returns nothing.
    /// </summary>
    public partial class rptTerminationLetter : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "TerminationLetter";

        public rptTerminationLetter()
        {
            InitializeComponent();
            EnsureHcmParameters();

            // The full-page letterhead used to live in the report .resx as a System.Drawing.Bitmap.
            // It ships as an embedded JPEG instead; see EmployeeInfoResources for why.
            xrPictureBox1.Image = EmployeeInfoResources.LoadImage(EmployeeInfoResources.TerminationLetterhead);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptTerminationLetter(string employeeCode) : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
        }
    }
}
