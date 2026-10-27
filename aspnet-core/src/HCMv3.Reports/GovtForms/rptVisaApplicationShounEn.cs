using System;

namespace HCMv3.Reports.GovtForms
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.rptVisaApplicationShounEn</c>.
    ///
    /// Shoun visa application. The one report in this batch that had no SQL at all: it bound an XAF CollectionDataSource directly onto the XPO Employee entity and walked navigation properties.
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
    public partial class rptVisaApplicationShounEn : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "GovtVisaApplicationShounEn";

        public rptVisaApplicationShounEn()
        {
            InitializeComponent();

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = GovtFormsImageStore.Load("rptVisaApplicationShounEn", "xrPictureBox1");
            xrPictureBox2.Image = GovtFormsImageStore.Load("rptVisaApplicationShounEn", "xrPictureBox2");
        }

        // THE BIG ONE FOR THIS REPORT. The legacy layout bound
        // DevExpress.Persistent.Base.ReportsV2.CollectionDataSource straight onto
        // HCM10.Module.BusinessObjects.PersonnelAdministration.Employee and read navigation paths
        // (Nationality.NameAr, Religion.NameAr, BasicPay.Amount,
        // OrgAssignment.OrganizationUnit.NameAr, Sponsor.*, VisaInfo.*). None of that survives:
        // there is no XPO session, and the report cannot resolve ABP repositories either. The data
        // source is now a SqlDataSource whose query flattens each of those paths into one column,
        // and the layout's XRBinding member names were updated to match. Three further changes are
        // called out inline in the designer file:
        //   * SponsorFullNameCal concatenated four Arabic sponsor name PARTS; HCMv3 Sponsors has
        //     only NameAr, so it collapses to that. *** FEATURE DEGRADED. ***
        //   * the report-level FilterString "[Code] = ?EmployeeCode" was replaced by a query
        //     parameter -- as a client-side filter it would fetch every tenant's employees first.
        //   * xrPictureBox1 had its Image bound to the "BloodGroup" column, a designer mis-drag
        //     that blanked the form scan. Removed.
        //
        // v17.2 -> v23.2 BREAK, and the subtlest one in this batch: every label here used the
        // legacy DataBindings collection (XRBinding("Text", null, "PassportNum")) rather than
        // ExpressionBindings. Against a report whose serialised Version is 23.2, DevExpress
        // renders those labels EMPTY -- no exception, no warning, correct page count, correct row
        // count from the data source, and a PDF containing nothing but the form scan. All 21 were
        // converted to ExpressionBinding("BeforePrint", "Text", "[Column]"). Every other report in
        // this batch already used ExpressionBindings, which is why only this one was affected.
        // WORTH CHECKING IN OTHER BATCHES: grep ported designers for XRBinding.
    }
}
