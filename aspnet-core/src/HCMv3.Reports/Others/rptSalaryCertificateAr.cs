using System;

namespace HCMv3.Reports.Others
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.Others.rptSalaryCertificate</c>.
    ///
    /// RENAMED ON PURPOSE. The legacy class is called <c>rptSalaryCertificate</c> -- an unqualified
    /// name that reads like a shared base class for the <c>*En</c> variant. It is not one. It is the
    /// ARABIC certificate, and inspecting the layout is what settles it rather than the catalog's
    /// label: every value label binds an Arabic column (<c>[FullNameAr]</c>, <c>[Nationality]</c>
    /// from <c>NameAr</c>, <c>[Jobtitle]</c> from <c>GovtJobTitle.NameAr</c>), every label is
    /// <c>MiddleRight</c>-aligned, and the full-page letterhead behind it is the Arabic one. It also
    /// shares no code with <c>rptSalaryCertificateEn</c> -- both derive straight from the legacy
    /// <c>MyXtraReport</c>. So the port is named <c>rptSalaryCertificateAr</c>, matching what it is,
    /// and registered under the catalog name <c>SalaryCertificateAr</c>.
    ///
    /// XAF CONSTRUCTS REPLACED: almost none -- this is the cheapest kind of report to port. The
    /// legacy code-behind is a constructor that sets <c>PrmDate</c> and an EMPTY <c>BeforePrint</c>
    /// handler; there is no XPO, no <c>SecuritySystem</c>, no <c>SysService</c>. The empty handler is
    /// dropped rather than ported (v17.2's <c>PrintEventHandler</c> signature no longer exists in
    /// v23.2 and there is nothing to run).
    ///
    /// LAYOUT NOTE (legacy defect, ported as-is): <c>xrLabel8</c> and <c>xrLabel10</c> both bind
    /// <c>[Org Unit]</c>, at two different places on the page. That looks like a designer slip in the
    /// legacy report, but it is a visible layout decision and is not silently "fixed" here.
    ///
    /// ARABIC / RTL: no <c>RightToLeft</c> or <c>RightToLeftLayout</c> flags and no embedded fonts
    /// are involved. The Arabic is achieved entirely with right-aligned labels, Arabic literal text
    /// in the layout, and Tahoma -- which ships with Windows and covers Arabic. Nothing is missing.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call <see cref="HcmReportBase.ApplyTenant"/>
    /// before <c>CreateDocument()</c> or the report returns nothing.
    /// </summary>
    public partial class rptSalaryCertificateAr : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "SalaryCertificateAr";

        public rptSalaryCertificateAr()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant global
            // query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            // See SalaryCertificateBackground: the letterhead moved out of the .resx.
            xrPictureBox1.Image = SalaryCertificateBackground.Load(SalaryCertificateBackground.ArabicResourceName);
        }

        /// <summary>Convenience overload for callers that already know the employee.</summary>
        public rptSalaryCertificateAr(string employeeCode, DateTime printDate)
            : this()
        {
            PrmEmpCode.Value = employeeCode ?? string.Empty;
            PrmDate.Value = printDate;
        }
    }
}
