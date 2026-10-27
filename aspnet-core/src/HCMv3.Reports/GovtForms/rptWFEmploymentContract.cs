using System;

namespace HCMv3.Reports.GovtForms
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.GovtForms.rptWFEmploymentContract</c>.
    ///
    /// MOSAL employment contract.
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
    public partial class rptWFEmploymentContract : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "GovtEmploymentContract";

        public rptWFEmploymentContract()
        {
            InitializeComponent();

            // Legacy ctor default; every real caller overwrites it.
            PrmDate.Value = DateTime.Now;
            PrmEmpCode.Value = string.Empty;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = GovtFormsImageStore.Load("rptWFEmploymentContract", "xrPictureBox1");
        }

        // CalcResidencyPeriod maps a residency period of 1..5 onto Arabic words. In HCMv3
        // EmployeeVisaInfos.CurrentResidencyPeriod is NVARCHAR, not an int, so the expression now
        // compares a string against integer literals. DevExpress coerces, and the seeded value "2"
        // resolves correctly -- but a non-numeric period would fall through to the empty branch.
        // *** Worth re-basing on a lookup once the residency-period domain is settled. ***
        // MISSING ASSET, NOT PORTED: the legacy code-behind carries a commented-out block that
        // loaded "~/Contents/fonts/BFantezy.ttf" into a System.Drawing.Text.PrivateFontCollection
        // and re-fonted the numeric date / ID labels with it, to render Arabic-styled numerals.
        // Three reasons it is not revived here:
        //   1. the font file is not in the legacy repository -- only the System.Web path is, so the
        //      bytes are simply not available to ship;
        //   2. it resolved that path through HttpContext.Current.Server.MapPath, which does not
        //      exist in ASP.NET Core, let alone inside a report constructed by DevExpress;
        //   3. it was already disabled in the legacy solution, so the forms in production have been
        //      printing with the designer fonts for years.
        // *** FLAGGED: if the ministry requires the Arabic-numeral typeface, the .ttf must be
        // obtained and embedded as an asset -- it is not something to approximate. ***
    }
}
