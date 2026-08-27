using System;

namespace HCMv3.Reports.GovtForms
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.GovtForms.rptBusinessVisitVisaExtensionPledge</c>.
    ///
    /// This is a statutory Kuwait government form: a full-page scan of the official paper form
    /// (<c>xrPictureBox1</c>) with a handful of absolutely positioned RTL labels dropped onto the
    /// blanks. The layout is reproduced verbatim -- coordinates, fonts, RightToLeft flags and the
    /// A4 page geometry -- because the printed result has to line up with the real form. Nothing
    /// in it was "tidied".
    ///
    /// XAF CONSTRUCTS REPLACED
    ///   * base class <c>MyXtraReport</c> -> <see cref="HcmReportBase"/>; the legacy base pulled
    ///     <c>SecuritySystem.CurrentUserName</c>, opened an <c>XPObjectSpaceProvider</c> and applied
    ///     row-level security to the employee-code parameter. A DevExpress-constructed report has no
    ///     ABP DI container, and row-level security is an authorization decision that must not live
    ///     in a layout a designer edit can revoke -- it belongs in the calling layer.
    ///   * the <c>.resx</c>-embedded form scan -> an embedded JPEG attached below; see
    ///     <see cref="GovtFormsImageStore"/>.
    ///   * connection name <c>HCM10ConnectionString</c> -> <see cref="HcmReportBase.DesignTimeConnectionName"/>,
    ///     overridden at run time by <see cref="HcmReportBase.ApplyConnectionString"/>.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report returns
    /// nothing (the parameter defaults to 0, which matches no tenant).
    /// </summary>
    public partial class rptBusinessVisitVisaExtensionPledge : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "GovtBusinessVisitVisaExtensionPledge";

        public rptBusinessVisitVisaExtensionPledge()
        {
            InitializeComponent();

            // Legacy defaulted the print date in the ctor.
            PrmDate.Value = DateTime.Now;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = GovtFormsImageStore.Load("rptBusinessVisitVisaExtensionPledge", "xrPictureBox1");
        }

        // v17.2 -> v23.2: report BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs). The legacy body was empty; the handler is kept
        // because the designer file wires it.
        private void rptBusinessVisitVisaExtensionPledge_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
        }
    }
}
