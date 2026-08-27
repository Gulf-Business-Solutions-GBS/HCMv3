using System;

namespace HCMv3.Reports.GovtForms
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.rptWFshounPhotoApplication</c>.
    ///
    /// Shoun personal-photo application.
    ///
    /// This is a statutory Kuwait government form: a full-page scan of the official paper form with
    /// absolutely positioned RTL labels dropped onto its blanks. The layout is reproduced verbatim
    /// -- coordinates, fonts, RightToLeft flags, page geometry -- because the print has to line up
    /// with the real form. Nothing in it was tidied.
    ///
    /// XAF CONSTRUCTS REPLACED
    ///   * base class <c>MyXtraReport</c> -> <see cref="HcmReportBase"/>. The legacy base read
    ///     <c>SecuritySystem.CurrentUserName</c>, opened an <c>XPObjectSpaceProvider</c> against
    ///     <c>HCM10ConnectionString</c> and applied row-level security to the employee-code
    ///     parameter. A DevExpress-constructed report has no ABP DI container, so anything it needs
    ///     arrives as a report parameter; and row-level security is an authorization decision that
    ///     must not live in a layout a designer edit could revoke -- it belongs in the calling layer.
    ///   * the <c>.resx</c>-embedded form scan -> an embedded JPEG attached in the constructor; see
    ///     <see cref="GovtFormsImageStore"/>.
    ///   * connection name <c>HCM10ConnectionString</c> -> <see cref="HcmReportBase.DesignTimeConnectionName"/>,
    ///     overridden at run time by <see cref="HcmReportBase.ApplyConnectionString"/>.
    ///   * the cached <c>ResultSchemaSerializable</c> column list -> dropped. The rewrite changed the
    ///     column set, and a stale schema cache is worse than none; DevExpress rebuilds it.
    ///
    /// MULTI-TENANCY: every query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c> or the report returns
    /// nothing -- the parameter defaults to 0, which matches no tenant.
    /// </summary>
    public partial class rptWFshounPhotoApplication : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "GovtShounPhotoApplication";

        public rptWFshounPhotoApplication()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = GovtFormsImageStore.Load("rptWFshounPhotoApplication", "xrPictureBox1");
        }

        // PORT: the legacy designer set Scripts.OnDataSourceDemanded = "rptWFshounPhotoApplication_DataSourceDemanded".
        // Report scripts are compiled at run time from a string and are not supported in the HCMv3
        // hosting model, so the assignment was dropped. Nothing is lost: the legacy solution carries
        // no script body for that handler -- the property was left behind by the designer.
    }
}
