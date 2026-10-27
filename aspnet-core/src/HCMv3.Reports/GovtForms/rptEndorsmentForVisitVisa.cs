using System;

namespace HCMv3.Reports.GovtForms
{
    /// <summary>
    /// HCMv3 port of the legacy XAF report
    /// <c>HCM10.Module.Reports.GovtForms.rptEndorsmentForVisitVisa</c>.
    ///
    /// A statutory Kuwait visit-visa endorsement: a full-page scan of the official form with
    /// absolutely positioned labels over the blanks, in two parallel sets -- one English, one
    /// Arabic -- of which exactly one prints. Layout reproduced verbatim.
    ///
    /// XAF CONSTRUCTS REPLACED
    ///   * base class <c>MyXtraReport</c> -> <see cref="HcmReportBase"/> (see the class remarks
    ///     there: <c>SecuritySystem.CurrentUserName</c>, <c>XPObjectSpaceProvider</c> and the
    ///     row-level-security gate on the employee-code parameter are all gone; RLS is an
    ///     authorization decision for the calling layer, not for a report layout).
    ///   * the <c>.resx</c>-embedded form scan -> an embedded JPEG; see <see cref="GovtFormsImageStore"/>.
    ///   * connection name <c>HCM10ConnectionString</c> -> <see cref="HcmReportBase.DesignTimeConnectionName"/>.
    ///
    /// The <c>BeforePrint</c> language switch is genuine report logic and IS ported -- see
    /// <see cref="rptEndorsmentForVisitVisa_BeforePrint"/>.
    ///
    /// MULTI-TENANCY: the query carries <c>@TenantId</c>. Call
    /// <see cref="HcmReportBase.ApplyTenant"/> before <c>CreateDocument()</c>.
    /// </summary>
    public partial class rptEndorsmentForVisitVisa : HcmReportBase
    {
        /// <summary>Report name used by the Web Report Designer / Document Viewer storage.</summary>
        public const string ReportName = "GovtEndorsementForVisitVisa";

        /// <summary>Value of PrmLanguage that selects the Arabic label set.</summary>
        private const string ArabicLanguage = "Arabic";

        public rptEndorsmentForVisitVisa()
        {
            InitializeComponent();

            PrmDate.Value = DateTime.Now.Date;

            // MANDATORY: the SQL runs raw through SqlDataSource, outside EF Core's per-tenant
            // global query filter. See HcmReportBase.EnsureHcmParameters.
            EnsureHcmParameters();

            xrPictureBox1.Image = GovtFormsImageStore.Load("rptEndorsmentForVisitVisa", "xrPictureBox1");
        }

        /// <summary>
        /// Shows either the English or the Arabic label set. Ported as-is from the legacy handler,
        /// including its asymmetry: the legacy code toggles five English labels and six Arabic ones
        /// (<c>lblPassportAr</c> has no English counterpart in the list), so the English rendering
        /// leaves the Arabic passport label hidden and vice versa. That is how the form printed and
        /// it is not "corrected" here.
        ///
        /// v17.2 -> v23.2: report BeforePrint changed from
        /// <c>PrintEventHandler(PrintEventArgs)</c> to <c>BeforePrintEventHandler(CancelEventArgs)</c>.
        /// </summary>
        private void rptEndorsmentForVisitVisa_BeforePrint(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var isEnglish = !string.Equals(
                Convert.ToString(PrmLanguage.Value), ArabicLanguage, StringComparison.Ordinal);

            lblFullNameEn.Visible = isEnglish;
            lblNationalityEn.Visible = isEnglish;
            lblNationalityEn2.Visible = isEnglish;
            lblFullNameEn2.Visible = isEnglish;
            lblCivilIdEn.Visible = isEnglish;

            lblFullNameAr.Visible = !isEnglish;
            lblPassportAr.Visible = !isEnglish;
            lblNationalityAr.Visible = !isEnglish;
            lblNationalityAr2.Visible = !isEnglish;
            lblFullNameAr2.Visible = !isEnglish;
            lblCivilIdAr.Visible = !isEnglish;
        }
    }
}
