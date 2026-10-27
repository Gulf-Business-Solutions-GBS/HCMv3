using System;

namespace HCMv3.Reports.GovtForms
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report <c>HCM10.Module.Reports.rptWFPayRequestShounEn</c>.
    ///
    /// Shoun pay / sponsorship-transfer request. The form has two variants -- a new request and a transfer -- and BeforePrint swaps which set of labels prints.
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
    public partial class rptWFPayRequestShounEn : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "GovtPayRequestShounEn";

        public rptWFPayRequestShounEn()
        {
            InitializeComponent();

            // Legacy ctor default; every real caller overwrites it.
            PrmDate.Value = DateTime.Now;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = GovtFormsImageStore.Load("rptWFPayRequestShounEn", "xrPictureBox1");
            xrPictureBox2.Image = GovtFormsImageStore.Load("rptWFPayRequestShounEn", "xrPictureBox2");
        }

        // v17.2 -> v23.2: BeforePrint changed from PrintEventHandler(PrintEventArgs) to
        // BeforePrintEventHandler(CancelEventArgs).
        private void rptWFPayRequestShounEn_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {

            // Ported verbatim from the legacy handler, including the Arabic literal it switches on.
            // "ØªØ­ÙˆÙŠÙ„" means "transfer": on a transfer the form prints the second set of sponsor /
            // contract / licence / file labels and hides the first. Do NOT "simplify" this by
            // comparing against a localisation key -- the value comes from the PrmFormOption
            // look-up list, whose items are the Arabic strings the ministry prints.
            var formOption = PrmFormOption.Value == null ? string.Empty : PrmFormOption.Value.ToString();
            var isTransfer = formOption == "ØªØ­ÙˆÙŠÙ„";

            lblSponsorName1.Visible = !isTransfer;
            lblAuthSign1.Visible = !isTransfer;
            lblCurrentContratNo1.Visible = !isTransfer;
            lblCurrentLicenseNo1.Visible = !isTransfer;
            lblSponsorComp1.Visible = !isTransfer;
            lblCurrentFileNo1.Visible = !isTransfer;

            lblSponsorName2.Visible = isTransfer;
            lblAuthSign2.Visible = isTransfer;
            lblCurrentContratNo2.Visible = isTransfer;
            lblCurrentLicenseNo2.Visible = isTransfer;
            lblSponsorComp2.Visible = isTransfer;
            lblCurrentFileNo2.Visible = isTransfer;
        }

        // The legacy layout also declares a "Governate" dynamic look-up on a query of that name
        // that its data source never contained -- the @PrmGovernorate combo has always been empty.
        // Ported as-is rather than invented: adding a governorate list would change which values an
        // operator can put on a statutory form.
    }
}
